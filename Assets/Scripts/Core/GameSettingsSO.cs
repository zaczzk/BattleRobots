using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO for player audio preferences (master, SFX, and music volumes).
    /// Volumes are stored in [0, 1]; effective volumes are master × channel and are
    /// consumed by <see cref="AudioManager"/> on every play call.
    ///
    /// ── Persistence ───────────────────────────────────────────────────────────
    ///   Snapshot is written into <see cref="SaveData.settingsSnapshot"/> by
    ///   <see cref="BattleRobots.UI.SettingsController"/> (on panel close) and
    ///   restored on startup by <see cref="GameBootstrapper"/>.
    ///
    /// ── ARCHITECTURE RULES ────────────────────────────────────────────────────
    ///   • Volumes mutated only through SetMasterVolume / SetSfxVolume / SetMusicVolume.
    ///   • LoadSnapshot must NOT raise _onSettingsChanged (bootstrapper context;
    ///     AudioManager may not be listening yet).
    ///   • _onSettingsChanged is optional — null-guarded on every Raise.
    ///   • SO asset immutable at runtime except through the designated mutators.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/GameSettings")]
    public sealed class GameSettingsSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Volumes")]
        [Tooltip("Master volume multiplier applied to all audio channels. Range [0, 1].")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;

        [Tooltip("SFX volume multiplier (combined with master). Range [0, 1].")]
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

        [Tooltip("Music volume multiplier (combined with master). Range [0, 1].")]
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 1f;

        [Header("Events")]
        [Tooltip("Raised after any volume change (Set* or Reset). " +
                 "AudioManager and SettingsController listen to this channel.")]
        [SerializeField] private VoidGameEvent _onSettingsChanged;

        // ── Read-only properties ──────────────────────────────────────────────

        public float MasterVolume => _masterVolume;
        public float SfxVolume    => _sfxVolume;
        public float MusicVolume  => _musicVolume;

        /// <summary>Effective SFX level = master × sfx. Pass to AudioSource.volume.</summary>
        public float EffectiveSfxVolume   => _masterVolume * _sfxVolume;

        /// <summary>Effective music level = master × music. Pass to AudioSource.volume.</summary>
        public float EffectiveMusicVolume => _masterVolume * _musicVolume;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>Sets master volume (clamped to [0, 1]) and raises the changed event.</summary>
        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            _onSettingsChanged?.Raise();
        }

        /// <summary>Sets SFX channel volume (clamped to [0, 1]) and raises the changed event.</summary>
        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            _onSettingsChanged?.Raise();
        }

        /// <summary>Sets music channel volume (clamped to [0, 1]) and raises the changed event.</summary>
        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            _onSettingsChanged?.Raise();
        }

        // ── Snapshot API ──────────────────────────────────────────────────────

        /// <summary>
        /// Restores volumes from a persisted snapshot.
        /// Does NOT raise <c>_onSettingsChanged</c> so that GameBootstrapper can call this
        /// before AudioManager is ready.
        /// </summary>
        public void LoadSnapshot(SettingsSnapshot snapshot)
        {
            if (snapshot == null) return;
            _masterVolume = Mathf.Clamp01(snapshot.masterVolume);
            _sfxVolume    = Mathf.Clamp01(snapshot.sfxVolume);
            _musicVolume  = Mathf.Clamp01(snapshot.musicVolume);
        }

        /// <summary>
        /// Returns a new <see cref="SettingsSnapshot"/> capturing the current volumes.
        /// Safe to call on any thread — no Unity types involved.
        /// </summary>
        public SettingsSnapshot TakeSnapshot() => new SettingsSnapshot
        {
            masterVolume = _masterVolume,
            sfxVolume    = _sfxVolume,
            musicVolume  = _musicVolume
        };

        /// <summary>
        /// Restores all volumes to their defaults (1.0) and raises the changed event.
        /// </summary>
        public void Reset()
        {
            _masterVolume = 1f;
            _sfxVolume    = 1f;
            _musicVolume  = 1f;
            _onSettingsChanged?.Raise();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onSettingsChanged == null)
                Debug.LogWarning("[GameSettingsSO] _onSettingsChanged event not assigned — " +
                                 "AudioManager will not react to volume changes at runtime.");
        }
#endif
    }
}
