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

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Apply a raw damage amount (e.g. from a collision callback or projectile hit).
        /// No allocation — value type param.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_health == null)
            {
                Debug.LogWarning($"[DamageReceiver] '{name}' has no HealthSO assigned.", this);
                return;
            }
            _health.ApplyDamage(amount);
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
