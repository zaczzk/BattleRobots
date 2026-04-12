using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Reactive UI bridge between <see cref="LoadoutValidator"/> and the pre-match
    /// "Start Match" (or "Enter Arena") button.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────────
    ///   1. OnEnable subscribes to <c>_onLoadoutChanged</c> (VoidGameEvent) and
    ///      calls Refresh().
    ///   2. Refresh() calls <see cref="LoadoutValidator.Validate"/> with the current
    ///      <see cref="PlayerLoadout"/>, <see cref="RobotDefinition"/>,
    ///      <see cref="PlayerInventory"/>, and <see cref="ShopCatalog"/>.
    ///   3. <c>_startMatchButton.interactable</c> is set to <c>result.IsValid</c>.
    ///   4. <c>_validationPanel</c> is shown / hidden based on validity.
    ///   5. <c>_validationText</c> displays all error messages (one per line) when
    ///      the loadout is invalid, or an empty string when valid.
    ///   6. OnDisable unsubscribes to prevent stale callbacks when the panel is hidden.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All inspector fields are optional; null dependencies are handled safely.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Refresh delegate cached in Awake; zero heap alloc after Awake on valid path.
    ///   • Works standalone: can be placed on any panel that has a "Start Match" button,
    ///     independently of <see cref="LoadoutBuilderController"/>.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────────
    ///   • _playerLoadout      → PlayerLoadout SO (same as LoadoutBuilderController)
    ///   • _robotDefinition    → RobotDefinition SO (defines required slot categories)
    ///   • _playerInventory    → PlayerInventory SO (ownership checks)
    ///   • _shopCatalog        → ShopCatalog SO (catalog-membership + category checks)
    ///   • _onLoadoutChanged   → same VoidGameEvent as PlayerLoadout._onLoadoutChanged
    ///   • _startMatchButton   → the Button whose interactable state is gated (optional)
    ///   • _validationPanel    → GameObject shown when loadout is invalid (optional)
    ///   • _validationText     → Text that lists all error messages (optional)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildValidationController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO holding the player's equipped part IDs. "
               + "Subscribe to _onLoadoutChanged to refresh when the selection changes.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Robot definition that specifies which slot categories must be filled.")]
        [SerializeField] private RobotDefinition _robotDefinition;

        [Tooltip("Optional — when assigned, each equipped part is checked for ownership. "
               + "Omit to skip the ownership check.")]
        [SerializeField] private PlayerInventory _playerInventory;

        [Tooltip("Optional — when assigned, catalog membership and slot coverage are checked. "
               + "Omit to skip those two rules (only null-guard and ownership rules apply).")]
        [SerializeField] private ShopCatalog _shopCatalog;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same VoidGameEvent as PlayerLoadout._onLoadoutChanged. "
               + "Triggers Refresh() on every loadout update.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Inspector — UI Refs (all optional) ────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Button whose interactable state mirrors loadout validity. "
               + "When null, only the panel / text are updated.")]
        [SerializeField] private Button _startMatchButton;

        [Tooltip("GameObject shown while the loadout is invalid and hidden when valid. "
               + "Useful for a warning panel or tooltip root.")]
        [SerializeField] private GameObject _validationPanel;

        [Tooltip("Text populated with all validation error messages (one per line). "
               + "Cleared to an empty string when the loadout is valid.")]
        [SerializeField] private Text _validationText;

        // ── Cached state ──────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onLoadoutChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLoadoutChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Runs <see cref="LoadoutValidator.Validate"/> and pushes the result to all
        /// wired UI elements.  Safe to call when any dependency is null.
        /// </summary>
        private void Refresh()
        {
            LoadoutValidationResult result = LoadoutValidator.Validate(
                _playerLoadout,
                _robotDefinition,
                _playerInventory,
                _shopCatalog);

            bool valid = result.IsValid;

            // Gate the start button.
            if (_startMatchButton != null)
                _startMatchButton.interactable = valid;

            // Show / hide the error panel.
            if (_validationPanel != null)
                _validationPanel.SetActive(!valid);

            // Populate the error text.
            if (_validationText != null)
            {
                if (valid)
                {
                    _validationText.text = string.Empty;
                }
                else
                {
                    // Build a newline-separated error list.  Errors list is small
                    // (typically 1–4 items) so StringBuilder is appropriate here.
                    var sb = new StringBuilder();
                    for (int i = 0; i < result.Errors.Count; i++)
                    {
                        if (i > 0) sb.Append('\n');
                        sb.Append(result.Errors[i]);
                    }
                    _validationText.text = sb.ToString();
                }
            }
        }
    }
}
