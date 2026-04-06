using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that owns the player's audio, input, and key-binding preferences.
    ///
    /// Responsibilities:
    ///   - Holds audio fields (masterVolume, sfxVolume, invertControls).
    ///   - Holds a runtime key-binding dictionary (action → KeyCode).
    ///   - Apply() broadcasts audio changes via SO event channels.
    ///   - LoadFromData / BuildData bridge audio state with the SaveData POCO.
    ///   - LoadKeyBindings / BuildKeyBindings bridge key bindings with the SaveData POCO.
    ///
    /// Default key bindings (used when no save file exists or action is missing):
    ///   Forward=W, Back=S, Left=A, Right=D, Fire=Space
    ///
    /// Lifecycle:
    ///   1. GameBootstrapper calls LoadFromData + LoadKeyBindings at startup.
    ///   2. Settings UI calls SetMasterVolume / SetBinding etc.
    ///   3. SettingsUI.PersistSettings() writes both data + keyBindings into SaveData.
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
        [SerializeField, Range(0f, 1f)] private float _defaultMasterVolume  = 1f;
        [SerializeField, Range(0f, 1f)] private float _defaultSfxVolume     = 1f;
        [SerializeField]                private bool  _defaultInvertControls = false;

        // ── Event channels ────────────────────────────────────────────────────

        [Header("Event Channels")]
        [Tooltip("Raised when masterVolume changes. Payload = new value [0,1].")]
        [SerializeField] private FloatGameEvent _onMasterVolumeChanged;

        [Tooltip("Raised when sfxVolume changes. Payload = new value [0,1].")]
        [SerializeField] private FloatGameEvent _onSfxVolumeChanged;

        [Tooltip("Raised whenever any key binding changes. SettingsUI uses this to refresh row labels.")]
        [SerializeField] private VoidGameEvent _onBindingsChanged;

        // Note: invertControls has no event channel because only RobotController
        // reads it each FixedUpdate (no need to broadcast).

        // ── Runtime state — audio ─────────────────────────────────────────────

        public float MasterVolume   { get; private set; }
        public float SfxVolume      { get; private set; }
        public bool  InvertControls { get; private set; }

        // ── Runtime state — key bindings ──────────────────────────────────────

        /// <summary>
        /// Default key bindings applied when no save data exists or an action is missing.
        /// </summary>
        private static readonly Dictionary<string, KeyCode> s_DefaultBindings =
            new Dictionary<string, KeyCode>(System.StringComparer.Ordinal)
            {
                { "Forward", KeyCode.W     },
                { "Back",    KeyCode.S     },
                { "Left",    KeyCode.A     },
                { "Right",   KeyCode.D     },
                { "Fire",    KeyCode.Space },
            };

        // Mutable runtime dictionary — populated from save data or defaults.
        private readonly Dictionary<string, KeyCode> _bindings =
            new Dictionary<string, KeyCode>(System.StringComparer.Ordinal);

        // ── Public API — audio ────────────────────────────────────────────────

        /// <summary>Initialises audio settings from defaults (called before save is loaded).</summary>
        public void ResetToDefaults()
        {
            MasterVolume   = _defaultMasterVolume;
            SfxVolume      = _defaultSfxVolume;
            InvertControls = _defaultInvertControls;
            Apply();
        }

        /// <summary>
        /// Populates runtime audio state from a deserialized <see cref="SettingsData"/> POCO.
        /// Call this immediately after <see cref="SaveSystem.Load"/>.
        /// </summary>
        public void LoadFromData(SettingsData data)
        {
            if (data == null) { ResetToDefaults(); return; }

            MasterVolume   = Mathf.Clamp01(data.masterVolume);
            SfxVolume      = Mathf.Clamp01(data.sfxVolume);
            InvertControls = data.invertControls;
            Apply();
        }

        /// <summary>Snapshots current audio state into a <see cref="SettingsData"/> POCO.</summary>
        public SettingsData BuildData() => new SettingsData
        {
            masterVolume   = MasterVolume,
            sfxVolume      = SfxVolume,
            invertControls = InvertControls,
        };

        /// <summary>Sets master AudioListener volume and fires the change event.</summary>
        public void SetMasterVolume(float value)
        {
            MasterVolume         = Mathf.Clamp01(value);
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
        public void SetInvertControls(bool invert) => InvertControls = invert;

        // ── Public API — key bindings ─────────────────────────────────────────

        /// <summary>
        /// Loads key bindings from a persisted <see cref="KeyBindingsData"/> POCO.
        /// Missing actions are filled with <see cref="s_DefaultBindings"/>.
        /// Call after <see cref="SaveSystem.Load"/>.
        /// </summary>
        public void LoadKeyBindings(KeyBindingsData data)
        {
            _bindings.Clear();

            // Seed from save data.
            if (data != null && data.entries != null)
            {
                foreach (KeyBindingEntry e in data.entries)
                {
                    if (!string.IsNullOrEmpty(e.actionName))
                        _bindings[e.actionName] = (KeyCode)e.keyCode;
                }
            }

            // Fill defaults for any action not present in save.
            foreach (var kv in s_DefaultBindings)
            {
                if (!_bindings.ContainsKey(kv.Key))
                    _bindings[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// Snapshots all current key bindings into a <see cref="KeyBindingsData"/> POCO
        /// ready to be written into SaveData.keyBindings.
        /// </summary>
        public KeyBindingsData BuildKeyBindings()
        {
            var data = new KeyBindingsData();
            foreach (var kv in _bindings)
                data.entries.Add(new KeyBindingEntry { actionName = kv.Key, keyCode = (int)kv.Value });
            return data;
        }

        /// <summary>
        /// Returns the <see cref="KeyCode"/> bound to <paramref name="actionName"/>.
        /// Returns <see cref="KeyCode.None"/> for unknown actions.
        /// O(1) dictionary lookup — safe to call from FixedUpdate.
        /// </summary>
        public KeyCode GetBinding(string actionName)
        {
            if (_bindings.TryGetValue(actionName, out KeyCode kc)) return kc;
            if (s_DefaultBindings.TryGetValue(actionName, out kc)) return kc;
            return KeyCode.None;
        }

        /// <summary>
        /// Updates the runtime binding for <paramref name="actionName"/> to <paramref name="key"/>
        /// and fires <c>_onBindingsChanged</c>.
        /// </summary>
        public void SetBinding(string actionName, KeyCode key)
        {
            if (string.IsNullOrEmpty(actionName)) return;
            _bindings[actionName] = key;
            _onBindingsChanged?.Raise();
        }

        /// <summary>
        /// Returns the ordered list of all configured action names.
        /// Used by SettingsUI to build rebind rows dynamically.
        /// Allocates — call only during UI initialisation, not per-frame.
        /// </summary>
        public IReadOnlyCollection<string> GetAllActionNames() => _bindings.Keys;

        // ── Helpers ───────────────────────────────────────────────────────────

        private void Apply()
        {
            AudioListener.volume = MasterVolume;
            _onMasterVolumeChanged?.Raise(MasterVolume);
            _onSfxVolumeChanged?.Raise(SfxVolume);
        }
    }
}
