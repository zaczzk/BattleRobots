using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// One entry in the zone capture history ring buffer.
    /// </summary>
    [Serializable]
    public struct ZoneCaptureHistoryEntry
    {
        /// <summary>The display identifier of the zone (from ControlZoneSO.ZoneId).</summary>
        public string zoneId;

        /// <summary>Match-elapsed time in seconds when the event occurred.</summary>
        public float timestamp;

        /// <summary>True = zone was captured; false = zone was lost.</summary>
        public bool isCapture;

        public ZoneCaptureHistoryEntry(string zoneId, float timestamp, bool isCapture)
        {
            this.zoneId    = zoneId    ?? string.Empty;
            this.timestamp = timestamp;
            this.isCapture = isCapture;
        }
    }

    /// <summary>
    /// Runtime ScriptableObject that records zone capture and loss events in a
    /// fixed-size ring buffer during a match.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Wire a <see cref="ZoneCaptureHistoryController"/> to feed
    ///     <see cref="AddEntry"/> from each zone's <c>_onCaptured</c> / <c>_onLost</c>.
    ///   • <see cref="ZoneCaptureHistoryHUDController"/> subscribes
    ///     <see cref="_onHistoryUpdated"/> to refresh the event list panel.
    ///   • Call <see cref="Clear"/> at match start to wipe the previous match's data.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Ring buffer pre-allocated on first write; zero alloc on subsequent adds.
    ///   - Runtime state not serialised — clears on domain reload.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneCaptureHistory.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneCaptureHistory", order = 19)]
    public sealed class ZoneCaptureHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of capture events retained in the ring buffer.")]
        [SerializeField, Min(1)] private int _maxEntries = 10;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each AddEntry and after Clear. " +
                 "Wire to ZoneCaptureHistoryHUDController.Refresh.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private ZoneCaptureHistoryEntry[] _entries;
        private int _head;
        private int _count;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => InitBuffer();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a zone capture or loss event in the ring buffer.
        /// Lazily re-initialises the buffer if null or size-mismatched.
        /// Fires <see cref="_onHistoryUpdated"/> after writing.
        /// Zero allocation after initial buffer creation.
        /// </summary>
        public void AddEntry(string zoneId, float timestamp, bool isCapture)
        {
            if (_entries == null || _entries.Length != _maxEntries)
                InitBuffer();

            _entries[_head] = new ZoneCaptureHistoryEntry(zoneId, timestamp, isCapture);
            _head           = (_head + 1) % _maxEntries;
            _count          = Mathf.Min(_count + 1, _maxEntries);

            _onHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Returns the entry at <paramref name="index"/> in newest-first order.
        /// Returns a default entry if <paramref name="index"/> is out of range.
        /// Zero allocation.
        /// </summary>
        public ZoneCaptureHistoryEntry GetEntry(int index)
        {
            if (_count == 0 || index < 0 || index >= _count)
                return default;

            // _head points to the next write position, so the newest entry is
            // at (_head - 1 + max) % max, the second-newest at (_head - 2 + max) % max, etc.
            int rawIndex = (_head - 1 - index + _maxEntries * 2) % _maxEntries;
            return _entries[rawIndex];
        }

        /// <summary>
        /// Clears the ring buffer and fires <see cref="_onHistoryUpdated"/>.
        /// Call at match start to discard the previous match's data.
        /// </summary>
        public void Clear()
        {
            _head  = 0;
            _count = 0;
            _onHistoryUpdated?.Raise();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of entries currently stored (0 – MaxEntries).</summary>
        public int Count => _count;

        /// <summary>Maximum entries the ring buffer can hold.</summary>
        public int MaxEntries => _maxEntries;

        /// <summary>Event raised on every AddEntry and Clear. May be null.</summary>
        public VoidGameEvent OnHistoryUpdated => _onHistoryUpdated;

        // ── Private helpers ───────────────────────────────────────────────────

        private void InitBuffer()
        {
            _entries = new ZoneCaptureHistoryEntry[_maxEntries];
            _head    = 0;
            _count   = 0;
        }
    }
}
