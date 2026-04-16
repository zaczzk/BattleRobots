using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks how frequently each zone index has been
    /// captured during a match session.
    ///
    /// Call <see cref="RecordCapture"/> with the zero-based zone index each time a
    /// zone changes hands.  <see cref="GetHeatLevel"/> returns a normalised [0,1]
    /// value relative to the zone with the most captures (hottest zone = 1.0).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on RecordCapture / GetHeatLevel hot paths.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlHeatmap.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlHeatmap", order = 44)]
    public sealed class ZoneControlHeatmapSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Number of independently tracked capture zones.")]
        [Min(1)]
        [SerializeField] private int _maxZones = 3;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after every RecordCapture call.")]
        [SerializeField] private VoidGameEvent _onHeatmapUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int[] _captureCounts;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of zones tracked by this heatmap.</summary>
        public int ZoneCount => _maxZones;

        /// <summary>
        /// Highest capture count across all zones.
        /// Returns 0 when no captures have been recorded.
        /// </summary>
        public int MaxCaptures
        {
            get
            {
                if (_captureCounts == null) return 0;
                int max = 0;
                for (int i = 0; i < _captureCounts.Length; i++)
                    if (_captureCounts[i] > max) max = _captureCounts[i];
                return max;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the capture count for <paramref name="zoneIndex"/> and fires
        /// <see cref="_onHeatmapUpdated"/>.
        /// Out-of-range or negative indices are silently ignored.
        /// </summary>
        public void RecordCapture(int zoneIndex)
        {
            if (_captureCounts == null || zoneIndex < 0 || zoneIndex >= _captureCounts.Length)
                return;
            _captureCounts[zoneIndex]++;
            _onHeatmapUpdated?.Raise();
        }

        /// <summary>
        /// Returns the heat level for <paramref name="zoneIndex"/> normalised to [0, 1]
        /// relative to the zone with the most captures.
        /// Returns 0 when no captures have occurred or index is out of range.
        /// </summary>
        public float GetHeatLevel(int zoneIndex)
        {
            if (_captureCounts == null || zoneIndex < 0 || zoneIndex >= _captureCounts.Length)
                return 0f;
            int max = MaxCaptures;
            return max > 0 ? Mathf.Clamp01((float)_captureCounts[zoneIndex] / max) : 0f;
        }

        /// <summary>
        /// Returns the raw capture count for <paramref name="zoneIndex"/>.
        /// Returns 0 for out-of-range indices.
        /// </summary>
        public int GetCaptureCount(int zoneIndex)
        {
            if (_captureCounts == null || zoneIndex < 0 || zoneIndex >= _captureCounts.Length)
                return 0;
            return _captureCounts[zoneIndex];
        }

        /// <summary>
        /// Resets all capture counts to zero silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _captureCounts = new int[Mathf.Max(0, _maxZones)];
        }
    }
}
