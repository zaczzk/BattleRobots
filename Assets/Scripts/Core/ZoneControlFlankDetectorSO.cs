using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that detects a "flank" — bots capturing two or more
    /// distinct zones within a configurable time window.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="RecordBotCapture(int, float)"/> each time a bot captures a zone,
    ///   passing the zone index and current timestamp (<c>Time.time</c>).
    ///   Stale entries (older than <see cref="FlankWindow"/> seconds) are pruned on
    ///   each call.  When the number of distinct active zones reaches
    ///   <see cref="FlankZoneCount"/>, <see cref="_onFlankDetected"/> is raised.
    ///   Call <see cref="Reset"/> at match start.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlFlankDetector.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlFlankDetector", order = 53)]
    public sealed class ZoneControlFlankDetectorSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Flank Settings")]
        [Tooltip("Seconds within which captures on distinct zones trigger a flank.")]
        [Min(0.1f)]
        [SerializeField] private float _flankWindow = 3f;

        [Tooltip("Number of distinct zones that must be captured within the window to count as a flank.")]
        [Min(2)]
        [SerializeField] private int _flankZoneCount = 2;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised whenever a flank is detected.")]
        [SerializeField] private VoidGameEvent _onFlankDetected;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private Dictionary<int, float> _recentCaptures = new Dictionary<int, float>();
        private List<int>              _keysToRemove   = new List<int>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The time window in seconds within which captures are considered active.</summary>
        public float FlankWindow    => _flankWindow;

        /// <summary>Minimum number of distinct zones that must be active to trigger a flank.</summary>
        public int   FlankZoneCount => _flankZoneCount;

        /// <summary>Number of distinct zones with an active (non-stale) capture record.</summary>
        public int   ActiveZoneCount => _recentCaptures.Count;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a bot capture on <paramref name="zoneIndex"/> at <paramref name="timestamp"/>.
        /// Prunes entries older than <see cref="FlankWindow"/> seconds, updates the zone's
        /// timestamp, then fires <see cref="_onFlankDetected"/> when the active zone count
        /// reaches <see cref="FlankZoneCount"/>.
        /// </summary>
        public void RecordBotCapture(int zoneIndex, float timestamp)
        {
            // Prune stale entries
            _keysToRemove.Clear();
            foreach (var kv in _recentCaptures)
                if (timestamp - kv.Value > _flankWindow)
                    _keysToRemove.Add(kv.Key);
            foreach (int key in _keysToRemove)
                _recentCaptures.Remove(key);

            _recentCaptures[zoneIndex] = timestamp;

            if (_recentCaptures.Count >= _flankZoneCount)
                _onFlankDetected?.Raise();
        }

        /// <summary>
        /// Clears all capture records silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            if (_recentCaptures == null) _recentCaptures = new Dictionary<int, float>();
            if (_keysToRemove   == null) _keysToRemove   = new List<int>();
            _recentCaptures.Clear();
            _keysToRemove.Clear();
        }

        private void OnValidate()
        {
            _flankWindow    = Mathf.Max(0.1f, _flankWindow);
            _flankZoneCount = Mathf.Max(2, _flankZoneCount);
        }
    }
}
