using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// State-machine background music controller driven by SO VoidGameEvent channels.
    ///
    /// Each <see cref="BGMBinding"/> entry pairs a <see cref="VoidGameEvent"/> trigger
    /// (e.g. "MatchStarted", "MainMenuOpened", "MatchEnded") with an
    /// <see cref="AudioClipSO"/> to start playing when that event fires.
    /// The controller owns a single looping <see cref="AudioSource"/> for BGM
    /// (separate from the SFX pool managed by <see cref="SFXPlayer"/> or
    /// <see cref="AudioManager"/>).
    ///
    /// Architecture rules observed:
    ///   • <c>BattleRobots.Core</c> namespace — no Physics or UI references.
    ///   • Delegates are pre-allocated in <c>Awake</c> (one closure per binding slot);
    ///     <c>OnEnable</c> / <c>OnDisable</c> only pass already-allocated references — zero alloc.
    ///   • No <c>Update</c> or <c>FixedUpdate</c> hot path.
    ///   • BGM crossfade is instant (hard cut) — no coroutine, no heap allocation.
    ///
    /// Scene wiring:
    ///   1. Add BGMController to a persistent GameObject (e.g. GameBootstrapper's child).
    ///   2. Assign a dedicated <c>_musicSource</c> AudioSource (loop = true, playOnAwake = false).
    ///   3. Populate <c>_bindings</c>: one entry per game state with its trigger SO + BGM clip SO.
    ///   4. Set <c>_masterVolume</c> to the desired BGM volume (can be changed at runtime).
    ///   5. Optionally assign <c>_defaultClip</c> to start BGM immediately on Awake.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BGMController : MonoBehaviour
    {
        // ── Inner types ────────────────────────────────────────────────────────

        /// <summary>
        /// Associates a state-entry VoidGameEvent trigger with the BGM clip to play.
        /// </summary>
        [Serializable]
        public struct BGMBinding
        {
            [Tooltip("VoidGameEvent SO that signals entry into this BGM state "
                   + "(e.g. MatchStarted, MainMenuLoaded, MatchEnded).")]
            public VoidGameEvent trigger;

            [Tooltip("AudioClipSO to start playing when the trigger fires. "
                   + "Null = silence (stops current BGM without starting a new track).")]
            public AudioClipSO clip;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Audio Source")]
        [Tooltip("Dedicated looping AudioSource for background music. "
               + "Set playOnAwake = false, loop = true in the Inspector.")]
        [SerializeField] private AudioSource _musicSource;

        [Header("BGM Bindings")]
        [Tooltip("One entry per game state. Evaluated in order; first matching trigger wins.")]
        [SerializeField] private BGMBinding[] _bindings;

        [Header("Default BGM")]
        [Tooltip("Optional clip played on Awake before any VoidGameEvent fires. "
               + "Useful for the main menu BGM that starts immediately on scene load.")]
        [SerializeField] private AudioClipSO _defaultClip;

        [Header("Volume")]
        [Tooltip("Master volume for all BGM. Changeable at runtime via SetMasterVolume.")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.5f;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Pre-allocated delegate array — one Action closure per binding (allocated in Awake).
        // OnEnable / OnDisable pass these pre-allocated references to avoid any allocations.
        private Action[] _callbacks;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Pre-allocate one closure per binding.  The captured int 'idx' is a value-type
            // copy, so each closure is independent.  This is a one-time cold allocation.
            if (_bindings != null && _bindings.Length > 0)
            {
                _callbacks = new Action[_bindings.Length];
                for (int i = 0; i < _bindings.Length; i++)
                {
                    int idx = i;                    // Capture by value — unique per iteration.
                    _callbacks[i] = () => SwitchTo(idx);
                }
            }
            else
            {
                _callbacks = Array.Empty<Action>();
            }

            // Start default BGM if assigned.
            if (_defaultClip != null)
                PlayClipInternal(_defaultClip);
        }

        private void OnEnable()
        {
            if (_bindings == null) return;
            for (int i = 0; i < _bindings.Length; i++)
                _bindings[i].trigger?.RegisterCallback(_callbacks[i]);
        }

        private void OnDisable()
        {
            if (_bindings == null) return;
            for (int i = 0; i < _bindings.Length; i++)
                _bindings[i].trigger?.UnregisterCallback(_callbacks[i]);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the BGM master volume immediately.
        /// Call from the Settings system when the player adjusts music volume.
        /// Zero heap allocation — float clamp + AudioSource.volume write only.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            if (_musicSource != null)
                _musicSource.volume = _masterVolume;
        }

        /// <summary>Current master volume (read-only).</summary>
        public float MasterVolume => _masterVolume;

        /// <summary>
        /// Hard-stops the currently playing BGM track.
        /// Useful for dramatic moments (robot destruction cutscene, etc.).
        /// </summary>
        public void StopMusic()
        {
            if (_musicSource != null)
                _musicSource.Stop();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        // Invoked by pre-allocated closures when a trigger fires.
        private void SwitchTo(int index)
        {
            if (_bindings == null || (uint)index >= (uint)_bindings.Length) return;
            PlayClipInternal(_bindings[index].clip);
        }

        // Switches the music source to the supplied clip (or stops if null).
        // Hard cut — no crossfade coroutine, no allocation.
        private void PlayClipInternal(AudioClipSO clipSO)
        {
            if (_musicSource == null) return;

            if (clipSO == null || clipSO.Clip == null)
            {
                _musicSource.Stop();
                return;
            }

            // Skip re-starting if the same clip is already playing.
            if (_musicSource.isPlaying && _musicSource.clip == clipSO.Clip) return;

            _musicSource.clip   = clipSO.Clip;
            _musicSource.volume = _masterVolume;
            _musicSource.loop   = true;
            _musicSource.Play();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_musicSource == null)
                Debug.LogWarning("[BGMController] _musicSource not assigned.", this);

            if (_bindings == null) return;
            for (int i = 0; i < _bindings.Length; i++)
            {
                if (_bindings[i].trigger == null)
                    Debug.LogWarning($"[BGMController] binding[{i}].trigger is null.", this);
            }
        }
#endif
    }
}
