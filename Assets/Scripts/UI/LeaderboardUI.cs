using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Aggregate stats dashboard — shows wins, losses, win rate, average
    /// damage dealt/taken, total earnings, and match count.
    ///
    /// Behaviour:
    ///   • <c>OnEnable</c> calls <see cref="SaveSystem.Load"/> once, computes
    ///     <see cref="LeaderboardStats"/> in a single pass, and writes all labels.
    ///   • No <c>Update</c> override — the panel is driven entirely by <c>OnEnable</c>.
    ///   • A "No matches played yet" empty-state panel is shown when history is empty.
    ///
    /// Architecture rules observed:
    ///   • <c>BattleRobots.UI</c> namespace; no <c>BattleRobots.Physics</c> references.
    ///   • No heap allocations after the initial <c>LeaderboardStats.Compute</c> pass.
    ///
    /// Inspector wiring checklist:
    ///   □ _matchCountLabel      → Text
    ///   □ _winsLabel            → Text
    ///   □ _lossesLabel          → Text
    ///   □ _winRateLabel         → Text
    ///   □ _avgDamageDealtLabel  → Text
    ///   □ _avgDamageTakenLabel  → Text
    ///   □ _totalEarningsLabel   → Text
    ///   □ _avgDurationLabel     → Text  (optional)
    ///   □ _statsPanel           → GameObject (hidden when no history)
    ///   □ _emptyStatePanel      → GameObject (shown when no history)
    ///   □ _closeButton          → Button  (hides this panel on click)
    /// </summary>
    public sealed class LeaderboardUI : MonoBehaviour
    {
        // ── Inspector — stat labels ───────────────────────────────────────────

        [Header("Stat Labels")]
        [SerializeField] private Text _matchCountLabel;
        [SerializeField] private Text _winsLabel;
        [SerializeField] private Text _lossesLabel;
        [SerializeField] private Text _winRateLabel;
        [SerializeField] private Text _avgDamageDealtLabel;
        [SerializeField] private Text _avgDamageTakenLabel;
        [SerializeField] private Text _totalEarningsLabel;

        [Tooltip("Optional. Displays average match duration in mm:ss format.")]
        [SerializeField] private Text _avgDurationLabel;

        // ── Inspector — panels & navigation ──────────────────────────────────

        [Header("Panels")]
        [Tooltip("Root of the stats content. Hidden when there are no matches.")]
        [SerializeField] private GameObject _statsPanel;

        [Tooltip("Shown in place of stats when match history is empty.")]
        [SerializeField] private GameObject _emptyStatePanel;

        [Header("Navigation")]
        [Tooltip("Hides this panel when clicked.")]
        [SerializeField] private Button _closeButton;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        /// <summary>
        /// Rebuilds all labels each time this panel becomes visible so they always
        /// reflect the latest saved data (e.g. immediately after a match).
        /// </summary>
        private void OnEnable()
        {
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reloads the save file and repopulates all stat labels.
        /// Safe to call externally (e.g. from a "Refresh" button).
        /// </summary>
        public void Refresh()
        {
            SaveData data  = SaveSystem.Load();
            LeaderboardStats stats = LeaderboardStats.Compute(data.matchHistory);

            bool hasMatches = stats.MatchCount > 0;

            if (_statsPanel    != null) _statsPanel.SetActive(hasMatches);
            if (_emptyStatePanel != null) _emptyStatePanel.SetActive(!hasMatches);

            if (!hasMatches) return;

            PopulateLabels(stats);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void PopulateLabels(in LeaderboardStats stats)
        {
            if (_matchCountLabel != null)
                _matchCountLabel.text = $"Matches: {stats.MatchCount}";

            if (_winsLabel != null)
                _winsLabel.text = $"Wins: {stats.Wins}";

            if (_lossesLabel != null)
                _lossesLabel.text = $"Losses: {stats.Losses}";

            if (_winRateLabel != null)
                _winRateLabel.text = $"Win Rate: {stats.WinRatePercent:F1}%";

            if (_avgDamageDealtLabel != null)
                _avgDamageDealtLabel.text = $"Avg Damage Dealt: {stats.AvgDamageDealt:F0}";

            if (_avgDamageTakenLabel != null)
                _avgDamageTakenLabel.text = $"Avg Damage Taken: {stats.AvgDamageTaken:F0}";

            if (_totalEarningsLabel != null)
                _totalEarningsLabel.text = $"Total Earned: {stats.TotalEarnings} cr";

            if (_avgDurationLabel != null)
            {
                int totalSecs = (int)stats.AvgDurationSeconds;
                _avgDurationLabel.text = $"Avg Duration: {totalSecs / 60}:{totalSecs % 60:D2}";
            }
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }
    }
}
