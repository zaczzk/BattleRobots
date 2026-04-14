using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that persists a rolling window of per-type damage totals
    /// from the last N completed matches.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────────
    ///   • Stores up to <see cref="MaxEntries"/> <see cref="MatchDamageHistoryEntry"/>
    ///     records in chronological order (oldest first).
    ///   • <see cref="AddEntry(MatchStatisticsSO)"/> appends the current match's
    ///     per-type damage totals and evicts the oldest entry when the ring overflows.
    ///   • <see cref="GetRollingAverage(DamageType)"/> returns the mean damage for a
    ///     given type across all stored entries.
    ///   • Persists across sessions via <see cref="LoadSnapshot"/> / <see cref="TakeSnapshot"/>
    ///     and <see cref="SaveData.damageHistoryEntries"/>.
    ///   • Fires optional <c>_onHistoryUpdated</c> after every <see cref="AddEntry"/> call.
    ///
    /// ── Integration ───────────────────────────────────────────────────────────────
    ///   1. Assign to <see cref="MatchManager._matchDamageHistory"/> — EndMatch() calls
    ///      AddEntry(_matchStatistics) and persists via TakeSnapshot().
    ///   2. Assign to <see cref="GameBootstrapper._matchDamageHistory"/> — LoadSnapshot()
    ///      rehydrates from <see cref="SaveData.damageHistoryEntries"/> on startup.
    ///   3. Assign to <see cref="BattleRobots.UI.PostMatchDamageHistoryController"/> for
    ///      the post-match damage-by-type history panel.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - Runtime list is never serialised to the SO asset; rehydrated from SaveData.
    ///   - Zero-alloc hot path: AddEntry / GetRollingAverage operate on existing list.
    ///   - <see cref="Reset"/> and <see cref="LoadSnapshot"/> do NOT fire events
    ///     (bootstrapper-safe).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchDamageHistory.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/MatchDamageHistory",
        fileName = "MatchDamageHistorySO")]
    public sealed class MatchDamageHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Configuration")]
        [Tooltip("Maximum number of match entries stored in the ring buffer. " +
                 "Oldest entries are evicted when the buffer is full.")]
        [SerializeField, Range(5, 20)] private int _maxEntries = 10;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised after every AddEntry call. Wire to " +
                 "PostMatchDamageHistoryController for reactive HUD updates.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<MatchDamageHistoryEntry> _entries
            = new List<MatchDamageHistoryEntry>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>Maximum number of entries kept in the ring buffer.</summary>
        public int MaxEntries => _maxEntries;

        /// <summary>Current number of stored match entries. Never exceeds <see cref="MaxEntries"/>.</summary>
        public int Count => _entries.Count;

        /// <summary>
        /// Read-only chronological view of stored entries (oldest first).
        /// </summary>
        public IReadOnlyList<MatchDamageHistoryEntry> Entries => _entries;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a new damage-history entry derived from <paramref name="stats"/>.
        /// The oldest entry is evicted when the ring exceeds <see cref="MaxEntries"/>.
        /// Fires <c>_onHistoryUpdated</c> after appending.
        ///
        /// <para>Silent no-op when <paramref name="stats"/> is <c>null</c>.</para>
        /// </summary>
        public void AddEntry(MatchStatisticsSO stats)
        {
            if (stats == null) return;

            var entry = new MatchDamageHistoryEntry
            {
                physicalDamage = stats.GetDealtByType(DamageType.Physical),
                energyDamage   = stats.GetDealtByType(DamageType.Energy),
                thermalDamage  = stats.GetDealtByType(DamageType.Thermal),
                shockDamage    = stats.GetDealtByType(DamageType.Shock),
            };

            // Evict oldest when at capacity.
            if (_entries.Count >= _maxEntries)
                _entries.RemoveAt(0);

            _entries.Add(entry);
            _onHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Returns the mean damage dealt for <paramref name="type"/> across all stored
        /// entries. Returns <c>0f</c> when the history is empty or the type is unknown.
        /// Safe division — never returns NaN.
        /// </summary>
        public float GetRollingAverage(DamageType type)
        {
            if (_entries.Count == 0) return 0f;

            float total = 0f;
            for (int i = 0; i < _entries.Count; i++)
                total += GetDamageForType(_entries[i], type);

            return total / _entries.Count;
        }

        /// <summary>
        /// Silently rehydrates the history from a <see cref="SaveData.damageHistoryEntries"/>
        /// snapshot. Bootstrapper-safe — does NOT fire any events.
        ///
        /// <para>Null input clears the history. Entries are capped at <see cref="MaxEntries"/>
        /// (most-recent kept).</para>
        /// </summary>
        public void LoadSnapshot(List<MatchDamageHistoryEntry> snapshot)
        {
            _entries.Clear();
            if (snapshot == null) return;

            // Keep the tail (most-recent) up to MaxEntries.
            int start = Mathf.Max(0, snapshot.Count - _maxEntries);
            for (int i = start; i < snapshot.Count; i++)
            {
                if (snapshot[i] != null)
                    _entries.Add(snapshot[i]);
            }
        }

        /// <summary>
        /// Returns a shallow copy of the current entries list for persistence into
        /// <see cref="SaveData.damageHistoryEntries"/>.
        /// </summary>
        public List<MatchDamageHistoryEntry> TakeSnapshot()
        {
            return new List<MatchDamageHistoryEntry>(_entries);
        }

        /// <summary>
        /// Clears all stored entries without firing any events.
        /// Intended for test resets only — does not persist.
        /// </summary>
        public void Reset()
        {
            _entries.Clear();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static float GetDamageForType(MatchDamageHistoryEntry entry, DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return entry.physicalDamage;
                case DamageType.Energy:   return entry.energyDamage;
                case DamageType.Thermal:  return entry.thermalDamage;
                case DamageType.Shock:    return entry.shockDamage;
                default:                  return 0f;
            }
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxEntries = Mathf.Clamp(_maxEntries, 5, 20);
        }
#endif
    }
}
