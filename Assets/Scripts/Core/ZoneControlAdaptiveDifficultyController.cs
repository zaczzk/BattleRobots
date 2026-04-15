using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that bridges <see cref="ZoneControlMatchRatingHistorySO"/>
    /// to <see cref="ZoneControlAdaptiveDifficultySO"/>, reading the latest match
    /// rating and applying it to the difficulty SO whenever the rating history changes.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Subscribes to <c>_onRatingHistoryUpdated</c>.
    ///   • On each event: reads the most recent rating from <c>_historySO</c>
    ///     and calls <c>_difficultySO.AdjustFromRating(latestRating)</c>.
    ///   • No-op when either SO is null or the history is empty.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one adaptive controller per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _difficultySO          → ZoneControlAdaptiveDifficultySO asset.
    ///   2. Assign _historySO             → ZoneControlMatchRatingHistorySO asset.
    ///   3. Assign _onRatingHistoryUpdated → ZoneControlMatchRatingHistorySO._onRatingHistoryUpdated.
    ///   4. (Optional) Call _difficultySO.Initialize(baseDuration) from the
    ///      arena bootstrapper before the first match.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAdaptiveDifficultyController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAdaptiveDifficultySO    _difficultySO;
        [SerializeField] private ZoneControlMatchRatingHistorySO     _historySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlMatchRatingHistorySO._onRatingHistoryUpdated.")]
        [SerializeField] private VoidGameEvent _onRatingHistoryUpdated;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleHistoryUpdatedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleHistoryUpdatedDelegate = HandleRatingHistoryUpdated;
        }

        private void OnEnable()
        {
            _onRatingHistoryUpdated?.RegisterCallback(_handleHistoryUpdatedDelegate);
        }

        private void OnDisable()
        {
            _onRatingHistoryUpdated?.UnregisterCallback(_handleHistoryUpdatedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the latest rating from <c>_historySO</c> and adjusts
        /// <c>_difficultySO</c> accordingly.
        /// No-op when either SO is null or the history buffer is empty.
        /// </summary>
        public void HandleRatingHistoryUpdated()
        {
            if (_difficultySO == null || _historySO == null) return;

            var ratings = _historySO.GetRatings();
            if (ratings.Count == 0) return;

            int latestRating = ratings[ratings.Count - 1];
            _difficultySO.AdjustFromRating(latestRating);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound adaptive difficulty SO (may be null).</summary>
        public ZoneControlAdaptiveDifficultySO DifficultySO => _difficultySO;

        /// <summary>The bound match rating history SO (may be null).</summary>
        public ZoneControlMatchRatingHistorySO HistorySO => _historySO;
    }
}
