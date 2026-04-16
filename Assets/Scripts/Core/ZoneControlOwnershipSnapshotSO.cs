using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that captures zone ownership state at match end
    /// and retains a ring buffer of recent snapshots.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="TakeSnapshot(ZoneControlZoneControllerCatalogSO)"/> at
    ///   match end to clone the current ownership array from the catalog.
    ///   Snapshots are stored newest-last; oldest is pruned when <see cref="MaxSnapshots"/>
    ///   is exceeded.
    ///   Use <see cref="GetSnapshot(int,int)"/> to query a specific zone within a
    ///   specific snapshot (both out-of-range → false).
    ///   Call <see cref="Reset"/> to discard all snapshots silently.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — reset on session start.
    ///   - Allocation occurs only in <see cref="TakeSnapshot"/>; hot-path queries
    ///     are allocation-free.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlOwnershipSnapshot.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlOwnershipSnapshot", order = 59)]
    public sealed class ZoneControlOwnershipSnapshotSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of match snapshots retained in the ring buffer.")]
        [Min(1)]
        [SerializeField] private int _maxSnapshots = 5;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each TakeSnapshot call.")]
        [SerializeField] private VoidGameEvent _onSnapshotTaken;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<bool[]> _snapshots = new List<bool[]>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of snapshots currently stored.</summary>
        public int SnapshotCount => _snapshots.Count;

        /// <summary>Maximum number of snapshots retained before the oldest is pruned.</summary>
        public int MaxSnapshots => _maxSnapshots;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Clones the current zone ownership state from <paramref name="catalogSO"/>
        /// and appends it to the ring buffer.
        /// Prunes the oldest entry when the buffer exceeds <see cref="MaxSnapshots"/>.
        /// Fires <see cref="_onSnapshotTaken"/>.
        /// No-op when <paramref name="catalogSO"/> is null.
        /// </summary>
        public void TakeSnapshot(ZoneControlZoneControllerCatalogSO catalogSO)
        {
            if (catalogSO == null) return;

            var snapshot = new bool[catalogSO.ZoneCount];
            for (int i = 0; i < catalogSO.ZoneCount; i++)
                snapshot[i] = catalogSO.GetZoneController(i);

            _snapshots.Add(snapshot);

            int cap = Mathf.Max(1, _maxSnapshots);
            while (_snapshots.Count > cap)
                _snapshots.RemoveAt(0);

            _onSnapshotTaken?.Raise();
        }

        /// <summary>
        /// Returns the ownership value for <paramref name="zoneIndex"/> within
        /// snapshot <paramref name="snapshotIndex"/>.
        /// Returns false for any out-of-range index.
        /// </summary>
        public bool GetSnapshot(int snapshotIndex, int zoneIndex)
        {
            if (snapshotIndex < 0 || snapshotIndex >= _snapshots.Count) return false;
            bool[] s = _snapshots[snapshotIndex];
            if (zoneIndex < 0 || zoneIndex >= s.Length) return false;
            return s[zoneIndex];
        }

        /// <summary>
        /// Clears all stored snapshots silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _snapshots.Clear();
        }

        private void OnValidate()
        {
            _maxSnapshots = Mathf.Max(1, _maxSnapshots);
        }
    }
}
