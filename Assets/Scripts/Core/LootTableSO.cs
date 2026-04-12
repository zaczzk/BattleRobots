using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// One weighted entry in a <see cref="LootTableSO"/>.
    /// </summary>
    [Serializable]
    public struct LootEntry
    {
        [Tooltip("The part that can be dropped. Leave null to skip this entry.")]
        public PartDefinition part;

        [Tooltip("Relative probability weight. Higher values increase drop frequency. " +
                 "Must be > 0 for the entry to be selectable.")]
        [Min(0.01f)] public float weight;
    }

    /// <summary>
    /// Weighted loot table that drives post-match part drops.
    ///
    /// ── Drop flow ─────────────────────────────────────────────────────────────
    ///   1. After a player win, <see cref="LootDropManager"/> rolls a drop-chance
    ///      check against <see cref="WinDropChance"/>.
    ///   2. If the check passes, <see cref="RollDrop"/> is called with a seed
    ///      to deterministically select a <see cref="PartDefinition"/> from
    ///      the weighted entry list.
    ///   3. If the returned part is not already owned by the player it is added
    ///      to <see cref="PlayerInventory"/> and persisted via SaveSystem.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics / UI references.
    ///   • SO asset immutable at runtime; all state owned by the caller.
    ///   • <see cref="RollDrop"/> uses <see cref="System.Random"/> with a caller-
    ///     supplied seed for full determinism in tests.
    ///   • Null-parts and zero-weight entries are silently skipped.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ LootTable.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/LootTable",
                     fileName = "LootTableSO")]
    public sealed class LootTableSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Entries")]
        [Tooltip("Weighted list of parts that can be awarded after a win. " +
                 "Null parts and zero-weight entries are skipped during roll.")]
        [SerializeField] private List<LootEntry> _entries = new List<LootEntry>();

        [Header("Drop Probability")]
        [Tooltip("Probability [0, 1] that a loot drop is attempted at all after a win. " +
                 "0 = never drop; 1 = always attempt a roll. Default 0.3 (30 %).")]
        [SerializeField, Range(0f, 1f)] private float _winDropChance = 0.3f;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Probability [0, 1] that a drop is attempted after a player win.
        /// </summary>
        public float WinDropChance => _winDropChance;

        /// <summary>
        /// Read-only view of the raw entry list.
        /// </summary>
        public IReadOnlyList<LootEntry> Entries => _entries;

        /// <summary>
        /// True when at least one entry has a non-null part and weight &gt; 0.
        /// </summary>
        public bool HasEntries
        {
            get
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].part != null && _entries[i].weight > 0f)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Sum of all valid (non-null part, weight &gt; 0) entry weights.
        /// Returns 0 when no valid entries exist.
        /// </summary>
        public float TotalWeight
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].part != null && _entries[i].weight > 0f)
                        total += _entries[i].weight;
                }
                return total;
            }
        }

        // ── Loot roll API ─────────────────────────────────────────────────────

        /// <summary>
        /// Selects a <see cref="PartDefinition"/> using a weighted-random walk
        /// seeded with <paramref name="seed"/> for determinism.
        ///
        /// <para>
        /// Only entries with a non-null part and weight &gt; 0 are eligible.
        /// Returns <c>null</c> when no valid entries exist (empty table, all null
        /// parts, all zero weights) or when total weight is effectively zero.
        /// </para>
        /// </summary>
        /// <param name="seed">
        /// Integer seed passed to <see cref="System.Random"/> so callers can
        /// reproduce results in tests.
        /// </param>
        public PartDefinition RollDrop(int seed)
        {
            float total = TotalWeight;
            if (total <= 0f) return null;

            // Walk the cumulative distribution.
            var rng    = new System.Random(seed);
            float roll = (float)(rng.NextDouble() * total);
            float cumulative = 0f;

            for (int i = 0; i < _entries.Count; i++)
            {
                LootEntry entry = _entries[i];
                if (entry.part == null || entry.weight <= 0f) continue;

                cumulative += entry.weight;
                if (roll < cumulative)
                    return entry.part;
            }

            // Floating-point edge: roll == total exactly → return last valid entry.
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].part != null && _entries[i].weight > 0f)
                    return _entries[i].part;
            }

            return null;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].part == null)
                    Debug.LogWarning(
                        $"[LootTableSO] Entry {i} has a null part — it will be skipped during rolls.",
                        this);

                if (_entries[i].weight <= 0f)
                    Debug.LogWarning(
                        $"[LootTableSO] Entry {i} has weight ≤ 0 — it will be skipped during rolls.",
                        this);
            }

            if (_entries.Count == 0)
                Debug.LogWarning(
                    "[LootTableSO] Entry list is empty — no parts will ever be dropped.", this);
        }
#endif
    }
}
