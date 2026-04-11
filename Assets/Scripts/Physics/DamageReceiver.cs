using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that routes incoming damage to a HealthSO.
    ///
    /// Usage — two ways to deliver damage:
    ///   1. Direct code call: <c>damageReceiver.TakeDamage(amount)</c>
    ///      Use from collision/trigger callbacks or projectile scripts.
    ///   2. SO event channel: add a <see cref="BattleRobots.Core.DamageGameEventListener"/>
    ///      component to the same GameObject, assign the DamageGameEvent channel,
    ///      and wire its UnityEvent response to TakeDamage(DamageInfo).
    ///      This keeps the event-bus wiring in the Inspector with no code coupling.
    ///
    /// ARCHITECTURE RULES:
    ///   • No Rigidbody — all physics via ArticulationBody.
    ///   • BattleRobots.UI must NOT reference this class.
    ///   • TakeDamage methods are allocation-free (struct / value-type params).
    /// </summary>
    public sealed class DamageReceiver : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Health")]
        [Tooltip("The HealthSO asset that tracks this robot's current health.")]
        [SerializeField] private HealthSO _health;

        [Header("Armor")]
        [Tooltip("Flat damage reduction per hit (0 = no reduction, 100 = immune). " +
                 "Set at runtime by CombatStatsApplicator from RobotCombatStats.TotalArmorRating.")]
        [SerializeField, Range(0, 100)] private int _armorRating = 0;

        [Header("Shield (optional)")]
        [Tooltip("When assigned, incoming damage is first offered to the shield. " +
                 "Only the leftover amount that the shield cannot absorb proceeds " +
                 "to armor reduction and HealthSO. Leave null for no shield.")]
        [SerializeField] private ShieldController _shield;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Current flat damage-reduction rating. Range [0, 100].</summary>
        public int ArmorRating => _armorRating;

        /// <summary>
        /// Overrides the armor rating at runtime (e.g., from CombatStatsApplicator).
        /// Values are clamped to [0, 100]. Allocation-free.
        /// </summary>
        public void SetArmorRating(int rating)
        {
            _armorRating = Mathf.Clamp(rating, 0, 100);
        }

        /// <summary>
        /// Apply a raw damage amount (e.g. from a collision callback or projectile hit).
        /// Applies flat armor reduction before forwarding to HealthSO.
        /// No allocation — value type param.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_health == null)
            {
                Debug.LogWarning($"[DamageReceiver] '{name}' has no HealthSO assigned.", this);
                return;
            }
            // Shield absorbs first; any leftover proceeds to armor + HealthSO.
            float afterShield = _shield != null ? _shield.AbsorbDamage(amount) : amount;
            float reduced     = Mathf.Max(0f, afterShield - _armorRating);
            _health.ApplyDamage(reduced);
        }

        /// <summary>
        /// Apply damage from a <see cref="DamageInfo"/> payload.
        /// Called by a DamageGameEventListener component wired in the Inspector,
        /// or directly from code when the full damage context is needed.
        /// No allocation — DamageInfo is a struct.
        /// </summary>
        public void TakeDamage(DamageInfo info)
        {
            TakeDamage(info.amount);
        }

        // ── Accessors ──────────────────────────────────────────────────────────

        /// <summary>Convenience passthrough — true if the linked HealthSO reports death.</summary>
        public bool IsDead => _health != null && _health.IsDead;

        /// <summary>Current health value from the linked HealthSO, or 0 if unassigned.</summary>
        public float CurrentHealth => _health != null ? _health.CurrentHealth : 0f;
    }
}
