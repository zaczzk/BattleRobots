using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the post-match results panel when a round ends.
    ///
    /// ── Data source ───────────────────────────────────────────────────────────
    ///   Reads <see cref="MatchResultSO"/>, which <see cref="MatchManager"/> writes
    ///   just <em>before</em> raising the MatchEnded VoidGameEvent.  This guarantees
    ///   that the SO contains fresh data when <see cref="ShowResults"/> runs.
    ///
    /// ── Results displayed ─────────────────────────────────────────────────────
    ///   • Outcome  — "VICTORY!" or "DEFEAT"
    ///   • Duration — MM:SS
    ///   • Earned   — currency awarded this match
    ///   • Balance  — new wallet total
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to a Canvas in the Arena scene.
    ///   2. Assign _resultPanel — the root GameObject of the results overlay.
    ///   3. Assign _matchResult — the MatchResultSO asset.
    ///   4. Assign _onMatchEnded — the same VoidGameEvent SO as MatchManager._onMatchEnded.
    ///   5. Assign _sceneRegistry — the shared SceneRegistry SO asset.
    ///   6. Assign the four optional Text fields.
    ///   7. Wire buttons:
    ///        "Play Again" → PostMatchController.OnPlayAgainPressed()
    ///        "Main Menu"  → PostMatchController.OnMainMenuPressed()
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace. References BattleRobots.Core only.
    ///   - Must NOT reference BattleRobots.Physics.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Delegates cached in Awake — zero alloc on Subscribe/Unsubscribe.
    ///   - String allocations in ShowResults() run at most once per match (cold path).
    /// </summary>
    public sealed class PostMatchController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Blackboard SO written by MatchManager before MatchEnded fires.")]
        [SerializeField] private MatchResultSO _matchResult;

        [Header("UI")]
        [Tooltip("Root GameObject of the post-match results overlay. Hidden by default.")]
        [SerializeField] private GameObject _resultPanel;

        [Tooltip("Displays 'VICTORY!' or 'DEFEAT'.")]
        [SerializeField] private Text _outcomeText;

        [Tooltip("Displays the match duration as MM:SS.")]
        [SerializeField] private Text _durationText;

        [Tooltip("Displays the currency earned this match.")]
        [SerializeField] private Text _rewardText;

        [Tooltip("Displays the new wallet balance after rewards.")]
        [SerializeField] private Text _balanceText;

        [Tooltip("Optional: displays total damage dealt by the player this match.")]
        [SerializeField] private Text _damageDoneText;

        [Tooltip("Optional: displays total damage taken by the player this match.")]
        [SerializeField] private Text _damageTakenText;

        [Tooltip("Optional: displays the player's current consecutive win streak after the match.")]
        [SerializeField] private Text _streakText;

        [Header("Win Streak (optional)")]
        [Tooltip("WinStreakSO read after match ends to display the current streak count. " +
                 "Leave null to hide streak display.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Header("M7 Progression (optional)")]
        [Tooltip("Displays XP earned this match, e.g. 'XP: +142'. " +
                 "Reads XPEarned from MatchResultSO. Leave null to omit.")]
        [SerializeField] private Text _xpEarnedText;

        [Tooltip("Displays the player's level after this match, e.g. 'Level 5'. " +
                 "Reads NewLevel from MatchResultSO. Leave null to omit.")]
        [SerializeField] private Text _levelText;

        [Tooltip("GameObject shown when the player levelled up this match (LeveledUp=true). " +
                 "Hidden when no level-up occurred. Leave null to omit.")]
        [SerializeField] private GameObject _levelUpBanner;

        [Header("M7 Tier (optional)")]
        [Tooltip("Reads current build rating from this SO to compute the player's tier. " +
                 "Requires _tierConfig to also be assigned. Leave null to omit tier display.")]
        [SerializeField] private BuildRatingSO _buildRating;

        [Tooltip("Tier threshold config used to resolve the tier display name. " +
                 "Requires _buildRating to also be assigned. Leave null to omit tier display.")]
        [SerializeField] private RobotTierConfig _tierConfig;

        [Tooltip("Displays the player's current build tier, e.g. 'Gold'. " +
                 "Populated from _buildRating + _tierConfig. Leave null to omit.")]
        [SerializeField] private Text _tierText;

        [Header("M7 Achievement (optional)")]
        [Tooltip("Runtime SO tracking achievement state. " +
                 "LastUnlockedId is read to show the most-recently unlocked achievement. " +
                 "Leave null to omit achievement display.")]
        [SerializeField] private PlayerAchievementsSO _playerAchievements;

        [Tooltip("Catalog used to resolve the achievement display name from LastUnlockedId. " +
                 "Leave null to fall back to showing the raw ID string.")]
        [SerializeField] private AchievementCatalogSO _achievementCatalog;

        [Tooltip("Displays the achievement unlocked this session, e.g. 'Achievement: First Victory'. " +
                 "Shown only when LastUnlockedId is non-empty. Leave null to omit.")]
        [SerializeField] private Text _achievementText;

        [Tooltip("GameObject shown when an achievement was unlocked (LastUnlockedId non-empty). " +
                 "Hidden when no achievement was unlocked. Leave null to omit.")]
        [SerializeField] private GameObject _achievementBanner;

        [Header("Bonus Display (optional)")]
        [Tooltip("Displays the total bonus currency earned from performance conditions. " +
                 "e.g. 'Bonus: +175' or 'Bonus: none'. " +
                 "Reads BonusEarned from the MatchResultSO blackboard. " +
                 "Leave null to omit.")]
        [SerializeField] private Text _bonusEarnedText;

        [Tooltip("Displays a per-condition breakdown of which bonuses were earned or missed. " +
                 "Requires _bonusCatalog to be assigned. " +
                 "e.g. '+ Perfect Shield: +100\\n○ Speed Run'. " +
                 "Leave null to omit.")]
        [SerializeField] private Text _bonusDetailText;

        [Tooltip("The same MatchBonusCatalogSO assigned to MatchManager._bonusCatalog. " +
                 "Conditions are re-evaluated here using MatchResultSO data to build the detail breakdown. " +
                 "Leave null to omit the per-condition breakdown (does not affect _bonusEarnedText).")]
        [SerializeField] private MatchBonusCatalogSO _bonusCatalog;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by MatchManager when the round ends.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Scene Names")]
        [Tooltip("Single SO holding all scene names. " +
                 "Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ SceneRegistry.")]
        [SerializeField] private SceneRegistry _sceneRegistry;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _matchEndedCallback;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchEndedCallback = ShowResults;

            // Hide the panel until a match ends.
            if (_resultPanel != null) _resultPanel.SetActive(false);
        }

        private void OnEnable()  => _onMatchEnded?.RegisterCallback(_matchEndedCallback);
        private void OnDisable() => _onMatchEnded?.UnregisterCallback(_matchEndedCallback);

        // ── Event handler ─────────────────────────────────────────────────────

        private void ShowResults()
        {
            if (_resultPanel != null) _resultPanel.SetActive(true);

            if (_matchResult == null)
            {
                Debug.LogWarning("[PostMatchController] _matchResult MatchResultSO not assigned — " +
                                 "results panel will show without data.");
                return;
            }

            // Outcome label
            if (_outcomeText != null)
                _outcomeText.text = _matchResult.PlayerWon ? "VICTORY!" : "DEFEAT";

            // Duration as MM:SS
            if (_durationText != null)
            {
                int totalSecs = Mathf.FloorToInt(_matchResult.DurationSeconds);
                int mins      = totalSecs / 60;
                int secs      = totalSecs % 60;
                _durationText.text = string.Format("Time: {0:00}:{1:00}", mins, secs);
            }

            // Currency earned
            if (_rewardText != null)
                _rewardText.text = string.Format("Earned: +{0}", _matchResult.CurrencyEarned);

            // New wallet balance
            if (_balanceText != null)
                _balanceText.text = string.Format("Balance: {0}", _matchResult.NewWalletBalance);

            // Damage statistics (optional — labels may not exist in every scene layout)
            if (_damageDoneText != null)
                _damageDoneText.text = string.Format("Damage Dealt: {0:F0}", _matchResult.DamageDone);

            if (_damageTakenText != null)
                _damageTakenText.text = string.Format("Damage Taken: {0:F0}", _matchResult.DamageTaken);

            // Win streak (optional)
            if (_streakText != null && _winStreak != null)
            {
                int streak = _winStreak.CurrentStreak;
                _streakText.text = streak > 0
                    ? string.Format("Win Streak: {0}!", streak)
                    : string.Format("Best Streak: {0}", _winStreak.BestStreak);
            }

            // Bonus earned total (optional — populated from MatchResultSO.BonusEarned)
            if (_bonusEarnedText != null)
            {
                int bonusEarned = _matchResult.BonusEarned;
                _bonusEarnedText.text = bonusEarned > 0
                    ? string.Format("Bonus: +{0}", bonusEarned)
                    : "Bonus: none";
            }

            // Per-condition bonus breakdown (optional — requires _bonusCatalog)
            // Conditions are re-evaluated against MatchResultSO data so the display
            // is always consistent with what MatchManager computed at match end.
            if (_bonusDetailText != null && _bonusCatalog != null)
            {
                var sb         = new System.Text.StringBuilder();
                var conditions = _bonusCatalog.Conditions;
                bool firstLine = true;
                for (int i = 0; i < conditions.Count; i++)
                {
                    var cond = conditions[i];
                    if (cond == null) continue;

                    bool met = MatchEndBonusEvaluator.EvaluateCondition(
                        cond,
                        _matchResult.PlayerWon,
                        _matchResult.DurationSeconds,
                        _matchResult.DamageDone,
                        _matchResult.DamageTaken);

                    string condName = string.IsNullOrWhiteSpace(cond.DisplayName)
                        ? cond.ConditionType.ToString()
                        : cond.DisplayName;

                    if (!firstLine) sb.Append('\n');
                    if (met)
                        sb.AppendFormat("+ {0}: +{1}", condName, cond.BonusAmount);
                    else
                        sb.AppendFormat("○ {0}", condName);
                    firstLine = false;
                }
                _bonusDetailText.text = sb.ToString();
            }

            // ── M7 Progression display ────────────────────────────────────────

            // XP earned this match (optional)
            if (_xpEarnedText != null)
                _xpEarnedText.text = string.Format("XP: +{0}", _matchResult.XPEarned);

            // Current level after match (optional)
            if (_levelText != null)
                _levelText.text = string.Format("Level {0}", _matchResult.NewLevel);

            // Level-up banner visibility (optional)
            if (_levelUpBanner != null)
                _levelUpBanner.SetActive(_matchResult.LeveledUp);

            // ── M7 Tier display ───────────────────────────────────────────────

            if (_tierText != null && _buildRating != null && _tierConfig != null)
            {
                RobotTierLevel tier = RobotTierEvaluator.EvaluateTier(_buildRating, _tierConfig);
                _tierText.text = _tierConfig.GetDisplayName(tier);
            }

            // ── M7 Achievement display ────────────────────────────────────────

            string lastAchievementId = _playerAchievements?.LastUnlockedId;
            bool hasAchievement = !string.IsNullOrWhiteSpace(lastAchievementId);

            if (_achievementBanner != null)
                _achievementBanner.SetActive(hasAchievement);

            if (_achievementText != null && hasAchievement)
            {
                // Resolve display name from catalog; fall back to raw ID when catalog absent.
                string displayName = null;
                if (_achievementCatalog != null)
                {
                    var all = _achievementCatalog.Achievements;
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (all[i] != null && all[i].Id == lastAchievementId)
                        {
                            displayName = all[i].DisplayName;
                            break;
                        }
                    }
                }
                _achievementText.text = string.Format("Achievement: {0}",
                    displayName ?? lastAchievementId);
            }

            Debug.Log($"[PostMatchController] Results shown — " +
                      $"PlayerWon={_matchResult.PlayerWon}, " +
                      $"Duration={_matchResult.DurationSeconds:F1}s, " +
                      $"Earned={_matchResult.CurrencyEarned}, " +
                      $"Balance={_matchResult.NewWalletBalance}.");
        }

        // ── Public API (button callbacks) ─────────────────────────────────────

        /// <summary>Called by the Play Again button — reloads the Arena scene.</summary>
        public void OnPlayAgainPressed()
        {
            if (_resultPanel != null) _resultPanel.SetActive(false);
            string sceneName = _sceneRegistry != null ? _sceneRegistry.ArenaSceneName : "Arena";
            SceneLoader.LoadScene(sceneName);
        }

        /// <summary>Called by the Main Menu button — returns to the main menu.</summary>
        public void OnMainMenuPressed()
        {
            if (_resultPanel != null) _resultPanel.SetActive(false);
            string sceneName = _sceneRegistry != null ? _sceneRegistry.MainMenuSceneName : "MainMenu";
            SceneLoader.LoadScene(sceneName);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_matchResult == null)
                Debug.LogWarning("[PostMatchController] _matchResult MatchResultSO not assigned.", this);
            if (_onMatchEnded == null)
                Debug.LogWarning("[PostMatchController] _onMatchEnded not assigned.", this);
            if (_resultPanel == null)
                Debug.LogWarning("[PostMatchController] _resultPanel not assigned.", this);
            if (_sceneRegistry == null)
                Debug.LogWarning("[PostMatchController] _sceneRegistry not assigned — " +
                                 "button transitions will fall back to hard-coded scene names.", this);
        }
#endif
    }
}
