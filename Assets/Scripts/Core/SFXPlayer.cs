using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Plays one-shot SFX clips using a fixed pool of <see cref="AudioSource"/> components.
    ///
    /// Design:
    ///   • Pool is pre-allocated in Awake — zero heap allocations in the hot path.
    ///   • <see cref="Play(AudioClip)"/> selects the next available source via
    ///     round-robin; if all are busy the oldest is stolen (no silent drops).
    ///   • Global volume is driven by <see cref="_masterVolume"/>; update it via
    ///     <see cref="SetMasterVolume"/> (called from Settings).
    ///
    /// Wiring:
    ///   1. Place one SFXPlayer on a persistent GameObject (e.g. the Audio Manager).
    ///   2. Add <see cref="AudioEventListener"/> components (one per channel) on the same
    ///      or child GameObject, wire their Response UnityEvent to <c>SFXPlayer.Play</c>.
    ///
    /// Architecture rules obeyed:
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - No allocations in Play() — array index, struct copies only.
    /// </summary>
    public sealed class SFXPlayer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Number of AudioSources in the pool. Increase if SFX are being cut off.")]
        [SerializeField, Range(4, 32)] private int _poolSize = 8;

        [Tooltip("Initial master volume [0, 1]. Changeable at runtime via SetMasterVolume.")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;

        [Tooltip("AudioSource spatial blend for all pooled sources. 0 = 2D, 1 = 3D.")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0f;

        // ── Pool ──────────────────────────────────────────────────────────────

        // Fixed-size array — no List, no LINQ; zero alloc in hot path.
        private AudioSource[] _pool;
        private int _nextIndex;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _pool = new AudioSource[_poolSize];

            for (int i = 0; i < _poolSize; i++)
            {
                // Child GameObjects so sources inherit position from this transform.
                var go = new GameObject($"SFX_Source_{i}");
                go.transform.SetParent(transform, worldPositionStays: false);

                AudioSource src = go.AddComponent<AudioSource>();
                src.playOnAwake  = false;
                src.loop         = false;
                src.volume       = _masterVolume;
                src.spatialBlend = _spatialBlend;

                _pool[i] = src;
            }

            _nextIndex = 0;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Plays <paramref name="clip"/> immediately on the next available pool slot.
        /// If all slots are busy the oldest playing source is stolen.
        ///
        /// Safe to call from a UnityEvent response (AudioEventListener) or directly.
        /// No heap allocations.
        /// </summary>
        /// <param name="clip">The AudioClip to play. Null clips are silently ignored.</param>
        public void Play(AudioClip clip)
        {
            if (clip == null) return;

            // Find next free source; fall back to round-robin steal if all are busy.
            int start = _nextIndex;
            do
            {
                if (!_pool[_nextIndex].isPlaying)
                    break;  // found a free slot

                _nextIndex = (_nextIndex + 1) % _poolSize;
            }
            while (_nextIndex != start);
            // At this point _nextIndex points to either a free slot or the oldest
            // (we've looped fully and will steal whoever we started at).

            AudioSource src = _pool[_nextIndex];
            src.Stop();
            src.clip = clip;
            src.volume = _masterVolume;
            src.Play();

            _nextIndex = (_nextIndex + 1) % _poolSize;
        }

        /// <summary>
        /// Updates the master volume for all pooled sources.
        /// Call from the Settings system when the player adjusts SFX volume.
        /// </summary>
        /// <param name="volume">Normalised volume [0, 1].</param>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);

            // Apply retroactively to all sources so currently-playing clips
            // also respect the new setting immediately.
            for (int i = 0; i < _pool.Length; i++)
                _pool[i].volume = _masterVolume;
        }

        /// <summary>Current master volume (read-only).</summary>
        public float MasterVolume => _masterVolume;
    }
}
