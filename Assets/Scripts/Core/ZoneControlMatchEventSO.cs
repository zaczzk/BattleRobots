using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Distinguishes the three types of in-match timeline events tracked by
    /// <see cref="ZoneControlMatchEventSO"/>.
    /// </summary>
    public enum ZoneControlMatchEventType
    {
        ZoneCaptured    = 0,
        HazardActivated = 1,
        ComboReached    = 2,
    }

    /// <summary>
    /// A single immutable event entry stored in the match timeline.
    /// </summary>
    [Serializable]
    public sealed class ZoneControlMatchEvent
    {
        [Tooltip("Time.time value when the event occurred.")]
        public float Timestamp;

        [Tooltip("Category of the event.")]
        public ZoneControlMatchEventType Type;

        [Tooltip("Human-readable description of the event.")]
        public string Description;
    }

    /// <summary>
    /// Runtime ScriptableObject that maintains an append-only timeline of in-match
    /// zone-control events (zone captured, hazard activated, combo reached).
    ///
    /// The internal buffer is a fixed-capacity ring: when full the oldest event is
    /// evicted to make room for the new one, keeping the list bounded and alloc-free
    /// on the hot path once the buffer is full.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation once the buffer has reached capacity.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchEvent", order = 40)]
    public sealed class ZoneControlMatchEventSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of events kept in the timeline buffer.")]
        [Min(1)]
        [SerializeField] private int _maxEvents = 20;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time a new event is added via AddEvent.")]
        [SerializeField] private VoidGameEvent _onEventAdded;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<ZoneControlMatchEvent> _events = new List<ZoneControlMatchEvent>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of events currently stored in the timeline buffer.</summary>
        public int EventCount => _events.Count;

        /// <summary>Maximum buffer capacity (inspector-configurable).</summary>
        public int MaxEvents => _maxEvents;

        /// <summary>Read-only view of the timeline, ordered oldest → newest.</summary>
        public IReadOnlyList<ZoneControlMatchEvent> Events => _events;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a new event to the timeline.
        /// When the buffer is full the oldest entry is evicted.
        /// Fires <see cref="_onEventAdded"/> after every successful insertion.
        /// </summary>
        /// <param name="timestamp">
        /// The timestamp of the event (typically <c>Time.time</c>).
        /// </param>
        /// <param name="type">Category of the event.</param>
        /// <param name="description">
        /// Human-readable description; null is stored as <see cref="string.Empty"/>.
        /// </param>
        public void AddEvent(float timestamp, ZoneControlMatchEventType type, string description)
        {
            if (_events.Count >= _maxEvents)
                _events.RemoveAt(0);

            _events.Add(new ZoneControlMatchEvent
            {
                Timestamp   = timestamp,
                Type        = type,
                Description = description ?? string.Empty,
            });

            _onEventAdded?.Raise();
        }

        /// <summary>
        /// Clears all events from the timeline buffer silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _events.Clear();
        }
    }
}
