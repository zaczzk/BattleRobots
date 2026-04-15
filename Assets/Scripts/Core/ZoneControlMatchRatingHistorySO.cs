using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that stores a ring buffer of the last
    /// <see cref="Capacity"/> match ratings (1–5 stars) and fires an event
    /// when the history changes.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Call <see cref="AddRating"/> (clamped to [1,5]) after each match.
    ///     When the buffer is full the oldest entry is dropped.
    ///   • Read back all stored ratings via <see cref="GetRatings"/>.
    ///   • Call <see cref="Reset"/> to clear the buffer (e.g. on career reset).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use a bootstrapper and
    ///     <see cref="LoadSnapshot"/> for persistence.
    ///   - Zero heap allocation on <see cref="AddRating"/> once the list
    ///     has reached capacity (one Add + one RemoveAt).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchRatingHistory.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchRatingHistory", order = 26)]
    public sealed class ZoneControlMatchRatingHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of match ratings to retain in the ring buffer.")]
        [SerializeField, Min(1)] private int _capacity = 5;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by AddRating, LoadSnapshot, and Reset.")]
        [SerializeField] private VoidGameEvent _onRatingHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<int> _ratings = new List<int>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of ratings currently stored.</summary>
        public int EntryCount => _ratings.Count;

        /// <summary>Maximum ratings retained (from inspector).</summary>
        public int Capacity => _capacity;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a read-only view of all stored ratings (oldest first).
        /// </summary>
        public IReadOnlyList<int> GetRatings() => _ratings;

        /// <summary>
        /// Appends <paramref name="rating"/> (clamped to [1,5]) to the ring buffer.
        /// When the buffer exceeds <see cref="Capacity"/> the oldest entry is removed.
        /// Fires <see cref="_onRatingHistoryUpdated"/>.
        /// </summary>
        public void AddRating(int rating)
        {
            int clamped = Mathf.Clamp(rating, 1, 5);
            _ratings.Add(clamped);
            while (_ratings.Count > _capacity)
                _ratings.RemoveAt(0);
            _onRatingHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Restores rating history from persisted data without firing any events.
        /// Each value is clamped to [1,5]; up to <see cref="Capacity"/> entries
        /// are accepted (oldest first).
        /// </summary>
        public void LoadSnapshot(IReadOnlyList<int> ratings)
        {
            _ratings.Clear();
            if (ratings == null) return;
            int limit = Mathf.Min(ratings.Count, _capacity);
            for (int i = 0; i < limit; i++)
                _ratings.Add(Mathf.Clamp(ratings[i], 1, 5));
        }

        /// <summary>
        /// Clears all stored ratings and fires <see cref="_onRatingHistoryUpdated"/>.
        /// </summary>
        public void Reset()
        {
            _ratings.Clear();
            _onRatingHistoryUpdated?.Raise();
        }
    }
}
