using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data asset that defines a flat damage bonus granted to the player's weapon
    /// when the equipped weapon's <see cref="DamageType"/> has been mastered in
    /// <see cref="DamageTypeMasterySO"/>.
    ///
    /// ── Design ────────────────────────────────────────────────────────────────
    ///   Applied once at match start by
    ///   <see cref="BattleRobots.Physics.WeaponMasteryBonusApplier"/>.
    ///   The bonus is cleared at match end so it cannot stack across matches.
    ///   Unlike <see cref="WeaponCombatStatsBonusSO"/> (which gates on a fixed type),
    ///   the mastery bonus gates on runtime mastery state — any mastered type qualifies.
    ///
    /// ── Use cases ─────────────────────────────────────────────────────────────
    ///   • Grant +15 flat damage when the player has mastered their equipped weapon type.
    ///   • Reward long-term type commitment without locking the bonus to a single type.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • SO asset is immutable at runtime — all fields are read-only properties.
    ///   • Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ WeaponMasteryBonus.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/WeaponMasteryBonus",
        fileName = "WeaponMasteryBonusSO")]
    public sealed class WeaponMasteryBonusSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bonus")]
        [Tooltip("Flat damage added to WeaponAttachmentController per fire when the " +
                 "equipped weapon type is mastered. Must be ≥ 0.")]
        [SerializeField, Min(0f)] private float _flatDamageBonus = 10f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Flat damage bonus added per hit when the equipped weapon type is mastered.
        /// Always ≥ 0.
        /// </summary>
        public float FlatDamageBonus => _flatDamageBonus;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_flatDamageBonus == 0f)
                Debug.LogWarning(
                    $"[WeaponMasteryBonusSO] '{name}' has FlatDamageBonus = 0 — " +
                    "this bonus will have no gameplay impact.", this);
        }
#endif
    }
}
