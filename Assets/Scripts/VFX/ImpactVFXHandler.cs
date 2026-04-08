using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.VFX
{
    /// <summary>
    /// Listens to a <see cref="DamageGameEvent"/> channel and spawns impact-spark
    /// particles at the hit point encoded in the <see cref="DamageInfo"/> payload.
    ///
    /// Architecture notes:
    ///   • Lives in BattleRobots.VFX — never references BattleRobots.Physics directly.
    ///   • Hit position comes from <see cref="DamageInfo.hitPoint"/> (a BattleRobots.Core
    ///     struct), which is safe to read from this namespace.
    ///   • Subscribes via <see cref="DamageGameEvent.RegisterCallback"/> so no additional
    ///     listener component or Inspector wiring is required.
    ///
    /// Setup: assign the shared DamageGameEvent SO and a <see cref="ParticlePool"/> that
    /// holds the spark prefab. One handler can serve the entire scene.
    /// </summary>
    public sealed class ImpactVFXHandler : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("SO event channel that broadcasts damage. Assign the shared DamageGameEvent asset.")]
        [SerializeField] private DamageGameEvent _damageChannel;

        [Tooltip("ParticlePool pre-loaded with the impact-spark prefab.")]
        [SerializeField] private ParticlePool _sparkPool;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_damageChannel != null)
                _damageChannel.RegisterCallback(HandleDamage);
        }

        private void OnDisable()
        {
            if (_damageChannel != null)
                _damageChannel.UnregisterCallback(HandleDamage);
        }

        // ── Handler ────────────────────────────────────────────────────────────

        private void HandleDamage(DamageInfo info)
        {
            if (_sparkPool != null)
                _sparkPool.Play(info.hitPoint, Quaternion.identity);
        }
    }
}
