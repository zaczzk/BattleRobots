using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that aggregates <see cref="ZoneControlHeatmapSO"/>
    /// capture-count snapshots across multiple matches, enabling a lifetime heatmap
    /// comparison alongside the current session.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="AddSnapshot(int[])"/> at match end with the current
    ///     session's per-zone capture counts.
    ///   • <see cref="GetLifetimeHeatLevel(int)"/> returns 0–1 normalised heat
    ///     for a zone relative to the most-captured zone across all snapshots.
    ///   • <see cref="GetLifetimeCount(int)"/> returns the raw summed count.
    ///   • Call <see cref="Reset"/> to clear all history (called automatically by OnEnable).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlHeatmapHistory.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlHeatmapHistory", order = 49)]
    public sealed class ZoneControlHeatmapHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of per-match snapshots to retain.")]
        [Min(1)]
        [SerializeField] private int _maxSnapshots = 10;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after a snapshot is added.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<int[]> _snapshots = new List<int[]>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of snapshots currently stored.</summary>
        public int SnapshotCount => _snapshots.Count;

        /// <summary>Maximum snapshots before oldest entries are pruned.</summary>
        public int MaxSnapshots => _maxSnapshots;

        /// <summary>Read-only view of all stored snapshots.</summary>
        public IReadOnlyList<int[]> Snapshots => _snapshots;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a deep copy of <paramref name="captureCountsSnapshot"/> to the history.
        /// Null input is silently ignored.  Oldest entry pruned when at capacity.
        /// Fires <see cref="_onHistoryUpdated"/>.
        /// </summary>
        public void AddSnapshot(int[] captureCountsSnapshot)
        {
            if (captureCountsSnapshot == null) return;

            var copy = new int[captureCountsSnapshot.Length];
            System.Array.Copy(captureCountsSnapshot, copy, captureCountsSnapshot.Length);

            if (_snapshots.Count >= _maxSnapshots)
                _snapshots.RemoveAt(0);

            _snapshots.Add(copy);
            _onHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Returns the lifetime normalised heat level [0, 1] for
        /// <paramref name="zoneIndex"/> relative to the most-captured zone across
        /// all stored snapshots.  Returns 0 when no snapshots exist or the index is
        /// out of range for all snapshots.
        /// </summary>
        public float GetLifetimeHeatLevel(int zoneIndex)
        {
            if (_snapshots.Count == 0) return 0f;

            // Determine zone count from the first snapshot; silently 0 for OOB queries.
            int zoneCount = _snapshots[0].Length;

            // Find the maximum lifetime count across all zones.
            int maxLifetime = 0;
            for (int z = 0; z < zoneCount; z++)
            {
                int total = GetLifetimeCount(z);
                if (total > maxLifetime) maxLifetime = total;
            }

            if (maxLifetime == 0) return 0f;

            return Mathf.Clamp01((float)GetLifetimeCount(zoneIndex) / maxLifetime);
        }

        /// <summary>
        /// Returns the raw lifetime capture count for <paramref name="zoneIndex"/>
        /// summed across all stored snapshots.  Returns 0 when no snapshots exist or
        /// the index is out of range.
        /// </summary>
        public int GetLifetimeCount(int zoneIndex)
        {
            int total = 0;
            foreach (var snap in _snapshots)
            {
                if (zoneIndex >= 0 && zoneIndex < snap.Length)
                    total += snap[zoneIndex];
            }
            return total;
        }

        /// <summary>
        /// Clears all stored snapshots silently. Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _snapshots.Clear();
        }
    }
}
