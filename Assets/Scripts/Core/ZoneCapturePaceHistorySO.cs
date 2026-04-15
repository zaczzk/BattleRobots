using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that stores a ring buffer of the last
    /// <see cref="Capacity"/> per-match average captures-per-minute readings and
    /// fires an event when the history changes.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Call <see cref="AddPaceReading"/> (clamped to ≥ 0) at the end of each
    ///     match, passing the average captures-per-minute for that match.
    ///     When the buffer is full the oldest entry is dropped.
    ///   • Read back stored readings via <see cref="GetReadings"/>.
    ///   • Call <see cref="LoadSnapshot"/> from a bootstrapper to restore persisted
    ///     pace history without triggering events.
    ///   • Call <see cref="Reset"/> to clear all readings (e.g. on career reset).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use LoadSnapshot for persistence.
    ///   - Zero heap allocation on AddPaceReading once the buffer is at capacity
    ///     (one Add + one RemoveAt).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneCapturePaceHistory.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneCapturePaceHistory", order = 27)]
    public sealed class ZoneCapturePaceHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of pace readings to retain in the ring buffer.")]
        [SerializeField, Min(1)] private int _capacity = 5;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by AddPaceReading, LoadSnapshot, and Reset.")]
        [SerializeField] private VoidGameEvent _onPaceHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<float> _readings = new List<float>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of pace readings currently stored.</summary>
        public int EntryCount => _readings.Count;

        /// <summary>Maximum readings retained (from inspector).</summary>
        public int Capacity => _capacity;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a read-only view of all stored pace readings (oldest first).
        /// </summary>
        public IReadOnlyList<float> GetReadings() => _readings;

        /// <summary>
        /// Appends <paramref name="rate"/> (clamped to ≥ 0) to the ring buffer.
        /// When the buffer exceeds <see cref="Capacity"/> the oldest entry is removed.
        /// Fires <see cref="_onPaceHistoryUpdated"/>.
        /// </summary>
        public void AddPaceReading(float rate)
        {
            float clamped = Mathf.Max(0f, rate);
            _readings.Add(clamped);
            while (_readings.Count > _capacity)
                _readings.RemoveAt(0);
            _onPaceHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Restores pace history from persisted data without firing any events.
        /// Each value is clamped to ≥ 0; up to <see cref="Capacity"/> entries
        /// are accepted (oldest first).
        /// </summary>
        public void LoadSnapshot(IReadOnlyList<float> readings)
        {
            _readings.Clear();
            if (readings == null) return;
            int limit = Mathf.Min(readings.Count, _capacity);
            for (int i = 0; i < limit; i++)
                _readings.Add(Mathf.Max(0f, readings[i]));
        }

        /// <summary>
        /// Clears all stored readings and fires <see cref="_onPaceHistoryUpdated"/>.
        /// </summary>
        public void Reset()
        {
            _readings.Clear();
            _onPaceHistoryUpdated?.Raise();
        }
    }
}
