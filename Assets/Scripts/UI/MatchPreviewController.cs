using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Read-only pre-match summary panel that consolidates all player selections
    /// (opponent, arena, modifier, difficulty) and current build readiness
    /// (tier, power rating) into a single reactive display panel.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   Subscribes to <c>_onSelectionsChanged</c>. When the event fires — or on
    ///   <c>OnEnable</c> — reads the current state of all optional SO data sources
    ///   and refreshes every wired Text label.  Missing or null SOs silently fall
    ///   back to sensible placeholder strings so the panel is always displayable.
    ///
    /// ── Typical wiring ────────────────────────────────────────────────────────
    ///   Place this component on the pre-match panel that shows before "Start Match".
    ///   Wire a single <c>VoidGameEvent</c> SO to <c>_onSelectionsChanged</c> and
    ///   raise it from <see cref="OpponentSelectionController"/>,
    ///   <see cref="ArenaSelectionController"/>, <see cref="MatchModifierSelectionController"/>,
    ///   and <see cref="DifficultySelectionController"/> whenever a player changes a
    ///   selection.  The preview panel then refreshes automatically.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields are optional — any subset may be wired.
    ///   - Delegate cached in Awake; zero heap allocations after Awake.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to the pre-match panel root GameObject.
    ///   2. Under "Selections (all optional)" assign any subset of the four
    ///      SelectedXxxSO assets already used by their respective selection controllers.
    ///   3. Under "Build Readiness (optional)" assign <c>_buildRating</c> and
    ///      optionally <c>_tierConfig</c> to show tier + power.
    ///   4. Under "Event Channel — In" assign <c>_onSelectionsChanged</c> — fire
    ///      this VoidGameEvent from all selection controllers that change state.
    ///   5. Under "Labels (all optional)" assign as many Text fields as needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchPreviewController : MonoBehaviour
    {
        // ── Inspector — selections ────────────────────────────────────────────

        [Header("Selections (all optional)")]
        [Tooltip("Reads CurrentDisplayName and Current.Description for the opponent labels. " +
                 "Assign the same SelectedOpponentSO used by OpponentSelectionController.")]
        [SerializeField] private SelectedOpponentSO _selectedOpponent;

        [Tooltip("Reads CurrentDisplayName for the arena label. " +
                 "Assign the same SelectedArenaSO used by ArenaSelectionController.")]
        [SerializeField] private SelectedArenaSO _selectedArena;

        [Tooltip("Reads CurrentDisplayName and Current.Description for the modifier labels. " +
                 "Assign the same SelectedModifierSO used by MatchModifierSelectionController.")]
        [SerializeField] private SelectedModifierSO _selectedModifier;

        [Tooltip("Reads Current.name as the difficulty label. " +
                 "Assign the same SelectedDifficultySO used by DifficultySelectionController.")]
        [SerializeField] private SelectedDifficultySO _selectedDifficulty;

        // ── Inspector — build readiness ────────────────────────────────────────

        [Header("Build Readiness (optional)")]
        [Tooltip("Provides CurrentRating for the Power label, and the rating input to EvaluateTier. " +
                 "Assign the shared BuildRatingSO asset (same one used by BuildRatingController).")]
        [SerializeField] private BuildRatingSO _buildRating;

        [Tooltip("Provides GetDisplayName(tier) for the Tier label. " +
                 "Requires _buildRating to also be assigned. " +
                 "Leave null to omit the tier display or show an empty label.")]
        [SerializeField] private RobotTierConfig _tierConfig;

        // ── Inspector — event channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("VoidGameEvent raised whenever any selection changes. " +
                 "Wire a shared 'any selection changed' broadcast event here, or the same " +
                 "event raised by individual selection controllers.  Leave null if the panel " +
                 "only needs to populate once on enable (no live refresh required).")]
        [SerializeField] private VoidGameEvent _onSelectionsChanged;

        // ── Inspector — labels (all optional) ────────────────────────────────

        [Header("Labels (all optional)")]
        [Tooltip("Opponent display name label, e.g. 'Ironclad Bob'. " +
                 "Falls back to 'Opponent' when no opponent SO is assigned.")]
        [SerializeField] private Text _opponentNameText;

        [Tooltip("Opponent flavour / lore description. " +
                 "Shows the profile description when an opponent is selected; empty otherwise.")]
        [SerializeField] private Text _opponentDescriptionText;

        [Tooltip("Arena display name label, e.g. 'Lava Pit'. " +
                 "Falls back to 'Arena' when no arena SO is assigned.")]
        [SerializeField] private Text _arenaNameText;

        [Tooltip("Match modifier display name label, e.g. 'Double Rewards'. " +
                 "Falls back to 'Standard' when no modifier SO is assigned.")]
        [SerializeField] private Text _modifierNameText;

        [Tooltip("Match modifier description text. " +
                 "Shows the modifier description when a modifier is selected; empty otherwise.")]
        [SerializeField] private Text _modifierDescriptionText;

        [Tooltip("Difficulty label derived from the BotDifficultyConfig asset name, e.g. 'Hard'. " +
                 "Empty when no difficulty has been selected via SelectedDifficultySO.")]
        [SerializeField] private Text _difficultyNameText;

        [Tooltip("Build tier label, e.g. 'Gold'. " +
                 "Requires both _buildRating and _tierConfig to be assigned; empty otherwise.")]
        [SerializeField] private Text _tierText;

        [Tooltip("Power rating label, e.g. 'Power: 420'. " +
                 "Requires _buildRating; shows 'Power: 0' when _buildRating is null.")]
        [SerializeField] private Text _ratingText;

        // ── Private ───────────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onSelectionsChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onSelectionsChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads all wired SO data sources and updates every assigned Text label.
        /// Safe to call with any combination of null SO and Text references.
        /// Called automatically on <see cref="OnEnable"/> and whenever
        /// <c>_onSelectionsChanged</c> fires.
        /// </summary>
        public void Refresh()
        {
            // ── Opponent ──────────────────────────────────────────────────────

            if (_opponentNameText != null)
                _opponentNameText.text =
                    _selectedOpponent?.CurrentDisplayName ?? "Opponent";

            if (_opponentDescriptionText != null)
                _opponentDescriptionText.text =
                    _selectedOpponent?.Current?.Description ?? string.Empty;

            // ── Arena ─────────────────────────────────────────────────────────

            if (_arenaNameText != null)
                _arenaNameText.text =
                    _selectedArena?.CurrentDisplayName ?? "Arena";

            // ── Modifier ──────────────────────────────────────────────────────

            if (_modifierNameText != null)
                _modifierNameText.text =
                    _selectedModifier?.CurrentDisplayName ?? "Standard";

            if (_modifierDescriptionText != null)
                _modifierDescriptionText.text =
                    _selectedModifier?.Current?.Description ?? string.Empty;

            // ── Difficulty ────────────────────────────────────────────────────
            // BotDifficultyConfig has no dedicated DisplayName property;
            // use the SO asset name (set by the designer, e.g. "Easy", "Normal", "Hard").

            if (_difficultyNameText != null)
                _difficultyNameText.text =
                    _selectedDifficulty?.Current?.name ?? string.Empty;

            // ── Tier ──────────────────────────────────────────────────────────
            // Requires both _buildRating and _tierConfig.

            if (_tierText != null)
            {
                if (_buildRating != null && _tierConfig != null)
                {
                    RobotTierLevel tier = RobotTierEvaluator.EvaluateTier(_buildRating, _tierConfig);
                    _tierText.text = _tierConfig.GetDisplayName(tier);
                }
                else
                {
                    _tierText.text = string.Empty;
                }
            }

            // ── Power rating ──────────────────────────────────────────────────

            if (_ratingText != null)
                _ratingText.text = _buildRating != null
                    ? string.Concat("Power: ", _buildRating.CurrentRating.ToString())
                    : "Power: 0";
        }
    }
}
