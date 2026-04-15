using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that aggregates per-match zone-control results into a
    /// <see cref="ZoneControlSessionSummarySO"/> and exposes a career-stats panel.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>:
    ///     1. Reads the current <see cref="ZoneDominanceSO.PlayerZoneCount"/> as
    ///        the capture count for this match.
    ///     2. Reads <see cref="ZoneDominanceSO.HasDominance"/> for the dominance flag.
    ///     3. Reads <see cref="ZoneCaptureStreakSO.CurrentStreak"/> for the streak.
    ///     4. Calls <see cref="ZoneControlSessionSummarySO.AddMatch"/> with these
    ///        values.
    ///     5. Calls <see cref="Refresh"/> to update the UI panel.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _totalCapturedLabel   → "Total: N zones"
    ///   _avgPerMatchLabel     → "Avg: N.NN / match"
    ///   _dominanceMatchesLabel→ "Dominated: N matches"
    ///   _bestStreakLabel      → "Best streak: N"
    ///   _panel               → hidden when _summarySO is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All SO and UI refs are optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per career panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _summarySO      → ZoneControlSessionSummarySO asset.
    ///   2. Assign _onMatchEnded   → match-end VoidGameEvent channel.
    ///   3. Optionally assign _dominanceSO, _captureStreakSO, and UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSessionSummaryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSessionSummarySO _summarySO;
        [SerializeField] private ZoneDominanceSO              _dominanceSO;
        [SerializeField] private ZoneCaptureStreakSO           _captureStreakSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _totalCapturedLabel;
        [SerializeField] private Text       _avgPerMatchLabel;
        [SerializeField] private Text       _dominanceMatchesLabel;
        [SerializeField] private Text       _bestStreakLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _matchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_matchEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_matchEndedDelegate);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleMatchEnded()
        {
            if (_summarySO == null) return;

            int  captured      = _dominanceSO?.PlayerZoneCount ?? 0;
            bool hadDominance  = _dominanceSO?.HasDominance    ?? false;
            int  captureStreak = _captureStreakSO?.CurrentStreak ?? 0;

            _summarySO.AddMatch(captured, hadDominance, captureStreak);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the career stats panel from the current summary SO state.
        /// Hides the panel when <c>_summarySO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_summarySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_totalCapturedLabel != null)
                _totalCapturedLabel.text = $"Total: {_summarySO.TotalZonesCaptured} zones";

            if (_avgPerMatchLabel != null)
                _avgPerMatchLabel.text = $"Avg: {_summarySO.AverageZonesPerMatch:F2} / match";

            if (_dominanceMatchesLabel != null)
                _dominanceMatchesLabel.text = $"Dominated: {_summarySO.MatchesWithDominance} matches";

            if (_bestStreakLabel != null)
                _bestStreakLabel.text = $"Best streak: {_summarySO.BestCaptureStreak}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound career summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;

        /// <summary>The bound dominance SO (may be null).</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;

        /// <summary>The bound capture-streak SO (may be null).</summary>
        public ZoneCaptureStreakSO CaptureStreakSO => _captureStreakSO;
    }
}
