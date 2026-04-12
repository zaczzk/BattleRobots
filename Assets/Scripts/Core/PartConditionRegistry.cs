using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that serves as a central registry mapping <c>partId</c> strings to
    /// their corresponding <see cref="PartConditionSO"/> assets.
    ///
    /// ── Purpose ─────────────────────────────────────────────────────────────
    ///   Enables the persistence layer (<see cref="SaveSystem"/> / <see cref="GameBootstrapper"/>)
    ///   to serialise and restore part-HP ratios without coupling to the Physics layer.
    ///   The registry is wired once in the Inspector; at runtime it provides:
    ///     • <see cref="GetCondition"/>     — O(n) lookup by partId.
    ///     • <see cref="GetDamagedParts"/>  — parts below full HP, for the repair UI.
    ///     • <see cref="TakeSnapshot"/>     — serialisable HP-ratio list for SaveData.
    ///     • <see cref="LoadSnapshot"/>     — restores HP ratios from a saved list.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO assets are immutable at runtime; only <see cref="LoadSnapshot"/> and the
    ///     Physics layer (via the individual PartConditionSO mutators) change HP state.
    ///   - <see cref="TakeSnapshot"/> and <see cref="GetDamagedParts"/> allocate once
    ///     per call — acceptable on non-hot paths (end-of-match / repair UI).
    ///   - <see cref="LoadSnapshot"/> is bootstrapper-safe: no events fired.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ PartConditionRegistry.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/PartConditionRegistry",
                     fileName = "PartConditionRegistry")]
    public sealed class PartConditionRegistry : ScriptableObject
    {
        // ── Nested types ───────────────────────────────────────────────────────

        /// <summary>
        /// Maps a <see cref="partId"/> string to its runtime <see cref="condition"/> SO.
        /// Serialised directly in the Inspector entry list.
        /// </summary>
        [Serializable]
        public struct PartConditionEntry
        {
            [Tooltip("Unique part identifier matching PartDefinition.PartId. " +
                     "Must be non-empty and unique within this registry.")]
            public string partId;

            [Tooltip("PartConditionSO asset that tracks HP for this part slot.")]
            public PartConditionSO condition;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("All registered part-condition pairs. Populate once in the Inspector; " +
                 "entries are read-only at runtime.")]
        [SerializeField] private List<PartConditionEntry> _entries = new List<PartConditionEntry>();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Read-only view of all registered entries.</summary>
        public IReadOnlyList<PartConditionEntry> Entries => _entries;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="PartConditionSO"/> registered for <paramref name="partId"/>,
        /// or <c>null</c> when the ID is unknown, null, or empty.
        /// O(n) linear scan — acceptable for the small entry counts expected.
        /// </summary>
        public PartConditionSO GetCondition(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return null;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].partId == partId)
                    return _entries[i].condition;
            }
            return null;
        }

        /// <summary>
        /// Returns all registered entries whose part HP is below MaxHP
        /// (including destroyed parts at HP = 0).
        /// Skips entries with a null <see cref="PartConditionEntry.condition"/>.
        /// Allocates a new list only when at least one damaged part exists;
        /// returns <see cref="Array.Empty{T}"/> when all parts are healthy.
        /// </summary>
        public IReadOnlyList<PartConditionEntry> GetDamagedParts()
        {
            List<PartConditionEntry> result = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.condition == null) continue;
                if (e.condition.HPRatio < 1f)
                {
                    if (result == null) result = new List<PartConditionEntry>();
                    result.Add(e);
                }
            }
            return result ?? (IReadOnlyList<PartConditionEntry>)Array.Empty<PartConditionEntry>();
        }

        /// <summary>
        /// Produces a serialisable snapshot of every registered part's HP ratio.
        /// Skips entries with a null or empty <see cref="PartConditionEntry.partId"/>
        /// or a null <see cref="PartConditionEntry.condition"/>.
        /// </summary>
        public List<PartConditionSnapshot> TakeSnapshot()
        {
            var list = new List<PartConditionSnapshot>(_entries.Count);
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (string.IsNullOrEmpty(e.partId) || e.condition == null) continue;
                list.Add(new PartConditionSnapshot
                {
                    partId  = e.partId,
                    hpRatio = e.condition.HPRatio,
                });
            }
            return list;
        }

        /// <summary>
        /// Restores HP ratios from a persisted snapshot by calling
        /// <see cref="PartConditionSO.LoadSnapshot"/> on each matched entry.
        /// Null input or null snapshot entries are silently skipped (bootstrapper-safe).
        /// Unknown part IDs are silently ignored.
        /// Does not fire any events.
        /// </summary>
        public void LoadSnapshot(List<PartConditionSnapshot> snapshots)
        {
            if (snapshots == null) return;
            for (int i = 0; i < snapshots.Count; i++)
            {
                var snap = snapshots[i];
                if (snap == null) continue;
                var condition = GetCondition(snap.partId);
                condition?.LoadSnapshot(snap.hpRatio);
            }
        }
    }
}
