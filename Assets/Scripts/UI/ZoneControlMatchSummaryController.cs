using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records zone-control match state into a
    /// <see cref="ZoneControlMatchSummarySO"/> at match end and then refreshes a
    /// post-match summary panel.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel              → root container; hidden when _summary is null.
    ///   _playerScoreLabel   → "P: N" (player zone-control score).
    ///   _enemyScoreLabel    → "E: N" (enemy zone-control score).
    ///   _dominanceBar       → Slider driven by DominanceRatio [0,1].
    ///   _streakLabel        → "Streak: N" (capture streak at match end).
    ///   _objectiveLabel     → "OBJECTIVE MET" / "OBJECTIVE MISSED".
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - HandleMatchEnded calls Summary.Record() then Refresh() — both actions
    ///     happen in a single frame before any subscriber reads the summary.
    ///   - Subscribes _onSummaryUpdated so external Record() calls also refresh the UI.
    ///   - All inspector refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _summary        → ZoneControlMatchSummarySO asset.
    ///   2. Assign _scoreTracker   → ZoneScoreTrackerSO asset (optional).
    ///   3. Assign _dominanceSO    → ZoneDominanceSO asset (optional).
    ///   4. Assign _streakSO       → ZoneCaptureStreakSO asset (optional).
    ///   5. Assign _objectiveSO    → ZoneObjectiveSO asset (optional).
    ///   6. Assign _onMatchEnded   → MatchManager._onMatchEnded channel.
    ///   7. Assign _onSummaryUpdated → ZoneControlMatchSummarySO._onSummaryUpdated channel.
    ///   8. Assign optional UI labels, bar, and panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchSummaryController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchSummarySO _summary;
        [SerializeField] private ZoneScoreTrackerSO        _scoreTracker;
        [SerializeField] private ZoneDominanceSO           _dominanceSO;
        [SerializeField] private ZoneCaptureStreakSO       _streakSO;
        [SerializeField] private ZoneObjectiveSO           _objectiveSO;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onSummaryUpdated;

        // ── Inspector — UI Refs ───────────────────────────────────────────────

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _playerScoreLabel;
        [SerializeField] private Text       _enemyScoreLabel;
        [SerializeField] private Slider     _dominanceBar;
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _objectiveLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onSummaryUpdated?.RegisterCallback(_refreshDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onSummaryUpdated?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Records the current zone-control state into <see cref="_summary"/> and
        /// then calls <see cref="Refresh"/> to update the UI.
        /// Null-safe — no-op when <c>_summary</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            _summary?.Record(_scoreTracker, _dominanceSO, _streakSO, _objectiveSO);
            Refresh();
        }

        /// <summary>
        /// Rebuilds all UI labels from the current <see cref="_summary"/> state.
        /// Hides the panel when <c>_summary</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_summary == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_playerScoreLabel != null)
                _playerScoreLabel.text = $"P: {Mathf.RoundToInt(_summary.PlayerScore)}";

            if (_enemyScoreLabel != null)
                _enemyScoreLabel.text = $"E: {Mathf.RoundToInt(_summary.EnemyScore)}";

            if (_dominanceBar != null)
                _dominanceBar.value = _summary.DominanceRatio;

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_summary.CaptureStreak}";

            if (_objectiveLabel != null)
                _objectiveLabel.text = _summary.ObjectiveComplete
                    ? "OBJECTIVE MET"
                    : "OBJECTIVE MISSED";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneControlMatchSummarySO"/>. May be null.</summary>
        public ZoneControlMatchSummarySO Summary => _summary;
    }
}
