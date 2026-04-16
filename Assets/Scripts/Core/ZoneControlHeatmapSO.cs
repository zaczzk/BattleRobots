using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks per-zone capture frequency across a
    /// zone-control match, enabling a heat-map visualisation of capture hotspots.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="RecordCapture(int)"/> with the captured zone index after
    ///     each player or bot zone-capture event.
    ///   • <see cref="GetHeatLevel(int)"/> returns a 0–1 normalised heat for a zone
    ///     relative to the most-captured zone (0 = cold, 1 = hottest zone).
    ///   • <see cref="GetCaptureCount(int)"/> returns the raw integer count.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlHeatmap.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlHeatmap", order = 44)]
    public sealed class ZoneControlHeatmapSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Number of zones tracked. Must match the number of ControlZoneSO assets.")]
        [Min(1)]
        [SerializeField] private int _zoneCount = 4;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after every successful RecordCapture and after Reset.")]
        [SerializeField] private VoidGameEvent _onHeatmapUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int[] _captureCounts;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of zones being tracked.</summary>
        public int ZoneCount => _zoneCount;

        /// <summary>
        /// Maximum single-zone capture count across all zones.
        /// 0 when no captures have been recorded.
        /// </summary>
        public int MaxCaptureCount
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
        /// Records a zone capture at <paramref name="zoneIndex"/>.
        /// Out-of-range indices are silently ignored.
        /// Fires <see cref="_onHeatmapUpdated"/> on success.
        /// </summary>
        public void RecordCapture(int zoneIndex)
        {
            if (_captureCounts == null || zoneIndex < 0 || zoneIndex >= _captureCounts.Length)
                return;

            _captureCounts[zoneIndex]++;
            _onHeatmapUpdated?.Raise();
        }

        /// <summary>
        /// Returns the normalised heat level for the zone at <paramref name="zoneIndex"/>
        /// in the range [0, 1], where 1 represents the most-captured zone.
        /// Returns 0 when the index is out of range or no captures have been recorded.
        /// </summary>
        public float GetHeatLevel(int zoneIndex)
        {
            if (_captureCounts == null || zoneIndex < 0 || zoneIndex >= _captureCounts.Length)
                return 0f;

            int maxCount = MaxCaptureCount;
            if (maxCount == 0) return 0f;

            return (float)_captureCounts[zoneIndex] / maxCount;
        }

        /// <summary>
        /// Returns the raw capture count for the zone at <paramref name="zoneIndex"/>.
        /// Returns 0 when the index is out of range.
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
            _captureCounts = new int[Mathf.Max(1, _zoneCount)];
        }
    }
}
