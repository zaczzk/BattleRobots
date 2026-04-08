using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.VFX
{
    /// <summary>
    /// Listens to a <see cref="VoidGameEvent"/> death channel and spawns an explosion
    /// particle at this GameObject's world position when the signal is raised.
    ///
    /// Typical setup: attach to a robot root GameObject. Assign the <see cref="HealthSO"/>
    /// death channel (<c>_onDeath</c> VoidGameEvent) and a <see cref="ParticlePool"/>
    /// pre-loaded with the explosion prefab.
    ///
    /// Architecture notes:
    ///   • Subscribes via <see cref="VoidGameEvent.RegisterCallback"/> — no extra listener
    ///     component or Inspector event wiring required.
    ///   • Spawns at <c>transform.position</c> so the effect appears at the robot's last
    ///     known location even if the GameObject is deactivated immediately afterward.
    /// </summary>
    public sealed class DestructionVFXHandler : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("VoidGameEvent that fires when this robot dies (HealthSO._onDeath channel).")]
        [SerializeField] private VoidGameEvent _deathChannel;

        [Tooltip("ParticlePool pre-loaded with the explosion/destruction prefab.")]
        [SerializeField] private ParticlePool _explosionPool;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_deathChannel != null)
                _deathChannel.RegisterCallback(HandleDeath);
        }

        private void OnDisable()
        {
            if (_deathChannel != null)
                _deathChannel.UnregisterCallback(HandleDeath);
        }

        // ── Handler ────────────────────────────────────────────────────────────

        private void HandleDeath()
        {
            if (_explosionPool != null)
                _explosionPool.Play(transform.position, Quaternion.identity);
        }
    }
}
