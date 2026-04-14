using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single snapshot of the loadout that was active during one match.
    /// </summary>
    [Serializable]
    public struct LoadoutHistoryEntry
    {
        /// <summary>Part IDs that were equipped at match time.</summary>
        public string[] partIds;

        /// <summary>True when the player won that match.</summary>
        public bool playerWon;

        /// <summary>
        /// Unix-style timestamp (seconds since epoch) recorded at match end.
        /// 0 when not set.
        /// </summary>
        public double timestamp;
    }

    /// <summary>
    /// Ring-buffer SO that stores the last N equipped loadout snapshots
    /// (part IDs + match result).
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   • <see cref="AddEntry"/> is called by <see cref="BattleRobots.UI.LoadoutHistoryController"/>
    ///     at the end of each match.
    ///   • <see cref="GetEntry"/> retrieves entries newest-first (index 0 = latest).
    ///   • <see cref="GetLatest"/> is shorthand for GetEntry(0).
    ///   • <see cref="Clear"/> resets the buffer.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime buffer is not SO-serialised; it resets between Editor play sessions.
    ///   - Zero heap allocations in AddEntry after the buffer is initialised.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ LoadoutHistory.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/LoadoutHistory",
        fileName = "LoadoutHistorySO")]
    public sealed class LoadoutHistorySO : ScriptableObject
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Tooltip("Maximum number of loadout snapshots to retain. Must be ≥ 1.")]
        [SerializeField, Min(1)] private int _maxHistory = 5;

        // ── Runtime state (not SO-serialised) ────────────────────────────────

        private LoadoutHistoryEntry[] _entries;
        private int _head;
        private int _count;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            InitBuffer();
        }

        private void InitBuffer()
        {
            if (_entries == null || _entries.Length != _maxHistory)
            {
                _entries = new LoadoutHistoryEntry[_maxHistory];
                _head    = 0;
                _count   = 0;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Maximum number of history entries retained.</summary>
        public int MaxHistory => _maxHistory;

        /// <summary>Number of entries currently stored (0 → <see cref="MaxHistory"/>).</summary>
        public int Count => _count;

        /// <summary>
        /// Records a new loadout snapshot.  When the buffer is full the oldest
        /// entry is silently overwritten.
        /// </summary>
        /// <param name="partIds">Equipped part IDs. Null is stored as an empty array.</param>
        /// <param name="playerWon">Whether the player won the match.</param>
        /// <param name="timestamp">Unix timestamp in seconds (use 0 if unknown).</param>
        public void AddEntry(IReadOnlyList<string> partIds, bool playerWon, double timestamp)
        {
            InitBuffer();

            string[] ids;
            if (partIds == null || partIds.Count == 0)
            {
                ids = Array.Empty<string>();
            }
            else
            {
                ids = new string[partIds.Count];
                for (int i = 0; i < partIds.Count; i++)
                    ids[i] = partIds[i];
            }

            _entries[_head] = new LoadoutHistoryEntry
            {
                partIds   = ids,
                playerWon = playerWon,
                timestamp = timestamp,
            };

            _head  = (_head + 1) % _maxHistory;
            _count = Mathf.Min(_count + 1, _maxHistory);
        }

        /// <summary>
        /// Returns the entry at <paramref name="indexFromNewest"/> (0 = most recent).
        /// Returns <c>null</c> when the buffer is empty or the index is out of range.
        /// </summary>
        public LoadoutHistoryEntry? GetEntry(int indexFromNewest)
        {
            if (_entries == null || _count == 0) return null;
            if (indexFromNewest < 0 || indexFromNewest >= _count) return null;

            // newest-first: walk backwards from _head - 1
            int idx = ((_head - 1 - indexFromNewest) % _maxHistory + _maxHistory) % _maxHistory;
            return _entries[idx];
        }

        /// <summary>
        /// Convenience shorthand for <c>GetEntry(0)</c>.
        /// Returns <c>null</c> when the buffer is empty.
        /// </summary>
        public LoadoutHistoryEntry? GetLatest() => GetEntry(0);

        /// <summary>
        /// Clears all stored entries and resets the ring-buffer head to 0.
        /// </summary>
        public void Clear()
        {
            _head  = 0;
            _count = 0;
            if (_entries != null)
                Array.Clear(_entries, 0, _entries.Length);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxHistory < 1)
            {
                _maxHistory = 1;
                Debug.LogWarning("[LoadoutHistorySO] _maxHistory must be ≥ 1; clamped to 1.", this);
            }
        }
#endif
    }
}
