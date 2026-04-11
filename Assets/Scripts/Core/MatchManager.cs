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

            // Use MatchRewardConfig when assigned; fall back to per-component inspector field.
            _timeRemaining = _rewardConfig != null ? _rewardConfig.RoundDuration : _roundDuration;
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

            // Use the same config-aware duration that HandleMatchStarted set _timeRemaining from,
            // so elapsed is always correct regardless of whether a MatchRewardConfig is assigned.
            float activeRoundDuration = _rewardConfig != null ? _rewardConfig.RoundDuration : _roundDuration;
            float elapsed = activeRoundDuration - Mathf.Max(0f, _timeRemaining);

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

            // Award XP — uses post-match streak value (0 after a loss, incremented after a win).
            if (_playerProgression != null)
            {
                int xpEarned = XPRewardCalculator.CalculateMatchXP(
                    playerWon, elapsed, _winStreak?.CurrentStreak ?? 0);
                _playerProgression.AddXP(xpEarned);
            }

            // Prefer accumulated per-hit stats from MatchStatisticsSO when available;
            // fall back to the end-of-match health-difference approximation otherwise.
            float damageDone = _matchStatistics != null
                ? _matchStatistics.TotalDamageDealt
                : (_enemyHealth  != null ? _enemyHealth.MaxHealth  - _enemyHealth.CurrentHealth  : 0f);
            float damageTaken = _matchStatistics != null
                ? _matchStatistics.TotalDamageTaken
                : (_playerHealth != null ? _playerHealth.MaxHealth - _playerHealth.CurrentHealth : 0f);

            // Award currency
            if (_playerWallet != null)
                _playerWallet.AddFunds(totalReward);

            int walletSnapshot = _playerWallet != null ? _playerWallet.Balance : 0;

            // Collect equipped part IDs from the player's assembler (if assigned).
            var partIds = _playerAssembler != null
                ? new List<string>(_playerAssembler.GetEquippedPartIds())
                : new List<string>();

            // Build and persist match record
            var record = new MatchRecord
            {
                timestamp       = DateTime.UtcNow.ToString("o"),
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

            SaveSystem.Save(saveData);

            // Write blackboard SO before raising MatchEnded so PostMatchController
            // reads correct data when its callback fires.
            _matchResult?.Write(playerWon, elapsed, totalReward, walletSnapshot, damageDone, damageTaken);

            // Signal other systems
            _onMatchEnded?.Raise();

            // Audio jingle
            if (playerWon) _onWinJingle?.Raise();
            else           _onLossJingle?.Raise();

            Debug.Log($"[MatchManager] Match ended. PlayerWon={playerWon}, " +
                      $"Duration={elapsed:F1}s, Reward={totalReward}, Wallet={walletSnapshot}, " +
                      $"Streak={_winStreak?.CurrentStreak ?? 0}, " +
                      $"Level={_playerProgression?.CurrentLevel ?? 1}, " +
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
