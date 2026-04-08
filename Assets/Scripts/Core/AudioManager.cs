using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Central audio manager that maintains a pool of <see cref="AudioSource"/> components
    /// and plays sounds in response to <see cref="AudioEvent"/> SO channels.
    ///
    /// Pool strategy: round-robin over a fixed array.  If all sources are busy the
    /// oldest playing source is stolen — acceptable for game SFX (priority system
    /// can be added later without breaking the interface).
    ///
    /// ARCHITECTURE RULES enforced here:
    ///   • Pool pre-allocated in Awake — zero heap allocation after startup.
    ///   • No BattleRobots.Physics or BattleRobots.UI references.
    ///   • AudioEvent SO channels for all subscriptions (OnEnable/OnDisable pattern).
    ///   • Update loop NOT used — driven entirely by callbacks.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Pool")]
        [Tooltip("Number of AudioSource slots pre-allocated at startup. " +
                 "If all are busy the oldest playing source is stolen.")]
        [SerializeField, Min(1)] private int _poolSize = 16;

        [Header("AudioEvents")]
        [Tooltip("SO audio event channels this manager listens to. " +
                 "Add one entry per distinct game sound.")]
        [SerializeField] private AudioEvent[] _events;

        // ── Private pool (fixed array — no alloc after Awake) ─────────────────
        private AudioSource[] _sources;
        private int           _nextIndex;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _sources = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                // Each source lives on a dedicated child GameObject so they can be
                // individually inspected and do not interfere with spatial audio setups.
                var go = new GameObject($"AudioSource_{i:D2}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sources[i] = src;
            }
        }

        private void OnEnable()
        {
            if (_events == null) return;
            for (int i = 0; i < _events.Length; i++)
                _events[i]?.RegisterCallback(OnAudioEvent);
        }

        private void OnDisable()
        {
            if (_events == null) return;
            for (int i = 0; i < _events.Length; i++)
                _events[i]?.UnregisterCallback(OnAudioEvent);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Play an <see cref="AudioEvent"/> directly (bypasses the SO callback chain).
        /// Useful for one-shot imperative plays from code (e.g., scripted sequences).
        /// </summary>
        public void Play(AudioEvent audioEvent)
        {
            if (audioEvent == null) return;
            AudioClip clip = audioEvent.PickClip();
            if (clip == null) return;
            float pitch = Random.Range(audioEvent.PitchMin, audioEvent.PitchMax);
            PlayClip(clip, audioEvent.Volume, pitch);
        }

        /// <summary>
        /// Play a raw <see cref="AudioClip"/> at the given volume and pitch.
        /// Acquires the next available pool slot (round-robin with steal fallback).
        /// </summary>
        public void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            AudioSource src = AcquireSource();
            src.clip   = clip;
            src.volume = Mathf.Clamp01(volume);
            src.pitch  = pitch;
            src.Play();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnAudioEvent(AudioEvent audioEvent) => Play(audioEvent);

        /// <summary>
        /// Returns the next idle AudioSource from the round-robin pool.
        /// Falls back to stealing the current slot when all sources are active.
        /// No allocation — iterates a fixed array of value-type pointers.
        /// </summary>
        private AudioSource AcquireSource()
        {
            int start = _nextIndex;
            do
            {
                AudioSource candidate = _sources[_nextIndex];
                _nextIndex = (_nextIndex + 1) % _poolSize;
                if (!candidate.isPlaying) return candidate;
            }
            while (_nextIndex != start);

            // All slots busy — steal the one at the current index (oldest by round-robin order).
            AudioSource stolen = _sources[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _poolSize;
            stolen.Stop();
            return stolen;
        }
    }
}
