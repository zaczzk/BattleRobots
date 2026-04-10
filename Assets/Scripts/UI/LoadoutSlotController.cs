using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives a single category-slot row in the pre-match <see cref="LoadoutBuilderController"/>.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Displays the slot's category label and the currently selected part's
    ///     name, description, and thumbnail.
    ///   • Maintains an ordered list of candidate parts (owned parts of the
    ///     matching category, injected by <see cref="LoadoutBuilderController"/>).
    ///   • Exposes <see cref="PreviousPart"/> / <see cref="NextPart"/> methods for
    ///     Previous / Next buttons; <see cref="GetSelectedPartDef"/> for the parent
    ///     to read the confirmed selection.
    ///
    /// ── Prefab setup ──────────────────────────────────────────────────────────
    ///   1. Attach to the root of a slot-row prefab.
    ///   2. Assign UI refs in the Inspector (all optional): _categoryLabel, _partNameLabel,
    ///      _partDescLabel, _thumbnailImage, _prevButton, _nextButton.
    ///   3. Wire _prevButton.onClick → <see cref="PreviousPart"/> and
    ///      _nextButton.onClick → <see cref="NextPart"/> in the prefab Inspector.
    ///   4. Do NOT assign data fields in the prefab — <see cref="LoadoutBuilderController"/>
    ///      calls <see cref="Setup"/> at runtime.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No Update or FixedUpdate.
    ///   - String allocations in <see cref="Refresh"/> are cold-path (user interaction).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadoutSlotController : MonoBehaviour
    {
        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI References")]
        [Tooltip("Shows the slot category name (e.g. 'Weapon', 'Armor').")]
        [SerializeField] private Text _categoryLabel;

        [Tooltip("Shows the currently selected part's display name, or 'None'.")]
        [SerializeField] private Text _partNameLabel;

        [Tooltip("Optional description / stat summary for the selected part.")]
        [SerializeField] private Text _partDescLabel;

        [Tooltip("Thumbnail image for the selected part. Hidden when no part is selected.")]
        [SerializeField] private Image _thumbnailImage;

        [Tooltip("Cycles to the previous available part (or wraps around to None).")]
        [SerializeField] private Button _prevButton;

        [Tooltip("Cycles to the next available part (or wraps around to None).")]
        [SerializeField] private Button _nextButton;

        // ── Runtime state (injected by LoadoutBuilderController) ─────────────

        // Ordered list of candidate parts for this slot's category.
        // Index 0 is the "None / unequipped" sentinel (null entry).
        private readonly List<PartDefinition> _candidates = new List<PartDefinition>();
        private PartCategory _category;
        private int          _selectedIndex; // 0 = None

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_prevButton != null)
                _prevButton.onClick.AddListener(PreviousPart);
            if (_nextButton != null)
                _nextButton.onClick.AddListener(NextPart);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises this row for <paramref name="category"/> with the given
        /// <paramref name="ownedParts"/> as candidates.  Called once by
        /// <see cref="LoadoutBuilderController"/> after instantiation.
        ///
        /// <paramref name="currentPartId"/> is the part that should be pre-selected
        /// (from <see cref="PlayerLoadout.EquippedPartIds"/>). Pass null to start at None.
        /// </summary>
        public void Setup(PartCategory category,
                          IReadOnlyList<PartDefinition> ownedParts,
                          string currentPartId)
        {
            _category      = category;
            _selectedIndex = 0;

            // Index 0 = None sentinel.
            _candidates.Clear();
            _candidates.Add(null);

            if (ownedParts != null)
            {
                foreach (PartDefinition def in ownedParts)
                {
                    if (def != null)
                        _candidates.Add(def);
                }
            }

            // Pre-select the saved part if it is available.
            if (!string.IsNullOrWhiteSpace(currentPartId))
            {
                for (int i = 1; i < _candidates.Count; i++)
                {
                    if (_candidates[i].PartId == currentPartId)
                    {
                        _selectedIndex = i;
                        break;
                    }
                }
            }

            Refresh();
        }

        /// <summary>
        /// Rebuilds the candidate list when the player's inventory changes
        /// (called by <see cref="LoadoutBuilderController.RefreshAllSlots"/>).
        /// Preserves the current selection if the part is still available.
        /// </summary>
        public void RebuildCandidates(IReadOnlyList<PartDefinition> ownedParts)
        {
            // Preserve the currently selected part ID, if any.
            string savedId = _candidates.Count > 0 && _selectedIndex > 0
                ? _candidates[_selectedIndex]?.PartId
                : null;

            _candidates.Clear();
            _candidates.Add(null);   // None sentinel

            if (ownedParts != null)
            {
                foreach (PartDefinition def in ownedParts)
                {
                    if (def != null)
                        _candidates.Add(def);
                }
            }

            // Restore saved selection or fall back to None.
            _selectedIndex = 0;
            if (savedId != null)
            {
                for (int i = 1; i < _candidates.Count; i++)
                {
                    if (_candidates[i].PartId == savedId)
                    {
                        _selectedIndex = i;
                        break;
                    }
                }
            }

            Refresh();
        }

        /// <summary>Cycles to the previous candidate (wraps around).</summary>
        public void PreviousPart()
        {
            if (_candidates.Count <= 1) return;
            _selectedIndex = (_selectedIndex - 1 + _candidates.Count) % _candidates.Count;
            Refresh();
        }

        /// <summary>Cycles to the next candidate (wraps around).</summary>
        public void NextPart()
        {
            if (_candidates.Count <= 1) return;
            _selectedIndex = (_selectedIndex + 1) % _candidates.Count;
            Refresh();
        }

        /// <summary>
        /// Returns the currently selected <see cref="PartDefinition"/>, or null if
        /// the "None" slot is selected.
        /// </summary>
        public PartDefinition GetSelectedPartDef()
            => _selectedIndex > 0 && _selectedIndex < _candidates.Count
                ? _candidates[_selectedIndex]
                : null;

        /// <summary>The slot category this row represents.</summary>
        public PartCategory Category => _category;

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates all UI widgets to reflect the current selection.
        /// Cold-path: called only on user interaction or Setup.
        /// </summary>
        private void Refresh()
        {
            if (_categoryLabel != null)
                _categoryLabel.text = _category.ToString();

            PartDefinition selected = GetSelectedPartDef();

            if (_partNameLabel != null)
                _partNameLabel.text = selected != null ? selected.DisplayName : "— None —";

            if (_partDescLabel != null)
                _partDescLabel.text = selected != null ? selected.Description : string.Empty;

            if (_thumbnailImage != null)
            {
                bool hasThumbnail = selected != null && selected.Thumbnail != null;
                _thumbnailImage.sprite  = hasThumbnail ? selected.Thumbnail : null;
                _thumbnailImage.enabled = hasThumbnail;
            }

            // Disable nav buttons when only the None option is available.
            bool canCycle = _candidates.Count > 1;
            if (_prevButton != null) _prevButton.interactable = canCycle;
            if (_nextButton != null) _nextButton.interactable = canCycle;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_prevButton == null)
                Debug.LogWarning("[LoadoutSlotController] _prevButton not assigned — " +
                                 "Previous navigation will not work.", this);
            if (_nextButton == null)
                Debug.LogWarning("[LoadoutSlotController] _nextButton not assigned — " +
                                 "Next navigation will not work.", this);
        }
#endif
    }
}
