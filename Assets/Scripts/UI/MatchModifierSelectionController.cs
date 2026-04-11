using BattleRobots.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that lets the player cycle through available match modifiers
    /// in the pre-match lobby or main-menu UI.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • Reads the ordered modifier list from <see cref="MatchModifierCatalogSO"/>.
    ///   • Writes the selected <see cref="MatchModifierSO"/> to
    ///     <see cref="SelectedModifierSO"/> immediately on each navigation step
    ///     so <see cref="MatchManager"/> and
    ///     <see cref="BattleRobots.Physics.CombatStatsApplicator"/> read the correct
    ///     modifier at Arena-scene startup.
    ///   • Updates optional name and description labels on every selection change.
    ///   • Previous / Next buttons cycle wrap-around through the modifier list.
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   BattleRobots.UI namespace — no BattleRobots.Physics references.
    ///   Button-listener delegates cached in Awake; zero per-frame allocations.
    ///   No Update method.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_catalog</c> (MatchModifierCatalogSO SO).
    ///   2. Assign <c>_selectedModifier</c> (SelectedModifierSO — the same instance
    ///      wired to MatchManager._selectedModifier and all CombatStatsApplicator
    ///      components in the Arena scene).
    ///   3. Optionally assign <c>_nameLabel</c>, <c>_descriptionLabel</c>,
    ///      <c>_prevButton</c>, <c>_nextButton</c> — null-safe; buttons may also
    ///      be wired directly to <c>PreviousModifier()</c> / <c>NextModifier()</c>
    ///      via Button.onClick in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchModifierSelectionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Immutable SO listing all available match modifiers in selection order.")]
        [SerializeField] private MatchModifierCatalogSO _catalog;

        [Tooltip("Mutable SO that stores the chosen modifier across scene loads. " +
                 "Assign the same instance to MatchManager and every " +
                 "CombatStatsApplicator in the Arena scene.")]
        [SerializeField] private SelectedModifierSO _selectedModifier;

        [Header("UI (optional)")]
        [Tooltip("Label displaying the current modifier's display name. May be null.")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("Label displaying the current modifier's description. May be null.")]
        [SerializeField] private Text _descriptionLabel;

        [Tooltip("Button that cycles to the previous modifier (wrap-around). May be null.")]
        [SerializeField] private Button _prevButton;

        [Tooltip("Button that cycles to the next modifier (wrap-around). May be null.")]
        [SerializeField] private Button _nextButton;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _selectedIndex;

        /// <summary>Current zero-based index into <see cref="MatchModifierCatalogSO.Modifiers"/>.</summary>
        public int SelectedIndex => _selectedIndex;

        // ── Cached delegates ──────────────────────────────────────────────────

        private UnityAction _prevAction;
        private UnityAction _nextAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _prevAction = PreviousModifier;
            _nextAction = NextModifier;
        }

        private void OnEnable()
        {
            _prevButton?.onClick.AddListener(_prevAction);
            _nextButton?.onClick.AddListener(_nextAction);
            // Apply the displayed selection immediately so SelectedModifierSO
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
        /// Advances to the next match modifier.
        /// Wraps from the last modifier back to the first.
        /// </summary>
        public void NextModifier()
        {
            if (_catalog == null || _catalog.Modifiers.Count == 0) return;

            _selectedIndex = (_selectedIndex + 1) % _catalog.Modifiers.Count;
            ApplySelection();
        }

        /// <summary>
        /// Steps back to the previous match modifier.
        /// Wraps from the first modifier around to the last.
        /// </summary>
        public void PreviousModifier()
        {
            if (_catalog == null || _catalog.Modifiers.Count == 0) return;

            _selectedIndex = (_selectedIndex - 1 + _catalog.Modifiers.Count) % _catalog.Modifiers.Count;
            ApplySelection();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplySelection()
        {
            if (_catalog == null || _catalog.Modifiers.Count == 0)
            {
                if (_nameLabel        != null) _nameLabel.text        = "\u2014"; // em-dash
                if (_descriptionLabel != null) _descriptionLabel.text = string.Empty;
                return;
            }

            var modifier = _catalog.Modifiers[_selectedIndex];
            _selectedModifier?.Select(modifier);

            if (_nameLabel != null)
                _nameLabel.text = modifier?.DisplayName ?? "\u2014";

            if (_descriptionLabel != null)
                _descriptionLabel.text = modifier?.Description ?? string.Empty;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_catalog == null)
                Debug.LogWarning("[MatchModifierSelectionController] _catalog is not assigned.", this);
            if (_selectedModifier == null)
                Debug.LogWarning("[MatchModifierSelectionController] _selectedModifier is not assigned.", this);
        }
#endif
    }
}
