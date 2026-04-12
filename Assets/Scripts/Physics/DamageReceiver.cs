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

        [Header("Status Effects (optional)")]
        [Tooltip("StatusEffectController on this robot. When assigned and a DamageInfo " +
                 "carries a non-null statusEffect field, ApplyEffect() is called automatically " +
                 "inside TakeDamage(DamageInfo). Also used by TriggerStatusEffect() for " +
                 "direct effect application (e.g. from HazardZone or power-up triggers). " +
                 "Leave null to skip status-effect processing (backwards-compatible).")]
        [SerializeField] private StatusEffectController _statusEffectController;

        [Header("Part Health (optional)")]
        [Tooltip("PartHealthSystem on this robot root. When assigned, each successful hit " +
                 "also distributes the post-armor damage to a randomly chosen living part " +
                 "via PartHealthSystem.DistributeDamage(). Leave null to skip part-condition " +
                 "tracking (backwards-compatible — all existing callers unaffected).")]
        [SerializeField] private PartHealthSystem _partHealthSystem;

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
            _partHealthSystem?.DistributeDamage(reduced);
        }

        /// <summary>
        /// Apply damage from a <see cref="DamageInfo"/> payload.
        /// Called by a DamageGameEventListener component wired in the Inspector,
        /// or directly from code when the full damage context is needed.
        ///
        /// When <paramref name="info"/>.<see cref="DamageInfo.statusEffect"/> is non-null,
        /// the effect is automatically routed to the optional
        /// <see cref="_statusEffectController"/> so that a single hit can deal damage
        /// and apply a Burn / Stun / Slow simultaneously.
        ///
        /// No allocation — DamageInfo is a struct; StatusEffectSO is a cached reference.
        /// </summary>
        public void TakeDamage(DamageInfo info)
        {
            TakeDamage(info.amount);

            // Route optional status effect carried in the damage payload.
            if (info.statusEffect != null)
                _statusEffectController?.ApplyEffect(info.statusEffect);
        }

        /// <summary>
        /// Directly apply a status effect to this robot without dealing damage.
        /// Useful for HazardZone or power-up triggers that inflict effects independently
        /// of a damage hit.
        /// Delegates to <see cref="StatusEffectController.ApplyEffect"/>; no-op when no
        /// controller is assigned. Allocation-free — reference-type param, no boxing.
        /// </summary>
        public void TriggerStatusEffect(StatusEffectSO effect)
        {
            _statusEffectController?.ApplyEffect(effect);
        }

        /// <summary>
        /// Restores health by <paramref name="amount"/> (e.g., from a power-up pickup).
        /// Delegates to <see cref="HealthSO.Heal"/>; no-ops when no HealthSO is assigned
        /// or the robot is already dead. Allocation-free — value type param.
        /// </summary>
        public void Heal(float amount)
        {
            _health?.Heal(amount);
        }

        /// <summary>
        /// Instantly restores shield HP by <paramref name="amount"/> (e.g., from a pickup).
        /// Delegates to <see cref="ShieldController.RestoreShield"/>; no-ops when no
        /// ShieldController is assigned. Bypasses the recharge delay timer.
        /// Allocation-free — value type param.
        /// </summary>
        public void RestoreShield(float amount)
        {
            _shield?.RestoreShield(amount);
        }

        // ── Accessors ──────────────────────────────────────────────────────────

        /// <summary>Convenience passthrough — true if the linked HealthSO reports death.</summary>
        public bool IsDead => _health != null && _health.IsDead;

        /// <summary>Current health value from the linked HealthSO, or 0 if unassigned.</summary>
        public float CurrentHealth => _health != null ? _health.CurrentHealth : 0f;
    }
}
