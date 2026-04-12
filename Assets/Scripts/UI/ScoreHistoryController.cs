using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's recent match score history from a <see cref="ScoreHistorySO"/>,
    /// showing the rolling average and an improvement/decline trend indicator.
    ///
    /// ── Row layout convention ────────────────────────────────────────────────────
    ///   Each instantiated bar prefab is expected to have at least one
    ///   <see cref="UnityEngine.UI.Text"/> component and optionally one
    ///   <see cref="UnityEngine.UI.Slider"/> component (found via
    ///   <c>GetComponentsInChildren</c> in index order):
    ///     Text   [0] — the numeric score value
    ///     Slider [0] — fill amount = Mathf.Clamp01(score / 1000f) (1000-point reference scale)
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Delegates cached in Awake; no closures or allocs after Awake.
    ///   - No Update; refresh is entirely event-driven via VoidGameEvent subscription.
    ///   - All inspector fields are optional; any unassigned field is silently skipped.
    ///   - <see cref="FormatTrend"/> is internal static for testability via reflection.
    ///
    /// ── Scene wiring ────────────────────────────────────────────────────────────
    ///   1. Add this component to the score-history panel root GameObject.
    ///   2. Assign <c>_scoreHistory</c> — the same <see cref="ScoreHistorySO"/> SO
    ///      used by <see cref="MatchManager"/> and <see cref="GameBootstrapper"/>.
    ///   3. Assign <c>_onHistoryUpdated</c> — the same VoidGameEvent SO wired
    ///      to <c>ScoreHistorySO._onHistoryUpdated</c>.
    ///   4. Assign <c>_averageText</c> — shows "Avg: N" (rounded to nearest int).
    ///   5. Assign <c>_trendText</c> — shows "+N ↑", "-N ↓", or "±0" based on TrendDelta.
    ///   6. Optionally create a bar prefab with one Text and one Slider, assign it to
    ///      <c>_barPrefab</c> and the scroll content rect as <c>_listContainer</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScoreHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data Source")]
        [Tooltip("Runtime SO that holds the chronological score history. " +
                 "Leave null to suppress all display.")]
        [SerializeField] private ScoreHistorySO _scoreHistory;

        [Header("Event Channel — In")]
        [Tooltip("VoidGameEvent raised by ScoreHistorySO after Record(). " +
                 "Wire the same asset assigned to ScoreHistorySO._onHistoryUpdated. " +
                 "Leave null if the panel only needs to display data on open (no live refresh).")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("Summary Labels (optional)")]
        [Tooltip("Text component that shows the rolling average score: 'Avg: N'.")]
        [SerializeField] private Text _averageText;

        [Tooltip("Text component that shows the improvement trend: '+N ↑', '-N ↓', or '±0'.")]
        [SerializeField] private Text _trendText;

        [Header("Bar List (optional)")]
        [Tooltip("Parent transform under which bar prefabs are instantiated " +
                 "(typically a HorizontalLayoutGroup or VerticalLayoutGroup inside a ScrollView).")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab with one child Text (score value) and optionally one Slider (fill = score / 1000).")]
        [SerializeField] private GameObject _barPrefab;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the summary labels and the bar list from the current
        /// <see cref="ScoreHistorySO"/> data.
        ///
        /// Summary labels are updated even when <c>_listContainer</c> / <c>_barPrefab</c>
        /// are null.  Bar list population is skipped when either container or prefab is null.
        /// Safe to call when any optional field is null (silently skipped).
        /// </summary>
        public void Refresh()
        {
            if (_scoreHistory == null)
            {
                if (_averageText != null) _averageText.text = "Avg: —";
                if (_trendText   != null) _trendText.text   = "±0";
                return;
            }

            // ── Summary labels ─────────────────────────────────────────────────

            if (_averageText != null)
                _averageText.text = "Avg: " + Mathf.RoundToInt(_scoreHistory.AverageScore);

            if (_trendText != null)
                _trendText.text = FormatTrend(_scoreHistory.TrendDelta);

            // ── Bar list ───────────────────────────────────────────────────────

            if (_listContainer == null) return;

            // Destroy all existing bar children.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            if (_barPrefab == null) return;

            var scores = _scoreHistory.Scores;
            for (int i = 0; i < scores.Count; i++)
            {
                int score = scores[i];

                GameObject bar = Instantiate(_barPrefab, _listContainer);

                // Set score text.
                Text[] texts = bar.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0)
                    texts[0].text = score.ToString();

                // Set slider fill (normalised to a 1000-point reference scale).
                Slider[] sliders = bar.GetComponentsInChildren<Slider>(true);
                if (sliders.Length > 0)
                    sliders[0].value = Mathf.Clamp01(score / 1000f);
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Converts a TrendDelta value to a human-readable trend string.
        /// Positive delta → "+N ↑"; negative delta → "-N ↓"; zero → "±0".
        /// Testable via reflection from ScoreHistoryControllerTests.
        /// </summary>
        internal static string FormatTrend(int delta)
        {
            if (delta > 0) return $"+{delta} \u2191";   // ↑
            if (delta < 0) return $"{delta} \u2193";    // ↓
            return "\u00b10";                            // ±0
        }
    }
}
