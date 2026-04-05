using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Settings screen controller.
    /// Exposes master volume, SFX volume, and invert-controls controls.
    ///
    /// Architecture constraints:
    ///   • <c>BattleRobots.UI</c> namespace — no reference to BattleRobots.Physics.
    ///   • Reads/writes only through <see cref="SettingsSO"/> (Core); never touches
    ///     AudioListener or SFXPlayer directly.
    ///   • Changes are saved via <see cref="SaveSystem"/> immediately on each interaction
    ///     so settings persist even if the app force-quits.
    ///   • No heap allocations — no Update, no string formatting in hot paths.
    ///
    /// Inspector wiring checklist:
    ///   □ _settings             → SettingsSO asset
    ///   □ _masterVolumeSlider   → Slider (0–1)
    ///   □ _sfxVolumeSlider      → Slider (0–1)
    ///   □ _invertControlsToggle → Toggle
    ///   □ _masterVolumeLabel    → Text (optional, shows numeric value)
    ///   □ _sfxVolumeLabel       → Text (optional)
    ///   □ _closeButton          → Button (deactivates the panel)
    /// </summary>
    public sealed class SettingsUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("SO References")]
        [Tooltip("SettingsSO asset — all mutations go through this.")]
        [SerializeField] private SettingsSO _settings;

        [Header("Controls")]
        [Tooltip("Master volume slider [0, 1].")]
        [SerializeField] private Slider _masterVolumeSlider;

        [Tooltip("SFX volume slider [0, 1].")]
        [SerializeField] private Slider _sfxVolumeSlider;

        [Tooltip("Toggle for inverting the vertical control axis.")]
        [SerializeField] private Toggle _invertControlsToggle;

        [Header("Labels (optional)")]
        [Tooltip("Displays the current master volume as a percentage.")]
        [SerializeField] private Text _masterVolumeLabel;

        [Tooltip("Displays the current SFX volume as a percentage.")]
        [SerializeField] private Text _sfxVolumeLabel;

        [Header("Navigation")]
        [Tooltip("Button that closes/deactivates the settings panel.")]
        [SerializeField] private Button _closeButton;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_masterVolumeSlider != null)
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            if (_invertControlsToggle != null)
                _invertControlsToggle.onValueChanged.AddListener(OnInvertControlsChanged);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnEnable()
        {
            // Sync controls to current SO state when the panel opens.
            RefreshControls();
        }

        private void OnDestroy()
        {
            if (_masterVolumeSlider    != null) _masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (_sfxVolumeSlider       != null) _sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            if (_invertControlsToggle  != null) _invertControlsToggle.onValueChanged.RemoveListener(OnInvertControlsChanged);
            if (_closeButton           != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        // ── Private Callbacks ─────────────────────────────────────────────────

        private void OnMasterVolumeChanged(float value)
        {
            if (_settings == null) return;
            _settings.SetMasterVolume(value);
            UpdateLabel(_masterVolumeLabel, value);
            PersistSettings();
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (_settings == null) return;
            _settings.SetSfxVolume(value);
            UpdateLabel(_sfxVolumeLabel, value);
            PersistSettings();
        }

        private void OnInvertControlsChanged(bool isOn)
        {
            if (_settings == null) return;
            _settings.SetInvertControls(isOn);
            PersistSettings();
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Syncs all UI controls to the current SettingsSO values.
        /// Uses SetValueWithoutNotify to avoid triggering the onValueChanged
        /// callbacks (which would write back to SO and persist unnecessarily).
        /// </summary>
        private void RefreshControls()
        {
            if (_settings == null) return;

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetValueWithoutNotify(_settings.MasterVolume);
                UpdateLabel(_masterVolumeLabel, _settings.MasterVolume);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.SetValueWithoutNotify(_settings.SfxVolume);
                UpdateLabel(_sfxVolumeLabel, _settings.SfxVolume);
            }

            if (_invertControlsToggle != null)
                _invertControlsToggle.SetIsOnWithoutNotify(_settings.InvertControls);
        }

        /// <summary>
        /// Writes current settings into SaveData and persists to disk.
        /// Called after every user interaction so no setting is ever lost.
        /// </summary>
        private void PersistSettings()
        {
            SaveData data = SaveSystem.Load();
            data.settings = _settings.BuildData();
            SaveSystem.Save(data);
        }

        /// <summary>Updates a volume percentage label (e.g. "75 %"). No-op if label is null.</summary>
        private static void UpdateLabel(Text label, float value)
        {
            if (label == null) return;
            // Integer percentage to avoid per-frame float→string alloc concerns.
            label.text = $"{Mathf.RoundToInt(value * 100f)} %";
        }
    }
}
