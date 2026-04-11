using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays career-wide statistics on any panel (Main Menu profile, post-match summary, etc.).
    ///
    /// Reads damage/currency/playtime totals from <see cref="PlayerCareerStatsSO"/> and
    /// win/loss counts from optional companion SOs (<see cref="PlayerAchievementsSO"/>,
    /// <see cref="WinStreakSO"/>, <see cref="PlayerProgressionSO"/>).
    ///
    /// ── Wiring instructions ───────────────────────────────────────────────────
    ///   1. Assign <c>_careerStats</c> (required).
    ///   2. Assign <c>_onStatsUpdated</c> — the same VoidGameEvent SO wired to
    ///      <c>PlayerCareerStatsSO._onStatsUpdated</c> — for live refresh after each match.
    ///   3. Optionally assign <c>_playerAchievements</c>, <c>_winStreak</c>, and
    ///      <c>_playerProgression</c> to display win rate, best streak, and level.
    ///   4. Assign Text label refs to whichever stat fields you want displayed.
    ///      Any unassigned Text field is silently skipped.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Delegates cached in Awake; no closures or allocs after Awake.
    ///   - No Update; refresh is event-driven via VoidGameEvent subscription.
    ///   - <see cref="FormatPlaytime"/> is internal static for testability via reflection.
    /// </summary>
    public sealed class CareerStatsController : MonoBehaviour
    {
        // ── Inspector — data sources ──────────────────────────────────────────

        [Header("Data Sources")]
        [Tooltip("Required. Provides TotalDamageDealt/Taken, TotalCurrencyEarned, TotalPlaytimeSeconds.")]
        [SerializeField] private PlayerCareerStatsSO _careerStats;

        [Tooltip("Optional. Provides TotalMatchesPlayed and TotalMatchesWon for win-rate display.")]
        [SerializeField] private PlayerAchievementsSO _playerAchievements;

        [Tooltip("Optional. Provides BestStreak for best-streak label.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Tooltip("Optional. Provides CurrentLevel for level label.")]
        [SerializeField] private PlayerProgressionSO _playerProgression;

        // ── Inspector — event channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("VoidGameEvent SO raised by PlayerCareerStatsSO after each RecordMatch(). " +
                 "Wire the same asset assigned to PlayerCareerStatsSO._onStatsUpdated.")]
        [SerializeField] private VoidGameEvent _onStatsUpdated;

        // ── Inspector — text labels (all optional) ────────────────────────────

        [Header("Labels (all optional)")]
        [SerializeField] private Text _matchesPlayedText;
        [SerializeField] private Text _winsText;
        [SerializeField] private Text _winRateText;
        [SerializeField] private Text _damageDealtText;
        [SerializeField] private Text _damageTakenText;
        [SerializeField] private Text _currencyEarnedText;
        [SerializeField] private Text _playtimeText;
        [SerializeField] private Text _bestStreakText;
        [SerializeField] private Text _levelText;

        // ── Private ───────────────────────────────────────────────────────────

        private System.Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onStatsUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onStatsUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates all wired Text labels from the current SO state.
        /// Safe to call when <c>_careerStats</c> is null (early-out, no throw).
        /// </summary>
        public void Refresh()
        {
            if (_careerStats == null) return;

            int played = _playerAchievements != null ? _playerAchievements.TotalMatchesPlayed : 0;
            int won    = _playerAchievements != null ? _playerAchievements.TotalMatchesWon    : 0;

            if (_matchesPlayedText != null)
                _matchesPlayedText.text = played.ToString();

            if (_winsText != null)
                _winsText.text = won.ToString();

            if (_winRateText != null)
            {
                float rate = played > 0 ? (float)won / played * 100f : 0f;
                _winRateText.text = $"{rate:F0}%";
            }

            if (_damageDealtText != null)
                _damageDealtText.text = $"{_careerStats.TotalDamageDealt:F0}";

            if (_damageTakenText != null)
                _damageTakenText.text = $"{_careerStats.TotalDamageTaken:F0}";

            if (_currencyEarnedText != null)
                _currencyEarnedText.text = _careerStats.TotalCurrencyEarned.ToString();

            if (_playtimeText != null)
                _playtimeText.text = FormatPlaytime(_careerStats.TotalPlaytimeSeconds);

            if (_bestStreakText != null && _winStreak != null)
                _bestStreakText.text = _winStreak.BestStreak.ToString();

            if (_levelText != null && _playerProgression != null)
                _levelText.text = $"Level {_playerProgression.CurrentLevel}";
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Formats a total-seconds value as "Xh Ym" (hours present) or "Ym" (minutes only).
        /// Negative input is treated as zero.
        /// Testable via reflection from <see cref="CareerStatsControllerTests"/>.
        /// </summary>
        internal static string FormatPlaytime(float totalSeconds)
        {
            if (totalSeconds < 0f) totalSeconds = 0f;
            int hours   = (int)(totalSeconds / 3600f);
            int minutes = (int)(totalSeconds % 3600f / 60f);
            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }
    }
}
