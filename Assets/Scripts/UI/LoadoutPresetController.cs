using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match UI controller that exposes <see cref="LoadoutPresetManagerSO"/> features:
    /// save the current loadout as a named preset, load a saved preset into the active
    /// loadout, and delete unwanted presets.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────
    ///   1. OnEnable subscribes to <c>_onPresetsChanged</c> and calls Refresh() so the
    ///      list panel always shows the current state on enable.
    ///   2. Player types a name into <c>_presetNameField</c> and clicks <c>_saveButton</c>
    ///      → <see cref="SaveCurrentPreset"/> is called.
    ///   3. <see cref="SaveCurrentPreset"/> reads <see cref="PlayerLoadout.EquippedPartIds"/>,
    ///      calls <see cref="LoadoutPresetManagerSO.SavePreset"/>, persists via
    ///      load → mutate → Save, and shows feedback in <c>_statusText</c>.
    ///   4. Each row in the preset list exposes a Load button (wired per-row in Refresh())
    ///      that calls <see cref="ApplyPreset"/>, and a Delete button that calls
    ///      <see cref="DeletePreset"/>.
    ///   5. <c>_onPresetsChanged</c> fires after any mutation → Refresh() repopulates.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All optional fields are null-safe; component works with any subset assigned.
    ///   • No Update / FixedUpdate.
    ///   • Delegates cached in Awake; zero alloc after Awake.
    ///   • Persist uses the load → mutate → Save round-trip (same as ShopManager).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add to the loadout-panel Canvas alongside LoadoutBuilderController.
    ///   2. Assign _presetManager → LoadoutPresetManagerSO SO asset.
    ///   3. Assign _playerLoadout → PlayerLoadout SO asset (same as builder uses).
    ///   4. Assign _onPresetsChanged → the VoidGameEvent inside LoadoutPresetManagerSO.
    ///   5. Optionally assign _presetNameField, _saveButton, _statusText, _listContainer,
    ///      and _rowPrefab (prefab must have Text children for name/load/delete buttons).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadoutPresetController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("SO that stores and manages named loadout presets.")]
        [SerializeField] private LoadoutPresetManagerSO _presetManager;

        [Tooltip("Runtime loadout SO. EquippedPartIds are snapshotted on save; " +
                 "SetLoadout() is called when the player loads a preset.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same VoidGameEvent as LoadoutPresetManagerSO._onPresetsChanged. " +
                 "Triggers a Refresh() of the preset list panel on any change.")]
        [SerializeField] private VoidGameEvent _onPresetsChanged;

        // ── Inspector — Save UI ───────────────────────────────────────────────

        [Header("Save Controls (optional)")]
        [Tooltip("InputField the player types the preset name into.")]
        [SerializeField] private InputField _presetNameField;

        [Tooltip("Button that calls SaveCurrentPreset(). May also be wired via Inspector.")]
        [SerializeField] private Button _saveButton;

        [Tooltip("Optional Text label for status / error feedback (e.g. 'Saved!' or 'Name required').")]
        [SerializeField] private Text _statusText;

        // ── Inspector — Preset List ───────────────────────────────────────────

        [Header("Preset List (optional)")]
        [Tooltip("Parent Transform for the instantiated preset rows.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab for one preset row.  The root must have a Text for the preset name " +
                 "plus two Buttons (Load, Delete) as direct children.")]
        [SerializeField] private GameObject _rowPrefab;

        // ── Cached delegates ──────────────────────────────────────────────────

        private System.Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;

            if (_saveButton != null)
                _saveButton.onClick.AddListener(SaveCurrentPreset);
        }

        private void OnEnable()
        {
            _onPresetsChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPresetsChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the preset name from <c>_presetNameField</c> and the part IDs from
        /// <see cref="PlayerLoadout.EquippedPartIds"/>, then saves a new preset via
        /// <see cref="LoadoutPresetManagerSO.SavePreset"/>.
        ///
        /// On success: persists to disk, clears the name field, shows "Saved!" feedback.
        /// On failure: shows the relevant reason in <c>_statusText</c>.
        /// All fields are optional — missing refs are handled gracefully.
        /// </summary>
        public void SaveCurrentPreset()
        {
            if (_presetManager == null)
            {
                SetStatus("No preset manager assigned.");
                return;
            }

            string presetName = _presetNameField != null
                ? _presetNameField.text
                : string.Empty;

            if (string.IsNullOrWhiteSpace(presetName))
            {
                SetStatus("Enter a preset name.");
                return;
            }

            if (_presetManager.IsFull)
            {
                SetStatus($"Max {_presetManager.MaxPresets} presets reached. Delete one first.");
                return;
            }

            IReadOnlyList<string> partIds = _playerLoadout != null
                ? _playerLoadout.EquippedPartIds
                : new List<string>();

            bool saved = _presetManager.SavePreset(presetName, partIds);
            if (!saved)
            {
                SetStatus("Could not save preset.");
                return;
            }

            // Persist the updated preset list to disk.
            PersistPresets();

            // Clear the name field and show success feedback.
            if (_presetNameField != null)
                _presetNameField.text = string.Empty;
            SetStatus("Saved!");

            Debug.Log($"[LoadoutPresetController] Preset '{presetName.Trim()}' saved " +
                      $"({partIds.Count} part(s)).");
        }

        /// <summary>
        /// Loads the preset at <paramref name="index"/> into the active
        /// <see cref="PlayerLoadout"/> and persists the change.
        /// No-op when <paramref name="index"/> is out of range or refs are null.
        /// </summary>
        public void ApplyPreset(int index)
        {
            if (_presetManager == null || _playerLoadout == null) return;

            IReadOnlyList<string> partIds = _presetManager.LoadPreset(index);
            if (partIds == null) return;

            _playerLoadout.SetLoadout(partIds);

            // Persist loadout to disk (same round-trip as LoadoutBuilderController).
            SaveData data = SaveSystem.Load();
            data.loadoutPartIds = new List<string>(partIds);
            SaveSystem.Save(data);

            SetStatus($"Preset '{_presetManager.Presets[index].name}' loaded.");
            Debug.Log($"[LoadoutPresetController] Preset {index} applied ({partIds.Count} part(s)).");
        }

        /// <summary>
        /// Deletes the preset at <paramref name="index"/> and persists the change.
        /// No-op when <paramref name="index"/> is out of range or _presetManager is null.
        /// </summary>
        public void DeletePreset(int index)
        {
            if (_presetManager == null) return;

            bool deleted = _presetManager.DeletePreset(index);
            if (!deleted) return;

            PersistPresets();
            Debug.Log($"[LoadoutPresetController] Preset {index} deleted.");
        }

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Repopulates the preset list panel.  Destroys all existing row children,
        /// then instantiates one row per saved preset.
        /// No-op when _listContainer or _rowPrefab are not assigned.
        /// </summary>
        private void Refresh()
        {
            if (_listContainer == null || _rowPrefab == null) return;
            if (_presetManager == null) return;

            // Destroy existing rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            IReadOnlyList<SavedLoadoutPreset> presets = _presetManager.Presets;
            for (int i = 0; i < presets.Count; i++)
            {
                int capturedIndex = i;
                GameObject row = Instantiate(_rowPrefab, _listContainer);

                // Name label — first Text child.
                Text nameLabel = row.GetComponentInChildren<Text>();
                if (nameLabel != null)
                    nameLabel.text = presets[i].name;

                // Wire up the first two Buttons (Load, Delete) in child order.
                Button[] buttons = row.GetComponentsInChildren<Button>();
                if (buttons.Length >= 1)
                    buttons[0].onClick.AddListener(() => ApplyPreset(capturedIndex));
                if (buttons.Length >= 2)
                    buttons[1].onClick.AddListener(() => DeletePreset(capturedIndex));
            }
        }

        private void PersistPresets()
        {
            if (_presetManager == null) return;

            SaveData data = SaveSystem.Load();
            data.savedLoadoutPresets = _presetManager.TakeSnapshot();
            SaveSystem.Save(data);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }
    }
}
