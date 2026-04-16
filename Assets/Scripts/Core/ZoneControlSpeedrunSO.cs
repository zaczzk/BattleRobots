using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Persisted entry recording the fastest time to reach a specific zone-count target.
    /// </summary>
    [Serializable]
    public sealed class ZoneControlSpeedrunEntry
    {
        [Tooltip("Number of zones captured to qualify this entry.")]
        public int ZoneCount;

        [Tooltip("Fastest elapsed time (seconds) to capture ZoneCount zones.")]
        public float BestTime;
    }

    /// <summary>
    /// Runtime ScriptableObject that tracks the fastest elapsed time to capture N zones
    /// in a zone-control match.  Call <see cref="RecordAttempt"/> with the current elapsed
    /// time and zone count at match end; only improvements replace the stored record.
    ///
    /// ── Persistence ────────────────────────────────────────────────────────────
    ///   Use <see cref="LoadSnapshot"/> / <see cref="TakeSnapshot"/> with a
    ///   bootstrapper.  <see cref="Reset"/> clears all records silently.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use LoadSnapshot for persistence.
    ///   - Zero heap allocation on RecordAttempt.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlSpeedrun.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlSpeedrun", order = 39)]
    public sealed class ZoneControlSpeedrunSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when a new personal best is set by RecordAttempt.")]
        [SerializeField] private VoidGameEvent _onNewRecord;

        [Tooltip("Raised after any state mutation (RecordAttempt, LoadSnapshot, Reset).")]
        [SerializeField] private VoidGameEvent _onSpeedrunUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        // Maps zone count → best elapsed time.
        private readonly Dictionary<int, float> _bestTimes = new Dictionary<int, float>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of distinct zone-count entries with a recorded best time.</summary>
        public int RecordCount => _bestTimes.Count;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the best elapsed time recorded for <paramref name="zoneCount"/>.
        /// Returns -1 when no record exists for that zone count.
        /// </summary>
        public float GetBestTime(int zoneCount)
        {
            if (_bestTimes.TryGetValue(zoneCount, out float t)) return t;
            return -1f;
        }

        /// <summary>
        /// Returns true if a personal best has been recorded for <paramref name="zoneCount"/>.
        /// </summary>
        public bool HasRecord(int zoneCount) => _bestTimes.ContainsKey(zoneCount);

        /// <summary>
        /// Records a match attempt.  If <paramref name="time"/> is faster than any
        /// existing record for <paramref name="zoneCount"/> (or no record exists), the
        /// record is updated, <see cref="_onNewRecord"/> fires, and
        /// <see cref="_onSpeedrunUpdated"/> fires.
        /// Negative time or non-positive zone count are ignored.
        /// </summary>
        public void RecordAttempt(float time, int zoneCount)
        {
            if (time < 0f || zoneCount <= 0) return;

            bool isNew = !_bestTimes.TryGetValue(zoneCount, out float existing)
                         || time < existing;

            if (!isNew) return;

            _bestTimes[zoneCount] = time;
            _onNewRecord?.Raise();
            _onSpeedrunUpdated?.Raise();
        }

        /// <summary>
        /// Restores persisted personal-best records from a snapshot.
        /// Bootstrapper-safe; no events fired.
        /// Null or invalid entries are skipped.
        /// </summary>
        public void LoadSnapshot(IReadOnlyList<ZoneControlSpeedrunEntry> entries)
        {
            _bestTimes.Clear();
            if (entries == null) return;
            foreach (var entry in entries)
                if (entry != null && entry.ZoneCount > 0 && entry.BestTime >= 0f)
                    _bestTimes[entry.ZoneCount] = entry.BestTime;
        }

        /// <summary>
        /// Returns all personal-best records sorted ascending by zone count.
        /// </summary>
        public IReadOnlyList<ZoneControlSpeedrunEntry> TakeSnapshot()
        {
            var list = new List<ZoneControlSpeedrunEntry>(_bestTimes.Count);
            foreach (var kv in _bestTimes)
                list.Add(new ZoneControlSpeedrunEntry { ZoneCount = kv.Key, BestTime = kv.Value });
            list.Sort((a, b) => a.ZoneCount.CompareTo(b.ZoneCount));
            return list;
        }

        /// <summary>
        /// Clears all personal-best records silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _bestTimes.Clear();
        }
    }
}
