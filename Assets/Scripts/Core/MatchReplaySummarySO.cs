using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single event captured during a match for timeline replay.
    /// Stores the damage type, amount, whether the player dealt the damage, and a timestamp.
    /// </summary>
    [Serializable]
    public struct MatchReplayEventEntry
    {
        /// <summary>The damage type associated with this event.</summary>
        public DamageType damageType;

        /// <summary>Amount of damage dealt or received.</summary>
        public float amount;

        /// <summary>True when the player was the source; false when the enemy was the source.</summary>
        public bool wasPlayer;

        /// <summary>Match-relative timestamp (seconds since match started).</summary>
        public double timestamp;
    }

    /// <summary>
    /// Ring-buffer SO that records match combat events for a chronological timeline
    /// displayed post-match by <see cref="BattleRobots.UI.MatchReplaySummaryController"/>.
    ///
    /// ── Ring buffer ───────────────────────────────────────────────────────────────
    ///   • Holds up to <see cref="MaxEvents"/> entries (default 50).
    ///   • Oldest entries are silently evicted when the buffer is full.
    ///   • <see cref="GetEntry"/> returns events newest-first
    ///     (index 0 = most recent).
    ///   • <see cref="Clear"/> resets head and count; does not allocate.
    ///
    /// ── Thread safety ────────────────────────────────────────────────────────────
    ///   Not thread-safe.  All calls must occur on the Unity main thread.
    ///
    /// ── Persistence ──────────────────────────────────────────────────────────────
    ///   Runtime-only — the ring buffer is NOT serialised to the SO asset.
    ///   <c>OnEnable</c> allocates a fresh buffer each time.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchReplaySummary.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/MatchReplaySummary",
        fileName = "MatchReplaySummarySO")]
    public sealed class MatchReplaySummarySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum number of events stored before the oldest entry is evicted.")]
        [SerializeField, Min(1)] private int _maxEvents = 50;

        [Tooltip("Optional VoidGameEvent raised after each AddEvent call.")]
        [SerializeField] private VoidGameEvent _onEventAdded;

        // ── Runtime ring buffer (not serialised) ─────────────────────────────

        private MatchReplayEventEntry[] _events;
        private int _head;
        private int _count;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable() => InitBuffer();

        private void InitBuffer()
        {
            _events = new MatchReplayEventEntry[_maxEvents];
            _head   = 0;
            _count  = 0;
        }

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>Maximum events this buffer can hold before evicting the oldest.</summary>
        public int MaxEvents => _maxEvents;

        /// <summary>Current number of events stored (0 … MaxEvents).</summary>
        public int Count => _count;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a new event.  When the buffer is full, the oldest entry is evicted.
        /// Fires <c>_onEventAdded</c> after each successful write.
        /// </summary>
        public void AddEvent(DamageType damageType, float amount, bool wasPlayer, double timestamp)
        {
            if (_events == null || _events.Length != _maxEvents) InitBuffer();

            _events[_head] = new MatchReplayEventEntry
            {
                damageType = damageType,
                amount     = amount,
                wasPlayer  = wasPlayer,
                timestamp  = timestamp,
            };

            _head  = (_head + 1) % _maxEvents;
            _count = Mathf.Min(_count + 1, _maxEvents);
            _onEventAdded?.Raise();
        }

        /// <summary>
        /// Returns the event at <paramref name="indexFromNewest"/> (0 = most recent).
        /// Returns null when the buffer is empty or the index is out of range.
        /// </summary>
        public MatchReplayEventEntry? GetEntry(int indexFromNewest)
        {
            if (_count == 0 || indexFromNewest < 0 || indexFromNewest >= _count)
                return null;

            int i = ((_head - 1 - indexFromNewest) % _maxEvents + _maxEvents) % _maxEvents;
            return _events[i];
        }

        /// <summary>
        /// Clears the buffer without reallocating.  Does not fire any events.
        /// </summary>
        public void Clear()
        {
            _head  = 0;
            _count = 0;
            if (_events != null)
                Array.Clear(_events, 0, _events.Length);
        }
    }
}
