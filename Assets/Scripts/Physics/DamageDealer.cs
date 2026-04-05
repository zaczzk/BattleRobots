using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Applies damage to the first <see cref="HealthOwner"/> found in the hierarchy of
    /// any collider this ArticulationBody part strikes, provided the collision impulse
    /// magnitude exceeds <c>_minImpulseThreshold</c>.
    ///
    /// Attach to weapon or ramming parts. The source identifier passed to the HealthSO
    /// is this GameObject's name — useful for per-weapon MatchRecord stats.
    ///
    /// Architecture notes:
    ///   - References <c>BattleRobots.Core.HealthOwner</c> (Core layer); allowed.
    ///   - No per-frame allocation: OnCollisionEnter fires only on physics contact events.
    ///   - Requires ArticulationBody so physics layer stays AB-only.
    /// </summary>
    [RequireComponent(typeof(ArticulationBody))]
    public sealed class DamageDealer : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField, Min(0f)] private float _damagePerImpact = 10f;

        [Header("Impact Gate")]
        [Tooltip("Collision impulse magnitude below which no damage is dealt. " +
                 "Prevents chip-damage from light grazes or resting contact.")]
        [SerializeField, Min(0f)] private float _minImpulseThreshold = 2f;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude < _minImpulseThreshold) return;

            HealthOwner target = collision.gameObject.GetComponentInParent<HealthOwner>();
            if (target != null)
                target.ApplyDamage(_damagePerImpact, gameObject.name);
        }
    }
}
