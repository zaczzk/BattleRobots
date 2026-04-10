using BattleRobots.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that lets the player cycle through available difficulty
    /// presets in the pre-match lobby or main-menu UI.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • Reads the ordered preset list from <see cref="DifficultyPresetsConfig"/>.
    ///   • Writes the selected <see cref="BotDifficultyConfig"/> to
    ///     <see cref="SelectedDifficultySO"/> immediately on each navigation step
    ///     so <see cref="BattleRobots.Physics.RobotAIController"/> reads the correct
    ///     config at Arena-scene Awake time.
    ///   • Updates an optional name label on every selection change.
    ///   • Previous / Next buttons cycle wrap-around through the preset list.
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   BattleRobots.UI namespace.  No BattleRobots.Physics references.
    ///   Button listener delegates cached in Awake; zero per-frame allocations.
    ///   No Update method.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_presets</c> (DifficultyPresetsConfig SO).
    ///   2. Assign <c>_selectedDifficulty</c> (SelectedDifficultySO — the same
    ///      instance wired to RobotAIController._selectedDifficulty in the Arena).
    ///   3. Optionally assign <c>_nameLabel</c>, <c>_prevButton</c>,
    ///      <c>_nextButton</c> — null-safe; buttons may also be wired in Inspector.
    ///   4. <c>PreviousPreset()</c> and <c>NextPreset()</c> are public and can be
    ///      wired directly to Button.onClick in the Inspector as an alternative.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DifficultySelectionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Immutable SO listing all available difficulty presets in selection order.")]
        [SerializeField] private DifficultyPresetsConfig _presets;

        [Tooltip("Mutable SO that stores the chosen preset's config across scene loads. " +
                 "Assign the same instance to every enemy RobotAIController in the Arena scene.")]
        [SerializeField] private SelectedDifficultySO _selectedDifficulty;

        [Header("UI (optional)")]
        [Tooltip("Label displaying the current preset's display name. May be null.")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("Button that cycles to the previous preset (wrap-around). May be null.")]
        [SerializeField] private Button _prevButton;

        [Tooltip("Button that cycles to the next preset (wrap-around). May be null.")]
        [SerializeField] private Button _nextButton;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _selectedIndex;

        /// <summary>Current zero-based index into <see cref="DifficultyPresetsConfig.Presets"/>.</summary>
        public int SelectedIndex => _selectedIndex;

        // ── Cached delegates ──────────────────────────────────────────────────

        private UnityAction _prevAction;
        private UnityAction _nextAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _prevAction = PreviousPreset;
            _nextAction = NextPreset;
        }

        private void OnEnable()
        {
            _prevButton?.onClick.AddListener(_prevAction);
            _nextButton?.onClick.AddListener(_nextAction);
            // Apply the displayed selection immediately so SelectedDifficultySO
            // is consistent with the UI the moment the panel opens.
            ApplySelection();
        }

        private void OnDisable()
        {
            _prevButton?.onClick.RemoveListener(_prevAction);
            _nextButton?.onClick.RemoveListener(_nextAction);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances to the next difficulty preset.
        /// Wraps from the last preset back to the first.
        /// </summary>
        public void NextPreset()
        {
            if (_presets == null || _presets.Presets.Count == 0) return;

            _selectedIndex = (_selectedIndex + 1) % _presets.Presets.Count;
            ApplySelection();
        }

        /// <summary>
        /// Steps back to the previous difficulty preset.
        /// Wraps from the first preset around to the last.
        /// </summary>
        public void PreviousPreset()
        {
            if (_presets == null || _presets.Presets.Count == 0) return;

            _selectedIndex = (_selectedIndex - 1 + _presets.Presets.Count) % _presets.Presets.Count;
            ApplySelection();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplySelection()
        {
            if (_presets == null || _presets.Presets.Count == 0)
            {
                if (_nameLabel != null) _nameLabel.text = "\u2014"; // em-dash
                return;
            }

            var preset = _presets.Presets[_selectedIndex];
            _selectedDifficulty?.Select(preset?.config);

            if (_nameLabel != null)
                _nameLabel.text = preset?.displayName ?? "\u2014";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_presets == null)
                Debug.LogWarning("[DifficultySelectionController] _presets is not assigned.", this);
            if (_selectedDifficulty == null)
                Debug.LogWarning("[DifficultySelectionController] _selectedDifficulty is not assigned.", this);
        }
#endif
    }
}
