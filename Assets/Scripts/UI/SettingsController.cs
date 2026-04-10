using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that bridges a settings panel's Sliders to the
    /// <see cref="GameSettingsSO"/> runtime SO and persists changes to disk when the
    /// panel is hidden.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   OnEnable → sliders initialised from SO (SetValueWithoutNotify — no alloc).
    ///   Slider.onValueChanged → SetMasterVolume / SetSfxVolume / SetMusicVolume
    ///     → SO stores value + raises _onSettingsChanged → AudioManager re-scales.
    ///   OnDisable → PersistSettings() writes snapshot into SaveData on disk.
    ///
    /// ── ARCHITECTURE RULES ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Delegates cached in Awake; added/removed in OnEnable/OnDisable only.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • PersistSettings() uses Load → mutate → Save to preserve wallet and history.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Assign _settings → the global GameSettingsSO asset.
    ///   2. Assign sliders (all optional — assign only the ones present in the UI).
    ///      Configure each slider's Min Value = 0, Max Value = 1.
    ///   3. No additional button wiring required — changes persist on panel hide.
    /// </summary>
    public sealed class SettingsController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The runtime GameSettingsSO shared with AudioManager and GameBootstrapper. " +
                 "Assign the same SO asset everywhere.")]
        [SerializeField] private GameSettingsSO _settings;

        [Header("Volume Sliders (optional — assign only those present in the UI)")]
        [Tooltip("Slider (0–1) controlling master volume.")]
        [SerializeField] private Slider _masterVolumeSlider;

        [Tooltip("Slider (0–1) controlling SFX channel volume.")]
        [SerializeField] private Slider _sfxVolumeSlider;

        [Tooltip("Slider (0–1) controlling music channel volume.")]
        [SerializeField] private Slider _musicVolumeSlider;

        // ── Cached delegates (allocated once in Awake — zero alloc thereafter) ─

        private UnityAction<float> _onMasterChanged;
        private UnityAction<float> _onSfxChanged;
        private UnityAction<float> _onMusicChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMasterChanged = OnMasterVolumeChanged;
            _onSfxChanged    = OnSfxVolumeChanged;
            _onMusicChanged  = OnMusicVolumeChanged;
        }

        private void OnEnable()
        {
            if (_settings == null) return;

            // Push current SO values to sliders without triggering their callbacks.
            _masterVolumeSlider?.SetValueWithoutNotify(_settings.MasterVolume);
            _sfxVolumeSlider?.SetValueWithoutNotify(_settings.SfxVolume);
            _musicVolumeSlider?.SetValueWithoutNotify(_settings.MusicVolume);

            // Subscribe slider change events.
            _masterVolumeSlider?.onValueChanged.AddListener(_onMasterChanged);
            _sfxVolumeSlider?.onValueChanged.AddListener(_onSfxChanged);
            _musicVolumeSlider?.onValueChanged.AddListener(_onMusicChanged);
        }

        private void OnDisable()
        {
            // Unsubscribe to avoid stale callbacks when the panel is hidden.
            _masterVolumeSlider?.onValueChanged.RemoveListener(_onMasterChanged);
            _sfxVolumeSlider?.onValueChanged.RemoveListener(_onSfxChanged);
            _musicVolumeSlider?.onValueChanged.RemoveListener(_onMusicChanged);

            // Persist on panel close so settings survive the next app launch.
            PersistSettings();
        }

        // ── Slider callbacks ──────────────────────────────────────────────────

        private void OnMasterVolumeChanged(float value) => _settings?.SetMasterVolume(value);
        private void OnSfxVolumeChanged(float value)    => _settings?.SetSfxVolume(value);
        private void OnMusicVolumeChanged(float value)  => _settings?.SetMusicVolume(value);

        // ── Persistence ───────────────────────────────────────────────────────

        /// <summary>
        /// Writes the current volume snapshot into the existing save file.
        /// Uses Load → mutate → Save to preserve wallet balance and match history.
        /// Called automatically on OnDisable (panel hide / scene unload).
        /// </summary>
        private void PersistSettings()
        {
            if (_settings == null) return;
            SaveData save = SaveSystem.Load();
            save.settingsSnapshot = _settings.TakeSnapshot();
            SaveSystem.Save(save);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_settings == null)
                Debug.LogWarning("[SettingsController] _settings GameSettingsSO not assigned — " +
                                 "volume sliders will have no effect.");
        }
#endif
    }
}
