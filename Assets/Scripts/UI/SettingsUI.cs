using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Settings screen controller.
    /// Exposes master volume, SFX volume, invert-controls, and key rebinding.
    ///
    /// Architecture constraints:
    ///   • <c>BattleRobots.UI</c> namespace — no reference to BattleRobots.Physics.
    ///   • Reads/writes only through <see cref="SettingsSO"/> (Core).
    ///   • Changes are persisted to disk after every interaction.
    ///   • Rebind key capture runs in a Coroutine (no per-frame Update allocation
    ///     outside of active capture; s_AllKeyCodes cached once in a static ctor).
    ///
    /// Inspector wiring checklist (audio):
    ///   □ _settings             → SettingsSO asset
    ///   □ _masterVolumeSlider   → Slider (0–1)
    ///   □ _sfxVolumeSlider      → Slider (0–1)
    ///   □ _invertControlsToggle → Toggle
    ///   □ _masterVolumeLabel    → Text (optional)
    ///   □ _sfxVolumeLabel       → Text (optional)
    ///   □ _closeButton          → Button (deactivates the panel)
    ///
    /// Inspector wiring checklist (rebinding):
    ///   □ _rebindRows[]         → RebindRowUI components (one per default action)
    ///   □ _rebindOverlay        → GameObject shown during key capture (with label)
    ///   □ _rebindOverlayLabel   → Text inside the overlay ("Press any key…")
    /// </summary>
    public sealed class SettingsUI : MonoBehaviour
    {
        // ── Inspector — audio ─────────────────────────────────────────────────

        [Header("SO References")]
        [Tooltip("SettingsSO asset — all mutations go through this.")]
        [SerializeField] private SettingsSO _settings;

        [Header("Audio Controls")]
        [Tooltip("Master volume slider [0, 1].")]
        [SerializeField] private Slider _masterVolumeSlider;

        [Tooltip("SFX volume slider [0, 1].")]
        [SerializeField] private Slider _sfxVolumeSlider;

        [Tooltip("Toggle for inverting the vertical control axis.")]
        [SerializeField] private Toggle _invertControlsToggle;

        [Header("Audio Labels (optional)")]
        [Tooltip("Displays the current master volume as a percentage.")]
        [SerializeField] private Text _masterVolumeLabel;

        [Tooltip("Displays the current SFX volume as a percentage.")]
        [SerializeField] private Text _sfxVolumeLabel;

        [Header("Navigation")]
        [Tooltip("Button that closes/deactivates the settings panel.")]
        [SerializeField] private Button _closeButton;

        // ── Inspector — rebinding ─────────────────────────────────────────────

        [Header("Key Rebinding")]
        [Tooltip("One RebindRowUI per rebindable action. " +
                 "Populated from SettingsSO action names on Awake.")]
        [SerializeField] private RebindRowUI[] _rebindRows;

        [Tooltip("Overlay shown while awaiting a key press. Hidden otherwise.")]
        [SerializeField] private GameObject _rebindOverlay;

        [Tooltip("Text inside the overlay describing the action being rebound.")]
        [SerializeField] private Text _rebindOverlayLabel;

        // ── Static — all keyboard KeyCodes (cached once, no per-frame alloc) ──

        /// <summary>
        /// All keyboard-range KeyCodes (excludes None, mouse buttons, joystick).
        /// Populated in the static constructor so it is ready before any instance
        /// calls Awake. Safe to iterate in a Coroutine without allocations.
        /// </summary>
        private static readonly KeyCode[] s_AllKeyCodes;

        static SettingsUI()
        {
            // KeyCode enum ranges:
            //   8–282  keyboard (Backspace … F15)
            //   283–309 system / modifier keys
            //   310–319 more modifiers
            //   320–330 numpad
            // Mouse0–6 start at 323 on some configs; safest bound is < 323.
            System.Array all = System.Enum.GetValues(typeof(KeyCode));
            var filtered = new System.Collections.Generic.List<KeyCode>(200);
            foreach (KeyCode kc in all)
            {
                int v = (int)kc;
                if (v >= 8 && v < 323)   // keyboard keys only
                    filtered.Add(kc);
            }
            s_AllKeyCodes = filtered.ToArray();
        }

        // ── Runtime state ─────────────────────────────────────────────────────

        private Coroutine _captureCoroutine;

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

            // Setup rebind rows. Actions come from SettingsSO (which seeds from defaults
            // after LoadKeyBindings — called by GameBootstrapper before scene loads).
            SetupRebindRows();

            if (_rebindOverlay != null)
                _rebindOverlay.SetActive(false);
        }

        private void OnEnable()
        {
            RefreshControls();
            RefreshRebindRows();
        }

        private void OnDestroy()
        {
            if (_masterVolumeSlider   != null) _masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (_sfxVolumeSlider      != null) _sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            if (_invertControlsToggle != null) _invertControlsToggle.onValueChanged.RemoveListener(OnInvertControlsChanged);
            if (_closeButton          != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        // ── Private — audio callbacks ─────────────────────────────────────────

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
            // Abort any in-progress rebind when the panel closes.
            if (_captureCoroutine != null)
            {
                StopCoroutine(_captureCoroutine);
                _captureCoroutine = null;
                SetRebindOverlayActive(false);
                SetRebindRowsInteractable(true);
            }
            gameObject.SetActive(false);
        }

        // ── Private — rebind UI ───────────────────────────────────────────────

        /// <summary>
        /// Calls Setup on each row using the action names supplied by SettingsSO.
        /// The number of rows is determined by the array wired in Inspector.
        /// </summary>
        private void SetupRebindRows()
        {
            if (_rebindRows == null || _rebindRows.Length == 0 || _settings == null) return;

            // Use SettingsSO's default action names in declaration order.
            string[] defaultActions = { "Forward", "Back", "Left", "Right", "Fire" };

            for (int i = 0; i < _rebindRows.Length && i < defaultActions.Length; i++)
            {
                if (_rebindRows[i] == null) continue;
                string action = defaultActions[i];
                _rebindRows[i].Setup(action, _settings.GetBinding(action), OnRebindRowClicked);
            }
        }

        /// <summary>Updates all row key labels from the current SettingsSO state.</summary>
        private void RefreshRebindRows()
        {
            if (_rebindRows == null || _settings == null) return;
            foreach (RebindRowUI row in _rebindRows)
            {
                if (row == null) continue;
                row.UpdateKeyDisplay(_settings.GetBinding(row.ActionName));
            }
        }

        /// <summary>Called by a RebindRowUI when its button is clicked.</summary>
        private void OnRebindRowClicked(string actionName)
        {
            if (_captureCoroutine != null) return; // capture already running

            _captureCoroutine = StartCoroutine(CaptureKeyCoroutine(actionName));
        }

        /// <summary>
        /// Waits for the player to press a keyboard key, then applies the rebind.
        /// Locks all rebind-row buttons during capture to prevent double-capture.
        /// </summary>
        private IEnumerator CaptureKeyCoroutine(string actionName)
        {
            // Show overlay.
            SetRebindOverlayActive(true);
            if (_rebindOverlayLabel != null)
                _rebindOverlayLabel.text = $"Rebinding: {actionName}\nPress any key…";

            // Lock rows.
            SetRebindRowsInteractable(false);

            // Wait one frame so the button click that started this doesn't immediately
            // register as the captured key.
            yield return null;

            // Scan for first key press.
            while (true)
            {
                if (Input.anyKeyDown)
                {
                    for (int i = 0; i < s_AllKeyCodes.Length; i++)
                    {
                        if (Input.GetKeyDown(s_AllKeyCodes[i]))
                        {
                            ApplyRebind(actionName, s_AllKeyCodes[i]);
                            goto Done;
                        }
                    }
                }
                yield return null;
            }

            Done:
            SetRebindOverlayActive(false);
            SetRebindRowsInteractable(true);
            _captureCoroutine = null;
        }

        private void ApplyRebind(string actionName, KeyCode key)
        {
            if (_settings == null) return;
            _settings.SetBinding(actionName, key);
            RefreshRebindRows();
            PersistSettings();
        }

        private void SetRebindOverlayActive(bool active)
        {
            if (_rebindOverlay != null) _rebindOverlay.SetActive(active);
        }

        private void SetRebindRowsInteractable(bool interactable)
        {
            if (_rebindRows == null) return;
            foreach (RebindRowUI row in _rebindRows)
                row?.SetInteractable(interactable);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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
        /// Writes current settings AND key bindings into SaveData and persists to disk.
        /// Called after every user interaction.
        /// </summary>
        private void PersistSettings()
        {
            if (_settings == null) return;
            SaveData data       = SaveSystem.Load();
            data.settings       = _settings.BuildData();
            data.keyBindings    = _settings.BuildKeyBindings();
            SaveSystem.Save(data);
        }

        private static void UpdateLabel(Text label, float value)
        {
            if (label == null) return;
            label.text = $"{Mathf.RoundToInt(value * 100f)} %";
        }
    }
}
