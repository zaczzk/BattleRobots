using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that owns the player's audio and input preferences.
    ///
    /// Responsibilities:
    ///   - Holds the three settings fields (masterVolume, sfxVolume, invertControls).
    ///   - Apply() broadcasts changes via SO event channels (no direct SFXPlayer ref).
    ///   - LoadFromData / BuildData bridge between runtime state and the SaveData POCO.
    ///
    /// Lifecycle:
    ///   1. GameBootstrapper calls LoadFromData(saveData.settings) at startup.
    ///   2. Settings UI calls the mutator methods (SetMasterVolume etc.).
    ///   3. On exit / match end, BuildData() snapshots the current values into SaveData.
    ///
    /// Architecture rules:
    ///   - BattleRobots.Core namespace only; no UI or Physics refs.
    ///   - All change notifications via SO event channels.
    ///   - SO asset is immutable (no public field setters); state mutated through API.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Settings/SettingsSO", order = 0)]
    public sealed class SettingsSO : ScriptableObject
    {
        // ── Default values ────────────────────────────────────────────────────

        [Header("Defaults (used when no save file exists)")]
        [SerializeField, Range(0f, 1f)] private float _defaultMasterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _defaultSfxVolume    = 1f;
        [SerializeField]                private bool  _defaultInvertControls = false;

        // ── Event channels ────────────────────────────────────────────────────

        [Header("Event Channels")]
        [Tooltip("Raised when masterVolume changes. Payload = new value [0,1].")]
        [SerializeField] private FloatGameEvent _onMasterVolumeChanged;

        [Tooltip("Raised when sfxVolume changes. Payload = new value [0,1].")]
        [SerializeField] private FloatGameEvent _onSfxVolumeChanged;

        // Note: invertControls has no event channel because only RobotController
        // reads it each FixedUpdate (no need to broadcast).

        // ── Runtime state ─────────────────────────────────────────────────────

        public float MasterVolume    { get; private set; }
        public float SfxVolume       { get; private set; }
        public bool  InvertControls  { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises from defaults. Called at game start before any save is loaded.
        /// </summary>
        public void ResetToDefaults()
        {
            MasterVolume   = _defaultMasterVolume;
            SfxVolume      = _defaultSfxVolume;
            InvertControls = _defaultInvertControls;
            Apply();
        }

        /// <summary>
        /// Populates runtime state from a deserialized <see cref="SettingsData"/> POCO.
        /// Call this immediately after <see cref="SaveSystem.Load"/>.
        /// </summary>
        public void LoadFromData(SettingsData data)
        {
            if (data == null)
            {
                ResetToDefaults();
                return;
            }

            MasterVolume   = Mathf.Clamp01(data.masterVolume);
            SfxVolume      = Mathf.Clamp01(data.sfxVolume);
            InvertControls = data.invertControls;
            Apply();
        }

        /// <summary>
        /// Snapshots current state into a <see cref="SettingsData"/> POCO
        /// ready to be written into SaveData.settings.
        /// </summary>
        public SettingsData BuildData() => new SettingsData
        {
            masterVolume   = MasterVolume,
            sfxVolume      = SfxVolume,
            invertControls = InvertControls,
        };

        // ── Mutators (called from SettingsUI) ─────────────────────────────────

        /// <summary>Sets master AudioListener volume and fires the change event.</summary>
        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
            _onMasterVolumeChanged?.Raise(MasterVolume);
        }

        /// <summary>Sets SFX bus volume and fires the change event (wired to SFXPlayer).</summary>
        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            _onSfxVolumeChanged?.Raise(SfxVolume);
        }

        /// <summary>Toggles invert-controls preference. No event needed (polled per frame).</summary>
        public void SetInvertControls(bool invert)
        {
            InvertControls = invert;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Broadcasts all current values through their channels so all listeners
        /// (SFXPlayer, AudioListener, etc.) are synchronised on load.
        /// </summary>
        private void Apply()
        {
            AudioListener.volume = MasterVolume;
            _onMasterVolumeChanged?.Raise(MasterVolume);
            _onSfxVolumeChanged?.Raise(SfxVolume);
        }
    }
}
