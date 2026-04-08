using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pooled AudioSource manager driven by SO event channels.
    ///
    /// Architecture:
    ///   - Fixed-size AudioSource pool allocated once in Awake; zero alloc in PlayClip or Update.
    ///   - Round-robin slot selection; steals oldest slot if all sources are busy.
    ///   - Subscribes to <see cref="AudioGameEvent"/> SFX channels via RegisterCallback (no Listener MB).
    ///   - Optional music AudioSource (looping, separate from SFX pool).
    ///   - BattleRobots.Core only — no Physics/UI namespace references.
    ///
    /// Scene wiring:
    ///   1. Add AudioManager to a persistent GameObject (e.g. GameBootstrapper).
    ///   2. Create a child AudioSource and assign it to _audioSourceTemplate (played-on-awake = false).
    ///   3. Assign one or more AudioGameEvent SO assets to _sfxChannels.
    ///   4. Optionally assign _musicClipSO + a dedicated _musicSource for BGM.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("SFX Pool")]
        [Tooltip("Template AudioSource cloned to build the pool. Disabled after Awake.")]
        [SerializeField] private AudioSource _audioSourceTemplate;

        [Tooltip("Number of pooled AudioSources. Increase if SFX overlap and cut each other off.")]
        [SerializeField, Min(1)] private int _poolSize = 8;

        [Header("SFX Event Channels")]
        [Tooltip("AudioGameEvent channels this manager subscribes to. Each Raise plays the clip payload.")]
        [SerializeField] private AudioGameEvent[] _sfxChannels;

        [Header("Music")]
        [Tooltip("Background music clip. Plays looped on Awake if assigned.")]
        [SerializeField] private AudioClipSO _musicClipSO;

        [Tooltip("Dedicated AudioSource for music. Leave null to skip BGM.")]
        [SerializeField] private AudioSource _musicSource;

        // ── Private state — fixed after Awake, zero alloc thereafter ──────────

        private AudioSource[] _pool;
        private int           _nextIndex;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildPool();
            SubscribeChannels();
            StartMusic();
        }

        private void OnDestroy()
        {
            UnsubscribeChannels();
        }

        // ── Initialisation helpers ─────────────────────────────────────────────

        private void BuildPool()
        {
            if (_audioSourceTemplate == null)
            {
                Debug.LogError("[AudioManager] _audioSourceTemplate not assigned — SFX pool cannot be built.");
                _pool = new AudioSource[0];
                return;
            }

            _pool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                _pool[i] = Instantiate(_audioSourceTemplate, transform);
                _pool[i].playOnAwake = false;
            }

            // Disable the template so it doesn't play or consume resources.
            _audioSourceTemplate.gameObject.SetActive(false);
            _nextIndex = 0;
        }

        private void SubscribeChannels()
        {
            if (_sfxChannels == null) return;
            for (int i = 0; i < _sfxChannels.Length; i++)
                _sfxChannels[i]?.RegisterCallback(PlayClip);
        }

        private void UnsubscribeChannels()
        {
            if (_sfxChannels == null) return;
            for (int i = 0; i < _sfxChannels.Length; i++)
                _sfxChannels[i]?.UnregisterCallback(PlayClip);
        }

        private void StartMusic()
        {
            if (_musicClipSO == null || _musicClipSO.Clip == null || _musicSource == null)
                return;

            _musicSource.clip   = _musicClipSO.Clip;
            _musicSource.volume = _musicClipSO.Volume;
            _musicSource.loop   = true;
            _musicSource.Play();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Plays <paramref name="clipSO"/> on the next available pooled AudioSource.
        /// Round-robin; steals the current slot (oldest play) if all sources are busy.
        /// Zero heap allocation — all operations on value types or pre-allocated objects.
        /// </summary>
        public void PlayClip(AudioClipSO clipSO)
        {
            if (clipSO == null || clipSO.Clip == null || _pool == null || _pool.Length == 0)
                return;

            AudioSource src = _pool[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _pool.Length;

            src.clip   = clipSO.Clip;
            src.volume = clipSO.Volume;   // samples Random.Range — float, no alloc
            src.pitch  = clipSO.Pitch;    // samples Random.Range — float, no alloc
            src.Play();
        }

        /// <summary>Stops all active SFX pool sources immediately.</summary>
        public void StopAllSFX()
        {
            if (_pool == null) return;
            for (int i = 0; i < _pool.Length; i++)
                _pool[i]?.Stop();
        }

        /// <summary>Pause / resume background music.</summary>
        public void SetMusicPaused(bool paused)
        {
            if (_musicSource == null) return;
            if (paused) _musicSource.Pause();
            else        _musicSource.UnPause();
        }
    }
}
