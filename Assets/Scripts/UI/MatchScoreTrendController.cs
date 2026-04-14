using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Reads <see cref="ScoreHistorySO.TrendDelta"/> and displays a directional trend
    /// indicator on a UI panel: "↑ Improving", "↓ Declining", or "↔ Steady".
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   • <see cref="ScoreHistorySO.TrendDelta"/> is updated by
    ///     <see cref="ScoreHistorySO.Record"/> after each match.
    ///   • <c>_onHistoryUpdated</c> fires immediately after Record().
    ///   • This controller subscribes that event and calls <see cref="Refresh"/>.
    ///   • Refresh also runs on <c>OnEnable</c> so the panel shows correctly on entry.
    ///
    /// ── Display rules ─────────────────────────────────────────────────────────
    ///   • Fewer than 2 recorded scores → label shows "—" (insufficient data).
    ///   • TrendDelta  &gt; 0 → "↑ Improving"
    ///   • TrendDelta  &lt; 0 → "↓ Declining"
    ///   • TrendDelta == 0 → "↔ Steady"
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one trend indicator per canvas.
    ///   - All inspector fields optional — safe with no refs assigned.
    ///   - No Update/FixedUpdate — purely event-driven.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _scoreHistory      → shared ScoreHistorySO asset.
    ///   _onHistoryUpdated  → same VoidGameEvent that ScoreHistorySO fires on Record().
    ///   _trendLabel        → Text component that receives the trend string.
    ///   _trendPanel        → optional container shown/hidden based on data availability.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchScoreTrendController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("ScoreHistorySO providing the rolling score window and TrendDelta. " +
                 "Leave null to show '—' on the trend label.")]
        [SerializeField] private ScoreHistorySO _scoreHistory;

        // ── Inspector — Event ─────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by ScoreHistorySO.Record() after each match score is stored. " +
                 "Triggers Refresh(). Leave null — OnEnable still calls Refresh() once.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Text label receiving the trend string ('↑ Improving', '↓ Declining', '↔ Steady', or '—').")]
        [SerializeField] private Text _trendLabel;

        [Tooltip("Optional container panel. Shown when sufficient score data is available; " +
                 "hidden when the history is null or has fewer than 2 entries.")]
        [SerializeField] private GameObject _trendPanel;

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

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads <see cref="ScoreHistorySO.TrendDelta"/> and updates
        /// <c>_trendLabel</c> and <c>_trendPanel</c>.
        ///
        /// Called on <c>OnEnable</c> and each time <c>_onHistoryUpdated</c> fires.
        /// Safe to call with null <c>_scoreHistory</c>.
        /// </summary>
        public void Refresh()
        {
            // Insufficient data: hide panel and show dash.
            if (_scoreHistory == null || _scoreHistory.Scores.Count < 2)
            {
                _trendPanel?.SetActive(false);
                if (_trendLabel != null) _trendLabel.text = "\u2014";
                return;
            }

            _trendPanel?.SetActive(true);

            if (_trendLabel == null) return;

            int delta = _scoreHistory.TrendDelta;
            if      (delta > 0) _trendLabel.text = "\u2191 Improving";
            else if (delta < 0) _trendLabel.text = "\u2193 Declining";
            else                _trendLabel.text = "\u2194 Steady";
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ScoreHistorySO"/>. May be null.</summary>
        public ScoreHistorySO ScoreHistory => _scoreHistory;
    }
}
