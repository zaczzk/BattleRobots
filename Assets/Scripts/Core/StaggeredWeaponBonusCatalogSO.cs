using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable catalog SO that lists multiple <see cref="WeaponCombatStatsBonusSO"/>
    /// entries to be stacked by <see cref="BattleRobots.Physics.StaggeredWeaponBonusApplier"/>.
    ///
    /// ── Design ────────────────────────────────────────────────────────────────
    ///   Extends the single-bonus pattern of <see cref="WeaponCombatStatsBonusSO"/> to
    ///   support layered weapon bonuses.  The applier iterates every entry and adds the
    ///   flat damage bonus for each entry whose <see cref="WeaponCombatStatsBonusSO.RequiredWeaponType"/>
    ///   matches the player's currently equipped weapon type.  Multiple matching entries
    ///   stack additively.
    ///
    /// ── Use cases ─────────────────────────────────────────────────────────────
    ///   • A "Thermal Specialist" build that grants +10 on tier 1 AND +20 on tier 2
    ///     Thermal weapon upgrades — represented as two catalog entries.
    ///   • A mixed catalog where each damage type has its own bonus entry; only the
    ///     matching entries are applied.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • SO asset is immutable at runtime — <see cref="Bonuses"/> is read-only.
    ///   • Null entries in the inspector list are silently skipped by the applier.
    ///   • Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ StaggeredWeaponBonusCatalog.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/StaggeredWeaponBonusCatalog",
        fileName = "StaggeredWeaponBonusCatalog")]
    public sealed class StaggeredWeaponBonusCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bonus Entries")]
        [Tooltip("List of WeaponCombatStatsBonusSO entries whose flat damage bonuses " +
                 "are stacked when the equipped weapon type matches. " +
                 "Null entries are silently skipped at runtime.")]
        [SerializeField] private List<WeaponCombatStatsBonusSO> _bonuses
            = new List<WeaponCombatStatsBonusSO>();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// All bonus entries in inspector order.
        /// Null slots may appear if the Inspector list has empty entries — they
        /// are skipped by <see cref="BattleRobots.Physics.StaggeredWeaponBonusApplier"/>.
        /// </summary>
        public IReadOnlyList<WeaponCombatStatsBonusSO> Bonuses => _bonuses;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < _bonuses.Count; i++)
            {
                if (_bonuses[i] == null)
                    Debug.LogWarning(
                        $"[StaggeredWeaponBonusCatalogSO] '{name}': entry [{i}] is null — " +
                        "assign a WeaponCombatStatsBonusSO asset or remove the empty slot.");
            }
        }
#endif
    }
}
