using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that aggregates <see cref="ZoneControlHeatmapSO"/>
    /// per-zone capture counts across multiple matches, enabling a lifetime heat
    /// visualisation that spans sessions.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="AddSnapshot(int[])"/> at match end with the per-zone
    ///     capture counts from <see cref="ZoneControlHeatmapSO"/>.
    ///   • Each snapshot is deep-copied and stored in a ring buffer of capacity
    ///     <see cref="MaxSnapshots"/>.
    ///   • <see cref="GetLifetimeHeatLevel(int)"/> returns a 0–1 normalised heat
    ///     for a zone index aggregated across all stored snapshots.
    ///   • <see cref="_onHistoryUpdated"/> fires after every successful
    ///     <see cref="AddSnapshot"/> call.
    ///   • Call <see cref="Reset"/> to clear all history silently.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — snapshots live for the session only.
    ///   - Zero heap allocation on the hot-path read methods after initialisation.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlHeatmapHistory.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlHeatmapHistory", order = 49)]
    public sealed class ZoneControlHeatmapHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of match snapshots retained (ring buffer).")]
        [Min(1)]
        [SerializeField] private int _maxSnapshots = 5;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after every successful AddSnapshot call.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<int[]> _snapshots = new List<int[]>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of snapshots retained in the ring buffer.</summary>
        public int MaxSnapshots => _maxSnapshots;

        /// <summary>Number of snapshots currently stored.</summary>
        public int SnapshotCount => _snapshots.Count;

        /// <summary>
        /// Number of zones in the most recent snapshot, or 0 when no snapshots exist.
        /// </summary>
        public int ZoneCount => _snapshots.Count > 0 ? _snapshots[_snapshots.Count - 1].Length : 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a deep copy of <paramref name="captureCounts"/> to the history.
        /// Evicts the oldest snapshot when the ring buffer is full.
        /// Null or empty arrays are silently ignored.
        /// Fires <see cref="_onHistoryUpdated"/> on success.
        /// </summary>
        public void AddSnapshot(int[] captureCounts)
        {
            if (captureCounts == null || captureCounts.Length == 0) return;

            var copy = new int[captureCounts.Length];
            for (int i = 0; i < captureCounts.Length; i++)
                copy[i] = captureCounts[i];

            while (_snapshots.Count >= Mathf.Max(1, _maxSnapshots))
                _snapshots.RemoveAt(0);

            _snapshots.Add(copy);
            _onHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Returns the normalised lifetime heat level [0, 1] for
        /// <paramref name="zoneIndex"/> aggregated across all stored snapshots.
        /// Returns 0 when no snapshots exist, the index is out of range, or the
        /// aggregate total for all zones is zero.
        /// </summary>
        public float GetLifetimeHeatLevel(int zoneIndex)
        {
            if (_snapshots.Count == 0) return 0f;

            int total      = 0;
            int zoneTotal  = 0;
            int maxZoneSum = 0;

            // Sum per-zone totals across all snapshots.
            int zoneCount = _snapshots[0].Length;
            for (int z = 0; z < zoneCount; z++)
            {
                int sum = 0;
                for (int s = 0; s < _snapshots.Count; s++)
                {
                    if (z < _snapshots[s].Length)
                        sum += _snapshots[s][z];
                }

                total += sum;
                if (z == zoneIndex) zoneTotal = sum;
                if (sum > maxZoneSum) maxZoneSum = sum;
            }

            if (maxZoneSum == 0) return 0f;
            if (zoneIndex < 0 || zoneIndex >= zoneCount) return 0f;

            return (float)zoneTotal / maxZoneSum;
        }

        /// <summary>
        /// Returns a copy of the per-zone totals for quick HUD display.
        /// Each element is the sum of that zone's captures across all snapshots.
        /// Returns an empty array when no snapshots exist.
        /// </summary>
        public int[] GetLifetimeTotals()
        {
            if (_snapshots.Count == 0) return new int[0];

            int zoneCount = _snapshots[0].Length;
            int[] totals  = new int[zoneCount];

            for (int z = 0; z < zoneCount; z++)
                for (int s = 0; s < _snapshots.Count; s++)
                    if (z < _snapshots[s].Length)
                        totals[z] += _snapshots[s][z];

            return totals;
        }

        /// <summary>
        /// Clears all snapshots silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _snapshots.Clear();
        }
    }
}
