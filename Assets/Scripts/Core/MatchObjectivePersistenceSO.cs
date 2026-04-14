using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Ring-buffer ScriptableObject that persists per-match bonus-objective outcomes
    /// (title, completed/expired flag, reward earned).
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. Call <see cref="Reset"/> at match start (or via VoidGameEventListener).
    ///   2. Call <see cref="Record"/> after each objective resolves.
    ///   3. Subscribe <see cref="_onHistoryUpdated"/> to refresh the HUD.
    ///
    /// ── Ring-buffer semantics ────────────────────────────────────────────────────
    ///   When <see cref="Count"/> reaches <see cref="MaxEntries"/> the oldest entry
    ///   (index 0) is evicted before the new entry is appended, keeping the buffer at
    ///   a fixed ceiling.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Record() allocates one List node per call (unavoidable ring-buffer append);
    ///     zero alloc on read paths.
    ///   - SO assets are immutable at runtime — only the _entries list mutates.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchObjectivePersistence.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/MatchObjectivePersistence")]
    public sealed class MatchObjectivePersistenceSO : ScriptableObject
    {
        // ── Nested types ──────────────────────────────────────────────────────

        /// <summary>
        /// Immutable record of a single bonus-objective outcome for one match.
        /// </summary>
        [Serializable]
        public struct MatchObjectiveEntry
        {
            /// <summary>Human-readable objective title.</summary>
            public string title;

            /// <summary>True if the objective was completed; false if it expired.</summary>
            public bool completed;

            /// <summary>Currency reward awarded (0 when objective was not completed).</summary>
            public int reward;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of entries to retain. Oldest entries are evicted when full.")]
        [SerializeField, Range(5, 50)] private int _maxEntries = 20;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised after every Record and Reset call.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<MatchObjectiveEntry> _entries = new List<MatchObjectiveEntry>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of entries the ring-buffer can hold.</summary>
        public int MaxEntries => _maxEntries;

        /// <summary>Number of entries currently stored.</summary>
        public int Count => _entries.Count;

        /// <summary>
        /// Read-only ordered view of stored entries, oldest first (index 0).
        /// Use reverse iteration (Count-1 down to 0) for newest-first display.
        /// </summary>
        public IReadOnlyList<MatchObjectiveEntry> Entries => _entries;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a new objective outcome to the history.
        /// When the buffer is full the oldest entry is evicted first.
        /// Fires <c>_onHistoryUpdated</c>.
        /// </summary>
        /// <param name="title">Human-readable objective title.</param>
        /// <param name="completed">True if the objective was completed; false if expired.</param>
        /// <param name="reward">Currency reward awarded (0 when not completed).</param>
        public void Record(string title, bool completed, int reward)
        {
            if (_entries.Count >= _maxEntries)
                _entries.RemoveAt(0);

            _entries.Add(new MatchObjectiveEntry
            {
                title     = title,
                completed = completed,
                reward    = reward
            });

            _onHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Clears all stored entries and fires <c>_onHistoryUpdated</c>.
        /// Call at match start via a VoidGameEventListener.
        /// </summary>
        public void Reset()
        {
            _entries.Clear();
            _onHistoryUpdated?.Raise();
        }
    }
}
