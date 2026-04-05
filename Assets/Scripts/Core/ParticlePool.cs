using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Simple fixed-capacity pool for <see cref="ParticleSystem"/> prefabs.
    /// Allocates all instances at startup; Play() recycles the oldest active
    /// particle when the pool is exhausted — never allocates at runtime.
    ///
    /// Usage:
    ///   var pool = new ParticlePool(prefab, capacity, parent);
    ///   pool.Play(position, rotation);   // zero-alloc hot path
    ///   pool.Dispose();                  // called from OnDestroy of owning MB
    /// </summary>
    public sealed class ParticlePool
    {
        private readonly ParticleSystem[] _instances;
        private int _nextIndex;

        /// <summary>
        /// Pre-warms the pool by instantiating <paramref name="capacity"/> copies
        /// of <paramref name="prefab"/> under <paramref name="parent"/>.
        /// </summary>
        public ParticlePool(ParticleSystem prefab, int capacity, Transform parent)
        {
            if (prefab == null)
                throw new System.ArgumentNullException(nameof(prefab));

            capacity = Mathf.Max(1, capacity);
            _instances = new ParticleSystem[capacity];

            for (int i = 0; i < capacity; i++)
            {
                ParticleSystem ps = Object.Instantiate(prefab, parent);
                ps.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                _instances[i] = ps;
            }
        }

        /// <summary>
        /// Plays the next available (or oldest) particle system at the given world
        /// position and rotation. Zero heap allocations.
        /// </summary>
        public void Play(Vector3 worldPosition, Quaternion worldRotation)
        {
            ParticleSystem ps = _instances[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _instances.Length;

            // Stop and reposition before replaying.
            ps.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
            Transform t = ps.transform;
            t.SetPositionAndRotation(worldPosition, worldRotation);
            ps.gameObject.SetActive(true);
            ps.Play(withChildren: true);
        }

        /// <summary>
        /// Destroys all pooled instances. Call from the owning MonoBehaviour's OnDestroy.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _instances.Length; i++)
            {
                if (_instances[i] != null)
                    Object.Destroy(_instances[i].gameObject);
                _instances[i] = null;
            }
        }
    }
}
