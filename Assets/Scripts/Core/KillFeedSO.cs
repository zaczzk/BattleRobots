using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that maintains a ring-buffer of recent kill events
    /// for display in a live kill-feed HUD.
    ///
    /// ── Kill-feed rules ─────────────────────────────────────────────────────────
    ///   • <see cref="Add"/> appends a <see cref="KillFeedEntry"/> to the buffer.
    ///     When the buffer is full, the oldest entry is silently evicted.
    ///   • <see cref="GetEntry"/> returns entries newest-first (index 0 = most recent).
    ///   • <see cref="Clear"/> empties the buffer.
    ///   • <see cref="_onFeedUpdated"/> fires after every Add and Clear so the UI
    ///     can refresh reactively without polling.
    ///
    /// ── KillFeedEntry ──────────────────────────────────────────────────────────
    ///   A plain serializable struct: attacker name, victim name, credit reward,
    ///   and combo count at the time of the kill. Zero-allocation to create and store.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Ring buffer allocated once in <see cref="OnEnable"/>; no per-call GC.
    ///   - SO asset is immutable at runtime — Add/Clear are the only mutators.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ UI ▶ KillFeed.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/UI/KillFeed")]
    public sealed class KillFeedSO : ScriptableObject
    {
        // ── Nested type ───────────────────────────────────────────────────────

        /// <summary>
        /// Immutable value type representing a single kill-feed event.
        /// Stored in the <see cref="KillFeedSO"/> ring buffer.
        /// </summary>
        [Serializable]
        public struct KillFeedEntry
        {
            /// <summary>Display name of the robot that dealt the killing blow.</summary>
            public string AttackerName;

            /// <summary>Display name of the robot that was destroyed.</summary>
            public string VictimName;

            /// <summary>Credit reward awarded for the kill (may be zero).</summary>
            public int Reward;

            /// <summary>Active combo multiplier at the moment of the kill (1 = no combo).</summary>
            public int ComboCount;

            public KillFeedEntry(string attacker, string victim, int reward = 0, int comboCount = 1)
            {
                AttackerName = attacker ?? string.Empty;
                VictimName   = victim   ?? string.Empty;
                Reward       = Mathf.Max(0, reward);
                ComboCount   = Mathf.Max(1, comboCount);
            }
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Buffer Settings")]
        [Tooltip("Maximum number of kill entries retained. Oldest entry is evicted when full.")]
        [SerializeField, Min(1)] private int _maxEntries = 5;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised after every Add and Clear call so the HUD can refresh reactively.")]
        [SerializeField] private VoidGameEvent _onFeedUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private KillFeedEntry[] _entries;   // ring buffer, pre-allocated in OnEnable
        private int             _head;      // index of the next write slot (mod _maxEntries)
        private int             _count;     // number of valid entries (0 … _maxEntries)

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of entries the buffer retains.</summary>
        public int MaxEntries => _maxEntries;

        /// <summary>Number of valid entries currently in the buffer. Range [0, MaxEntries].</summary>
        public int Count => _count;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _entries = new KillFeedEntry[_maxEntries];
            _head    = 0;
            _count   = 0;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends <paramref name="entry"/> to the feed.
        /// If the buffer is already full the oldest entry is silently overwritten.
        /// Fires <see cref="_onFeedUpdated"/> after each call.
        /// Zero allocation — struct copy into pre-allocated array slot.
        /// </summary>
        public void Add(KillFeedEntry entry)
        {
            EnsureBuffer();

            _entries[_head] = entry;
            _head = (_head + 1) % _maxEntries;

            if (_count < _maxEntries)
                _count++;

            _onFeedUpdated?.Raise();
        }

        /// <summary>
        /// Empties the buffer and fires <see cref="_onFeedUpdated"/>.
        /// Call at match start/end to reset the feed.
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _head  = 0;
            _onFeedUpdated?.Raise();
        }

        /// <summary>
        /// Returns the entry at <paramref name="index"/> where index 0 is the most
        /// recently added entry (newest-first order).
        /// Returns <c>default</c> when <paramref name="index"/> is out of [0, Count).
        /// Zero allocation — returns a struct by value.
        /// </summary>
        public KillFeedEntry GetEntry(int index)
        {
            if (index < 0 || index >= _count) return default;

            EnsureBuffer();

            // Most recent entry is at (_head - 1), wrapping backwards.
            int slot = ((_head - 1 - index) % _maxEntries + _maxEntries) % _maxEntries;
            return _entries[slot];
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>Ensures the buffer array is initialised (defensive: covers domain reloads).</summary>
        private void EnsureBuffer()
        {
            if (_entries == null || _entries.Length != _maxEntries)
            {
                _entries = new KillFeedEntry[_maxEntries];
                _head    = 0;
                _count   = 0;
            }
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxEntries = Mathf.Max(1, _maxEntries);
        }
#endif
    }
}
