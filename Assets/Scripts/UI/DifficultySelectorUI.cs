using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Difficulty selector screen.  Three buttons (Easy / Medium / Hard) call
    /// <see cref="DifficultySelectionSO.Select"/> which mutates the shared
    /// <see cref="DifficultySO"/> asset via <c>LoadPreset</c>.  MatchManager
    /// reads the same SO at StartMatch, so no additional wiring is needed.
    ///
    /// Architecture constraints:
    ///   • <c>BattleRobots.UI</c> namespace — no Physics references.
    ///   • No heap allocations in Update (Update not overridden).
    ///   • Communicates with MatchManager via <see cref="DifficultySelectionSO"/> (Core).
    ///
    /// Inspector wiring checklist:
    ///   □ _difficultySelection   → DifficultySelectionSO runtime SO
    ///   □ _easyButton            → Button (Easy preset)
    ///   □ _mediumButton          → Button (Medium preset)
    ///   □ _hardButton            → Button (Hard preset)
    ///   □ _easyButtonImage       → Image on Easy button root (for highlight)
    ///   □ _mediumButtonImage     → Image on Medium button root (for highlight)
    ///   □ _hardButtonImage       → Image on Hard button root (for highlight)
    ///   □ _difficultyNameLabel   → Text (shows selected difficulty name)
    ///   □ _descriptionLabel      → Text (shows stats summary for selection)
    ///   □ _selectedColor         → Color applied to the active button background
    ///   □ _normalColor           → Color applied to inactive button backgrounds
    ///   □ _confirmButton         → Button (optional — enabled once a selection is made)
    /// </summary>
    public sealed class DifficultySelectorUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Selection SO")]
        [Tooltip("Runtime SO that stores the chosen difficulty and mutates the shared DifficultySO.")]
        [SerializeField] private DifficultySelectionSO _difficultySelection;

        [Header("Buttons")]
        [Tooltip("Button that selects the Easy preset.")]
        [SerializeField] private Button _easyButton;

        [Tooltip("Button that selects the Medium preset.")]
        [SerializeField] private Button _mediumButton;

        [Tooltip("Button that selects the Hard preset.")]
        [SerializeField] private Button _hardButton;

        [Header("Button Background Images (for highlight)")]
        [Tooltip("Background Image on the Easy button — tinted when selected.")]
        [SerializeField] private Image _easyButtonImage;

        [Tooltip("Background Image on the Medium button — tinted when selected.")]
        [SerializeField] private Image _mediumButtonImage;

        [Tooltip("Background Image on the Hard button — tinted when selected.")]
        [SerializeField] private Image _hardButtonImage;

        [Header("Highlight Colours")]
        [Tooltip("Color applied to the background Image of the currently selected button.")]
        [SerializeField] private Color _selectedColor = new Color(0.25f, 0.75f, 0.25f, 1f);

        [Tooltip("Color applied to the background Images of the unselected buttons.")]
        [SerializeField] private Color _normalColor   = Color.white;

        [Header("Labels (optional)")]
        [Tooltip("Displays the selected difficulty's display name.")]
        [SerializeField] private Text _difficultyNameLabel;

        [Tooltip("Shows a short summary of the selected preset's stat modifiers.")]
        [SerializeField] private Text _descriptionLabel;

        [Header("Confirm (optional)")]
        [Tooltip("'Start Match' or similar button — interactable only once a difficulty is picked. " +
                 "Leave null if the difficulty is always applied and no confirmation step is needed.")]
        [SerializeField] private Button _confirmButton;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // AddListener allocates once at Awake — acceptable outside FixedUpdate.
            if (_easyButton   != null) _easyButton.onClick.AddListener(  OnEasyClicked);
            if (_mediumButton != null) _mediumButton.onClick.AddListener(OnMediumClicked);
            if (_hardButton   != null) _hardButton.onClick.AddListener(  OnHardClicked);
        }

        private void OnEnable()
        {
            // Apply default preset and refresh visuals each time the panel opens.
            _difficultySelection?.Reset();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_easyButton   != null) _easyButton.onClick.RemoveListener(  OnEasyClicked);
            if (_mediumButton != null) _mediumButton.onClick.RemoveListener(OnMediumClicked);
            if (_hardButton   != null) _hardButton.onClick.RemoveListener(  OnHardClicked);
        }

        // ── Button Callbacks ──────────────────────────────────────────────────

        private void OnEasyClicked()   => ApplySelection(DifficultyLevel.Easy);
        private void OnMediumClicked() => ApplySelection(DifficultyLevel.Medium);
        private void OnHardClicked()   => ApplySelection(DifficultyLevel.Hard);

        private void ApplySelection(DifficultyLevel level)
        {
            _difficultySelection?.Select(level);
            Refresh();
        }

        // ── UI Refresh ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds button highlights, name label, description, and confirm button state
        /// from the current <see cref="DifficultySelectionSO"/> values.
        /// Zero heap allocation — reads value-type fields from SO.
        /// </summary>
        private void Refresh()
        {
            if (_difficultySelection == null) return;

            DifficultyLevel level = _difficultySelection.SelectedLevel;

            // ── Button highlights ─────────────────────────────────────────────
            SetButtonHighlight(_easyButtonImage,   level == DifficultyLevel.Easy);
            SetButtonHighlight(_mediumButtonImage, level == DifficultyLevel.Medium);
            SetButtonHighlight(_hardButtonImage,   level == DifficultyLevel.Hard);

            // ── Name label ────────────────────────────────────────────────────
            if (_difficultyNameLabel != null)
            {
                DifficultySO so = _difficultySelection.ActiveDifficulty;
                _difficultyNameLabel.text = so != null ? so.DifficultyName : level.ToString();
            }

            // ── Description label ─────────────────────────────────────────────
            if (_descriptionLabel != null)
                _descriptionLabel.text = BuildDescription(level);

            // ── Confirm button ────────────────────────────────────────────────
            if (_confirmButton != null)
                _confirmButton.interactable = _difficultySelection.HasSelection;
        }

        private void SetButtonHighlight(Image image, bool isSelected)
        {
            if (image == null) return;
            image.color = isSelected ? _selectedColor : _normalColor;
        }

        /// <summary>
        /// Returns a short human-readable description of the selected difficulty preset.
        /// Allocation-free since it only executes on button click (not per frame).
        /// </summary>
        private static string BuildDescription(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Easy:
                    return "AI is sluggish and deals reduced damage.\n" +
                           "Time limit is extended.\n" +
                           "Recommended for beginners.";

                case DifficultyLevel.Medium:
                    return "Balanced challenge for most players.\n" +
                           "All stats at standard values.";

                case DifficultyLevel.Hard:
                    return "Aggressive AI, increased damage.\n" +
                           "Shorter time limit.\n" +
                           "For experienced pilots only.";

                default:
                    return string.Empty;
            }
        }
    }
}
