using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single record of one prestige event stored in <see cref="PrestigeHistorySO"/>.
    /// </summary>
    [System.Serializable]
    public struct PrestigeHistoryEntry
    {
        /// <summary>The prestige count achieved at the time of this prestige event.</summary>
        public int prestigeCount;

        /// <summary>The human-readable rank label at the time of this prestige event
        /// (e.g. "Bronze I", "Silver III", "Legend").</summary>
        public string rankLabel;
    }

    /// <summary>
    /// Runtime SO that maintains a fixed-size ring buffer of
    /// <see cref="PrestigeHistoryEntry"/> records (one per prestige event).
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Stores up to <see cref="MaxHistory"/> recent prestige events in a ring
    ///     buffer (oldest entry overwritten first when full).
    ///   • <see cref="AddEntry"/> writes a new record each time the player prestiges.
    ///   • <see cref="GetEntry(int)"/> retrieves an entry by index from newest (0) to
    ///     oldest (<see cref="Count"/> − 1).
    ///   • <see cref="GetLatest"/> is a convenience wrapper for index 0.
    ///   • <see cref="Clear"/> wipes all records.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Ring buffer initialised in OnEnable; re-initialised lazily in AddEntry.
    ///   - Zero alloc on the hot path: struct writes + integer arithmetic.
    ///   - Runtime state is NOT serialised to the SO asset.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ PrestigeHistory.
    /// Assign to <see cref="BattleRobots.UI.PostPrestigeHistoryController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/PrestigeHistory",
        fileName = "PrestigeHistorySO")]
    public sealed class PrestigeHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of prestige events to retain. " +
                 "Oldest entry is overwritten first once the buffer is full.")]
        [SerializeField, Min(1)] private int _maxHistory = 10;

        // ── Runtime state ─────────────────────────────────────────────────────

        private PrestigeHistoryEntry[] _entries;
        private int                    _head;    // next write position
        private int                    _count;   // valid entries (≤ MaxHistory)

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of prestige events retained in the ring buffer.</summary>
        public int MaxHistory => _maxHistory;

        /// <summary>Number of prestige events currently stored. Always in [0, MaxHistory].</summary>
        public int Count => _count;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            InitBuffer();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a prestige event.  When the buffer is full the oldest entry is
        /// overwritten.  The internal buffer is re-initialised lazily if its size no
        /// longer matches <see cref="MaxHistory"/>.
        /// </summary>
        /// <param name="prestigeCount">
        /// The new prestige count after the prestige action.
        /// </param>
        /// <param name="rankLabel">
        /// The human-readable rank label for <paramref name="prestigeCount"/>
        /// (from <see cref="PrestigeSystemSO.GetRankLabelForCount"/>).
        /// </param>
        public void AddEntry(int prestigeCount, string rankLabel)
        {
            if (_entries == null || _entries.Length != _maxHistory)
                InitBuffer();

            _entries[_head] = new PrestigeHistoryEntry
            {
                prestigeCount = prestigeCount,
                rankLabel     = rankLabel ?? string.Empty
            };
            _head = (_head + 1) % _maxHistory;
            if (_count < _maxHistory) _count++;
        }

        /// <summary>
        /// Retrieves a prestige entry by index, where 0 is the most recent and
        /// <see cref="Count"/> − 1 is the oldest.
        ///
        /// <para>Returns a <see cref="System.Nullable{T}"/> — null when
        /// <paramref name="indexFromNewest"/> is out of range or the buffer is empty.</para>
        /// </summary>
        public PrestigeHistoryEntry? GetEntry(int indexFromNewest)
        {
            if (_entries == null || _count == 0 || indexFromNewest < 0 || indexFromNewest >= _count)
                return null;

            // The ring-buffer write pointer (_head) points to the NEXT write position,
            // so the most recent valid entry is at (_head - 1 + max) % max.
            int physicalIndex = (_head - 1 - indexFromNewest + _maxHistory * 2) % _maxHistory;
            return _entries[physicalIndex];
        }

        /// <summary>
        /// Returns the most recently added prestige entry, or null when the buffer is empty.
        /// Equivalent to <c>GetEntry(0)</c>.
        /// </summary>
        public PrestigeHistoryEntry? GetLatest() => GetEntry(0);

        /// <summary>
        /// Wipes all stored prestige events without reallocating the underlying array.
        /// </summary>
        public void Clear()
        {
            _head  = 0;
            _count = 0;

            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                    _entries[i] = default;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void InitBuffer()
        {
            _entries = new PrestigeHistoryEntry[_maxHistory];
            _head    = 0;
            _count   = 0;
        }
    }
}
