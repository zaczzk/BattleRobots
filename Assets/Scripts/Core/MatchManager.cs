using System;
using System.Collections.Generic;
using BattleRobots.Physics;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Manages a single match round: countdown timer, win-condition checking,
    /// MatchRecord writing, and economy reward distribution.
    ///
    /// ── Win conditions (checked every frame while match is running) ───────────
    ///   1. Enemy HealthSO reaches zero   → player wins.
    ///   2. Player HealthSO reaches zero  → enemy wins.
    ///   3. Timer expires                 → combatant with more health wins;
    ///                                      tie counts as a player loss.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   • Add a VoidGameEventListener to the same GameObject.
    ///     Event = MatchStarted SO, Response = MatchManager.HandleMatchStarted().
    ///   • Assign _playerHealth and _enemyHealth HealthSO assets.
    ///   • Assign _playerWallet PlayerWallet SO.
    ///   • Assign _onMatchEnded and (optionally) _onTimerUpdated event channel SOs.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - No heap allocations in Update (only float/bool comparisons).
    ///   - SaveSystem called only in EndMatch (cold path).
    ///   - BattleRobots.Core only — no Physics/UI namespace references.
    ///   - Cross-component signalling via SO event channels.
    /// </summary>
    public sealed class MatchManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Match Config (optional)")]
        [Tooltip("When assigned, RoundDuration / BaseWinReward / ConsolationReward are read " +
                 "from this SO asset instead of the per-field inspector values below. " +
                 "Centralises balance tuning to a single shared asset. " +
                 "Leave null to use the per-component inspector values (backwards-compatible).")]
        [SerializeField] private MatchRewardConfig _rewardConfig;

        [Header("Round Settings")]
        [Tooltip("Maximum duration of one round in seconds. " +
                 "Ignored when MatchRewardConfig is assigned.")]
        [SerializeField, Min(10f)] private float _roundDuration = 120f;

        [Header("Economy")]
        [Tooltip("Currency awarded to the player for winning a match. " +
                 "Ignored when MatchRewardConfig is assigned.")]
        [SerializeField, Min(0)] private int _winReward = 200;

        [Tooltip("Consolation currency awarded even on a loss. " +
                 "Ignored when MatchRewardConfig is assigned.")]
        [SerializeField, Min(0)] private int _lossConsolationReward = 50;

        [Header("Combatants")]
        [Tooltip("HealthSO for the local player robot.")]
        [SerializeField] private HealthSO _playerHealth;

        [Tooltip("HealthSO for the enemy robot (AI or remote).")]
        [SerializeField] private HealthSO _enemyHealth;

        [Header("Wallet")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Event Channels — Out")]
        [Tooltip("Raised when the match ends (win or loss).")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised every frame while match is running. Payload = seconds remaining.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        [Header("Match Result")]
        [Tooltip("Blackboard SO written with match outcome just before _onMatchEnded fires. " +
                 "Read by PostMatchController to populate the results screen.")]
        [SerializeField] private MatchResultSO _matchResult;

        [Tooltip("RobotAssembler on the player's robot root. " +
                 "Its equipped part IDs are recorded in the MatchRecord.")]
        [SerializeField] private RobotAssembler _playerAssembler;

        [Header("Statistics (optional)")]
        [Tooltip("Runtime stat blackboard that accumulates per-hit damage data via " +
                 "DamageGameEventListener components in the scene. When assigned, EndMatch() " +
                 "reads TotalDamageDealt/TotalDamageTaken from here instead of the end-of-match " +
                 "health-difference approximation. Reset() is called at HandleMatchStarted().")]
        [SerializeField] private MatchStatisticsSO _matchStatistics;

        [Header("Win Streak (optional)")]
        [Tooltip("SO that tracks the player's consecutive win streak. " +
                 "RecordWin() / RecordLoss() called in EndMatch(); streak persisted to SaveData. " +
                 "When assigned, the win reward is multiplied by StreakBonusCalculator " +
                 "(+10 % per streak level, capped at +50 % for streak ≥ 5). " +
                 "Leave null to skip streak tracking (backwards-compatible).")]
        [SerializeField] private WinStreakSO _winStreak;

        [Header("Progression (optional)")]
        [Tooltip("SO that tracks the player's accumulated XP and level. " +
                 "XPRewardCalculator.CalculateMatchXP() is called in EndMatch() and the " +
                 "result is passed to AddXP(). XP and level are persisted to SaveData. " +
                 "Leave null to skip progression tracking (backwards-compatible).")]
        [SerializeField] private PlayerProgressionSO _playerProgression;

        [Header("Career Statistics (optional)")]
        [Tooltip("SO that accumulates career-wide damage, currency, and playtime totals. " +
                 "RecordMatch() called in EndMatch(); PatchSaveData() called before Save(). " +
                 "Leave null to skip career stat tracking (backwards-compatible).")]
        [SerializeField] private PlayerCareerStatsSO _careerStats;

        [Header("Arena Selection (optional)")]
        [Tooltip("Runtime SO written by ArenaSelectionController. " +
                 "When assigned and HasSelection is true, Current.config.ArenaIndex is written " +
                 "to the MatchRecord so post-match analytics can identify the arena played. " +
                 "Leave null to record arenaIndex = 0 (backwards-compatible).")]
        [SerializeField] private SelectedArenaSO _selectedArena;

        [Header("Opponent Selection (optional)")]
        [Tooltip("Runtime SO written by OpponentSelectionController in the pre-match lobby. " +
                 "When assigned and HasSelection is true, Current.DisplayName is written to " +
                 "MatchRecord.opponentName so match history shows the opponent chosen. " +
                 "Leave null to record opponentName = \"\" (backwards-compatible).")]
        [SerializeField] private SelectedOpponentSO _selectedOpponent;

        [Header("Match Modifier (optional)")]
        [Tooltip("Runtime SO written by MatchModifierSelectionController. " +
                 "When assigned and HasSelection is true:\n" +
                 "  • Current.TimeMultiplier is applied to the base round duration at match start.\n" +
                 "  • Current.RewardMultiplier is applied to the total reward at match end.\n" +
                 "Leave null to use unmodified duration and rewards (backwards-compatible).")]
        [SerializeField] private SelectedModifierSO _selectedModifier;

        [Header("Match End Bonuses (optional)")]
        [Tooltip("Catalogue of performance-bonus conditions evaluated at match end. " +
                 "Each satisfied condition adds its BonusAmount to the match reward before the " +
                 "wallet is credited and before the MatchRecord is built. " +
                 "Leave null to skip bonus evaluation (backwards-compatible).")]
        [SerializeField] private MatchBonusCatalogSO _bonusCatalog;

        [Header("Personal Best (optional)")]
        [Tooltip("Tracks the player's numeric match score and all-time personal best. " +
                 "MatchScoreCalculator.Calculate() is called in EndMatch() after the " +
                 "MatchResultSO blackboard is written; Submit() is called before " +
                 "_onMatchEnded fires so UI subscribers already see the updated score. " +
                 "BestScore is persisted to SaveData.personalBestScore. " +
                 "Leave null to skip score tracking (backwards-compatible).")]
        [SerializeField] private PersonalBestSO _personalBest;

        [Header("Leaderboard (optional)")]
        [Tooltip("Local top-N match score leaderboard. Submit() is called in EndMatch() just " +
                 "after the MatchResultSO blackboard is written and the match score is computed, " +
                 "so the board is updated before _onMatchEnded fires. " +
                 "TakeSnapshot() persists the updated board to SaveData.leaderboardEntries. " +
                 "Leave null to skip leaderboard tracking (backwards-compatible).")]
        [SerializeField] private MatchLeaderboardSO _matchLeaderboard;

        [Header("Score History (optional)")]
        [Tooltip("Rolling chronological log of the last N match scores. Record() is called in " +
                 "EndMatch() after the match score is computed, so the history reflects the " +
                 "completed match before _onMatchEnded fires. " +
                 "TakeSnapshot() persists the updated history to SaveData.scoreHistoryScores. " +
                 "Leave null to skip score-history tracking (backwards-compatible).")]
        [SerializeField] private ScoreHistorySO _scoreHistory;

        [Header("Career Highlights (optional)")]
        [Tooltip("Tracks per-category single-match career bests (best damage, fastest win, etc.). " +
                 "Update() is called in EndMatch() after MatchResultSO.Write() so all fields " +
                 "reflect this match's outcome before _onMatchEnded fires. " +
                 "TakeSnapshot() persists updated records to SaveData.careerHighlights. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private CareerHighlightsSO _careerHighlights;

        [Header("Session Summary (optional)")]
        [Tooltip("Lightweight session-scoped tracker (matches played, wins, currency earned). " +
                 "RecordMatch() is called in EndMatch() after MatchResultSO.Write() so all result " +
                 "fields are current when the session counters are updated. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private SessionSummarySO _sessionSummary;

        [Header("Combo Counter (optional)")]
        [Tooltip("Runtime SO that tracks the player's hit combo streak during a match. " +
                 "MaxCombo is read in EndMatch() and passed to MatchScoreCalculator.Calculate() " +
                 "to award +5 points per hit in the best streak (e.g. MaxCombo=10 → +50 score). " +
                 "Leave null to skip combo scoring (backwards-compatible: maxCombo defaults to 0).")]
        [SerializeField] private ComboCounterSO _comboCounter;

        [Header("Timer Warning (optional)")]
        [Tooltip("Configures time thresholds that fire VoidGameEvent channels as the match timer " +
                 "counts down (e.g. at 60 s, 30 s, 10 s). Reset() is called at match start so " +
                 "each match fires every threshold fresh. Wire MatchTimerWarningController to the " +
                 "same _onTimerUpdated FloatGameEvent to show a panel overlay at low-time marks. " +
                 "Leave null to skip timer-warning events (backwards-compatible).")]
        [SerializeField] private MatchTimerWarningSO _timerWarning;

        [Header("Damage Type Mastery (optional — T179)")]
        [Tooltip("Cumulative cross-session mastery SO. AddDealtFromStats() is called in EndMatch() " +
                 "to accumulate per-type damage for the session. TakeSnapshot() then persists to " +
                 "SaveData. Leave null to skip mastery tracking (backwards-compatible).")]
        [SerializeField] private DamageTypeMasterySO _masterySystem;

        [Header("Audio")]
        [Tooltip("AudioEvent SO played when the player wins the match.")]
        [SerializeField] private AudioEvent _onWinJingle;

        [Tooltip("AudioEvent SO played when the player loses the match.")]
        [SerializeField] private AudioEvent _onLossJingle;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>True while a round is active.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Seconds remaining in the current round. Zero when not running.</summary>
        public float TimeRemaining => _timeRemaining;

        private bool  _matchRunning;
        private float _timeRemaining;

        // Cached modified round duration set in HandleMatchStarted (base × time multiplier).
        // EndMatch reads this instead of recalculating to ensure elapsed is always positive
        // when a MatchModifier changes the round length.
        private float _activeRoundDuration;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_matchRunning) return;

            // Decrement timer — no allocation, pure float math
            _timeRemaining -= Time.deltaTime;
            _onTimerUpdated?.Raise(_timeRemaining);

            // Check death win conditions
            if (_enemyHealth != null && _enemyHealth.IsDead)
            {
                EndMatch(playerWon: true);
                return;
            }

            if (_playerHealth != null && _playerHealth.IsDead)
            {
                EndMatch(playerWon: false);
                return;
            }

            // Timer expiry — most health wins; tie is a loss for the player
            if (_timeRemaining <= 0f)
            {
                bool playerHasMoreHealth =
                    _playerHealth != null &&
                    _enemyHealth  != null &&
                    _playerHealth.CurrentHealth > _enemyHealth.CurrentHealth;

                EndMatch(playerWon: playerHasMoreHealth);
            }
        }

        // ── Public API (called by VoidGameEventListener response) ─────────────

        /// <summary>
        /// Resets both HealthSOs and starts the round timer.
        /// Wire to a VoidGameEventListener responding to the MatchStarted SO channel.
        /// Safe to call from the Inspector Response UnityEvent.
        /// </summary>
        public void HandleMatchStarted()
        {
            if (_playerHealth == null || _enemyHealth == null)
            {
                Debug.LogError("[MatchManager] Player or enemy HealthSO not assigned — match cannot start.");
                return;
            }

            _playerHealth.Reset();
            _enemyHealth.Reset();

            // Clear per-match stat accumulator so previous match data does not bleed in.
            _matchStatistics?.Reset();

            // Reset timer-warning SO so every threshold can fire once more for this match.
            _timerWarning?.Reset();

            // Use MatchRewardConfig when assigned; fall back to per-component inspector field.
            _timeRemaining = _rewardConfig != null ? _rewardConfig.RoundDuration : _roundDuration;

            // Apply match-modifier time multiplier when a modifier is selected.
            // Modifier is applied after the base duration is resolved so both
            // MatchRewardConfig and per-field inspector paths are covered.
            if (_selectedModifier != null
                && _selectedModifier.HasSelection
                && _selectedModifier.Current != null)
            {
                _timeRemaining *= _selectedModifier.Current.TimeMultiplier;
            }

            // Cache the final (post-modifier) round duration so EndMatch can compute
            // elapsed time correctly even when a time-multiplier modifier is active.
            _activeRoundDuration = _timeRemaining;

            _matchRunning  = true;

            // Broadcast initial timer value so UI can display it immediately
            _onTimerUpdated?.Raise(_timeRemaining);

            Debug.Log($"[MatchManager] Match started. Round duration: {_timeRemaining}s.");
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void EndMatch(bool playerWon)
        {
            // Guard against re-entry (both HealthSOs could die in the same frame)
            if (!_matchRunning) return;
            _matchRunning = false;

            // Use _activeRoundDuration (set in HandleMatchStarted after applying any time-modifier)
            // so elapsed is always correct regardless of MatchRewardConfig or MatchModifier.
            float elapsed = _activeRoundDuration - Mathf.Max(0f, _timeRemaining);

            // Update win streak before computing the bonus so the bonus uses the
            // post-win streak value (e.g. streak was 2, RecordWin() → 3, bonus = +30 %).
            if (playerWon) _winStreak?.RecordWin();
            else           _winStreak?.RecordLoss();

            // Apply streak bonus to win rewards only; consolation reward is never boosted.
            // Use MatchRewardConfig when assigned; fall back to per-component inspector fields.
            int baseReward = playerWon
                ? (_rewardConfig != null ? _rewardConfig.BaseWinReward     : _winReward)
                : (_rewardConfig != null ? _rewardConfig.ConsolationReward : _lossConsolationReward);
            int totalReward  = (playerWon && _winStreak != null)
                ? StreakBonusCalculator.ApplyToReward(baseReward, _winStreak.CurrentStreak)
                : baseReward;

            // Apply match-modifier reward multiplier when a modifier is selected.
            // Applied after streak bonus so both stack correctly (streak × modifier).
            if (_selectedModifier != null
                && _selectedModifier.HasSelection
                && _selectedModifier.Current != null)
            {
                totalReward = Mathf.RoundToInt(totalReward * _selectedModifier.Current.RewardMultiplier);
            }

            // Award XP — uses post-match streak value (0 after a loss, incremented after a win).
            // Capture level before AddXP so we can detect level-up for the post-match screen.
            int levelBefore = _playerProgression?.CurrentLevel ?? 1;
            int xpEarned    = 0;
            if (_playerProgression != null)
            {
                xpEarned = XPRewardCalculator.CalculateMatchXP(
                    playerWon, elapsed, _winStreak?.CurrentStreak ?? 0);
                _playerProgression.AddXP(xpEarned);
            }
            bool leveledUp = _playerProgression != null &&
                             _playerProgression.CurrentLevel > levelBefore;
            int newLevel = _playerProgression?.CurrentLevel ?? 1;

            // Prefer accumulated per-hit stats from MatchStatisticsSO when available;
            // fall back to the end-of-match health-difference approximation otherwise.
            float damageDone = _matchStatistics != null
                ? _matchStatistics.TotalDamageDealt
                : (_enemyHealth  != null ? _enemyHealth.MaxHealth  - _enemyHealth.CurrentHealth  : 0f);
            float damageTaken = _matchStatistics != null
                ? _matchStatistics.TotalDamageTaken
                : (_playerHealth != null ? _playerHealth.MaxHealth - _playerHealth.CurrentHealth : 0f);

            // Evaluate performance bonuses (win-only conditions) and add to total reward.
            // Evaluated after damageDone/damageTaken are known so all condition types are valid.
            // Null catalog is a no-op (returns 0) — backwards-compatible with existing wiring.
            int bonusEarned = MatchEndBonusEvaluator.Evaluate(
                playerWon, elapsed, damageDone, damageTaken, _bonusCatalog);
            totalReward += bonusEarned;

            // Award currency
            if (_playerWallet != null)
                _playerWallet.AddFunds(totalReward);

            int walletSnapshot = _playerWallet != null ? _playerWallet.Balance : 0;

            // Collect equipped part IDs from the player's assembler (if assigned).
            var partIds = _playerAssembler != null
                ? new List<string>(_playerAssembler.GetEquippedPartIds())
                : new List<string>();

            // Resolve arena index from SelectedArenaSO when available; default to 0.
            int arenaIndex = (_selectedArena != null
                              && _selectedArena.HasSelection
                              && _selectedArena.Current?.config != null)
                ? _selectedArena.Current.config.ArenaIndex
                : 0;

            // Resolve opponent name from SelectedOpponentSO when available; default to "".
            string opponentName = (_selectedOpponent != null
                                   && _selectedOpponent.HasSelection
                                   && _selectedOpponent.Current != null)
                ? (_selectedOpponent.Current.DisplayName ?? "")
                : "";

            // Build and persist match record
            var record = new MatchRecord
            {
                timestamp       = DateTime.UtcNow.ToString("o"),
                arenaIndex      = arenaIndex,
                opponentName    = opponentName,
                playerWon       = playerWon,
                durationSeconds = elapsed,
                damageDone      = damageDone,
                damageTaken     = damageTaken,
                currencyEarned  = totalReward,
                walletSnapshot  = walletSnapshot,
                equippedPartIds = partIds,
            };

            // Update career-wide stat accumulator (damage, currency, playtime).
            _careerStats?.RecordMatch(record);

            // Append to save file — load, mutate, save
            SaveData saveData = SaveSystem.Load();
            saveData.walletBalance = walletSnapshot;
            saveData.matchHistory.Add(record);

            // Persist current streak values so they survive the next session.
            if (_winStreak != null)
            {
                saveData.currentWinStreak = _winStreak.CurrentStreak;
                saveData.bestWinStreak    = _winStreak.BestStreak;
            }

            // Persist XP and level so they survive the next session.
            if (_playerProgression != null)
            {
                saveData.playerTotalXP = _playerProgression.TotalXP;
                saveData.playerLevel   = _playerProgression.CurrentLevel;
            }

            // Persist career-wide totals so they survive the next session.
            _careerStats?.PatchSaveData(saveData);

            // Write blackboard SO before raising MatchEnded so PostMatchController
            // reads correct data when its callback fires.
            // bonusEarned is stored separately so the UI can display "Bonus: +N" without
            // re-evaluating conditions; it is already included in totalReward / currencyEarned.
            // xpEarned / leveledUp / newLevel give the post-match screen M7 progression data.
            _matchResult?.Write(playerWon, elapsed, totalReward, walletSnapshot,
                                damageDone, damageTaken, bonusEarned,
                                xpEarned, leveledUp, newLevel);

            // Update session summary (matches played, wins, currency earned this session).
            // Called after MatchResultSO.Write() so all result fields are current.
            // Session summary is never persisted — it resets each play session.
            _sessionSummary?.RecordMatch(_matchResult);

            // Compute and submit match score for personal best tracking.
            // Called AFTER MatchResultSO.Write() so the calculator reads fresh data.
            // Called BEFORE _onMatchEnded fires so UI subscribers already see the
            // updated PersonalBestSO.CurrentScore / BestScore / IsNewBest.
            if (_personalBest != null)
            {
                int matchScore = MatchScoreCalculator.Calculate(_matchResult,
                    _comboCounter != null ? _comboCounter.MaxCombo : 0);
                _personalBest.Submit(matchScore);
            }

            // Persist personal best score so it survives the next session.
            if (_personalBest != null)
                saveData.personalBestScore = _personalBest.BestScore;

            // Submit to local leaderboard — fired after MatchResultSO is written and the
            // match score is available, so the board reflects the completed match.
            // TakeSnapshot() is called immediately so the persisted list is always in sync.
            if (_matchLeaderboard != null && _matchResult != null)
            {
                _matchLeaderboard.Submit(_matchResult, opponentName, arenaIndex);
                saveData.leaderboardEntries = _matchLeaderboard.TakeSnapshot();
            }

            // Record match score in the chronological score history.
            // Called after MatchResultSO is written so MatchScoreCalculator reads fresh data.
            // Null _matchResult is safe — Record(0) would be misleading so we guard here.
            if (_scoreHistory != null && _matchResult != null)
            {
                int historyScore = MatchScoreCalculator.Calculate(_matchResult,
                    _comboCounter != null ? _comboCounter.MaxCombo : 0);
                _scoreHistory.Record(historyScore);
                saveData.scoreHistoryScores = _scoreHistory.TakeSnapshot();
            }

            // Update per-category career highlights (best damage, fastest win, etc.).
            // Called after MatchResultSO.Write() so all result fields are current.
            // Update() null-guards _matchResult internally, so no outer guard required.
            if (_careerHighlights != null)
            {
                _careerHighlights.Update(_matchResult);
                saveData.careerHighlights = _careerHighlights.TakeSnapshot();
            }

            // Accumulate per-type damage into the cross-session mastery system (T179).
            // AddDealtFromStats is a no-op when stats is null; TakeSnapshot is allocation-free.
            if (_masterySystem != null && _matchStatistics != null)
            {
                _masterySystem.AddDealtFromStats(_matchStatistics);
                _masterySystem.TakeSnapshot(
                    out saveData.masteryPhysicalAccum, out saveData.masteryEnergyAccum,
                    out saveData.masteryThermalAccum,  out saveData.masteryShockAccum,
                    out saveData.masteryPhysicalDone,  out saveData.masteryEnergyDone,
                    out saveData.masteryThermalDone,   out saveData.masteryShockDone);
            }

            SaveSystem.Save(saveData);

            // Signal other systems
            _onMatchEnded?.Raise();

            // Audio jingle
            if (playerWon) _onWinJingle?.Raise();
            else           _onLossJingle?.Raise();

            Debug.Log($"[MatchManager] Match ended. PlayerWon={playerWon}, " +
                      $"Duration={elapsed:F1}s, Reward={totalReward} (bonus={bonusEarned}), " +
                      $"Wallet={walletSnapshot}, " +
                      $"Streak={_winStreak?.CurrentStreak ?? 0}, " +
                      $"XP+{xpEarned} Level={newLevel}{(leveledUp ? " (LEVEL UP)" : string.Empty)}, " +
                      $"TotalXP={_playerProgression?.TotalXP ?? 0}.");
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_playerHealth == null)
                Debug.LogWarning("[MatchManager] _playerHealth HealthSO not assigned.");
            if (_enemyHealth == null)
                Debug.LogWarning("[MatchManager] _enemyHealth HealthSO not assigned.");
            if (_playerWallet == null)
                Debug.LogWarning("[MatchManager] _playerWallet PlayerWallet SO not assigned.");
            if (_onMatchEnded == null)
                Debug.LogWarning("[MatchManager] _onMatchEnded event channel not assigned.");
        }
#endif
    }
}
