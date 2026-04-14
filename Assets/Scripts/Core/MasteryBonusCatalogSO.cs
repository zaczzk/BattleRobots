using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single entry in <see cref="MasteryBonusCatalogSO"/> — a bonus that becomes
    /// active once the player has mastered the specified <see cref="DamageType"/>.
    /// </summary>
    [System.Serializable]
    public struct MasteryBonusEntry
    {
        [Tooltip("Damage type that must be mastered before this bonus activates.")]
        public DamageType requiredType;

        [Tooltip("Human-readable label shown in the bonus list (e.g. 'Physical Mastery +10%').")]
        public string label;

        [Tooltip("Score bonus multiplier applied while this bonus is active. 1.0 = no bonus.")]
        [Min(1f)] public float bonusMultiplier;
    }

    /// <summary>
    /// Catalog of per-mastery-type score bonuses that become active once the player
    /// has mastered the corresponding <see cref="DamageType"/> in
    /// <see cref="DamageTypeMasterySO"/>.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Stores designer-authored <see cref="MasteryBonusEntry"/> items keyed by
    ///     <see cref="DamageType"/>.
    ///   • <see cref="IsActive"/> checks whether a single entry's required type is
    ///     currently mastered.
    ///   • <see cref="GetTotalMultiplier"/> multiplies all active bonus multipliers
    ///     together (baseline 1.0 when no bonuses are active).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO asset is immutable at runtime — entries are read-only data.
    ///   - All read paths are zero-alloc (linear scans over value-type array).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ MasteryBonusCatalog.
    /// Assign to <see cref="BattleRobots.UI.MasteryBonusCatalogController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/MasteryBonusCatalog",
        fileName = "MasteryBonusCatalogSO")]
    public sealed class MasteryBonusCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("List of mastery-gated bonus entries. " +
                 "Each entry activates when its required DamageType is mastered.")]
        [SerializeField] private MasteryBonusEntry[] _entries = new MasteryBonusEntry[0];

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of bonus entries in the catalog.</summary>
        public int Count => _entries == null ? 0 : _entries.Length;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when <paramref name="entry"/>'s
        /// <see cref="MasteryBonusEntry.requiredType"/> is currently mastered
        /// in <paramref name="mastery"/>.
        /// Returns false when <paramref name="mastery"/> is null.
        /// </summary>
        public bool IsActive(MasteryBonusEntry entry, DamageTypeMasterySO mastery)
        {
            if (mastery == null) return false;
            return mastery.IsTypeMastered(entry.requiredType);
        }

        /// <summary>
        /// Returns the product of <see cref="MasteryBonusEntry.bonusMultiplier"/> for
        /// all entries whose required type is currently mastered.
        /// Returns 1.0 when no entries are active or <paramref name="mastery"/> is null.
        /// </summary>
        public float GetTotalMultiplier(DamageTypeMasterySO mastery)
        {
            if (_entries == null) return 1f;

            float total = 1f;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (IsActive(_entries[i], mastery))
                    total *= _entries[i].bonusMultiplier;
            }
            return total;
        }

        /// <summary>
        /// Retrieves the entry at <paramref name="index"/>.
        /// Returns false when out-of-range.
        /// </summary>
        public bool TryGetEntry(int index, out MasteryBonusEntry entry)
        {
            if (_entries != null && index >= 0 && index < _entries.Length)
            {
                entry = _entries[index];
                return true;
            }
            entry = default;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_entries == null || _entries.Length == 0)
                Debug.LogWarning(
                    $"[MasteryBonusCatalogSO] No entries configured in '{name}'.", this);
        }
#endif
    }
}
