using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Mutable runtime SO that stores the currently-selected
    /// <see cref="MatchModifierSO"/> and persists it across scene loads
    /// (within a single play session — cleared on domain reload).
    ///
    /// ── Write path ────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.UI.MatchModifierSelectionController"/> calls
    ///   <see cref="Select"/> each time the player picks a modifier in the
    ///   pre-match UI.
    ///
    /// ── Read paths ────────────────────────────────────────────────────────────
    ///   <see cref="MatchManager"/>: reads <see cref="Current"/> at
    ///   <c>HandleMatchStarted()</c> (time multiplier) and <c>EndMatch()</c>
    ///   (reward multiplier). When <see cref="HasSelection"/> is true and
    ///   <c>Current</c> is non-null the multipliers override the unmodified values.
    ///
    ///   <see cref="BattleRobots.Physics.CombatStatsApplicator"/>: reads
    ///   <see cref="Current"/> in <c>ApplyStats()</c> to scale armor rating and
    ///   base speed before pushing values to robot components.
    ///
    /// ── Null / unset semantics ────────────────────────────────────────────────
    ///   <see cref="HasSelection"/> == false means no runtime modifier has been
    ///   chosen; all consuming systems fall back to their unmodified values.
    ///   <see cref="Reset"/> clears the selection without raising the event,
    ///   e.g. to restore default behaviour between matches.
    ///
    /// ── Runtime state lifetime ────────────────────────────────────────────────
    ///   <c>_current</c> and <c>_hasSelection</c> are NOT serialized.
    ///   They default to null/false on domain reload (Enter Play Mode, restart).
    ///   This is intentional: the pre-match UI always writes a fresh selection.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Match ▶ SelectedModifier.
    /// Assign the same SO instance to
    ///   <see cref="BattleRobots.UI.MatchModifierSelectionController"/>,
    ///   <see cref="MatchManager"/>, and every
    ///   <see cref="BattleRobots.Physics.CombatStatsApplicator"/> in the Arena.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Match/SelectedModifier", order = 3)]
    public sealed class SelectedModifierSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Raised when Select() is called. Subscribe in UI if a preview label " +
                 "or stats panel needs to update reactively on modifier change.")]
        [SerializeField] private VoidGameEvent _onModifierChanged;

        // ── Runtime state (not serialized — resets on domain reload) ──────────

        private MatchModifierSO _current;
        private bool _hasSelection;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// The currently-selected modifier SO, or <c>null</c> if no runtime
        /// selection has been made (<see cref="HasSelection"/> will be false).
        /// </summary>
        public MatchModifierSO Current => _current;

        /// <summary>
        /// True if <see cref="Select"/> has been called at least once since
        /// the last <see cref="Reset"/> or domain reload.
        /// </summary>
        public bool HasSelection => _hasSelection;

        /// <summary>
        /// Display name of the current modifier, or <c>"Standard"</c> when nothing
        /// is selected or the selected modifier has a null/whitespace display name.
        /// </summary>
        public string CurrentDisplayName =>
            _hasSelection && _current != null && !string.IsNullOrWhiteSpace(_current.DisplayName)
                ? _current.DisplayName
                : "Standard";

        /// <summary>
        /// Sets the active modifier and fires <c>_onModifierChanged</c>.
        /// Passing <c>null</c> is allowed — it clears the modifier reference while
        /// keeping <see cref="HasSelection"/> true (indicating a deliberate "no
        /// modifier" selection rather than an uninitialised state).
        /// All consuming systems null-check <see cref="Current"/> before applying
        /// multipliers, so null is safely treated as "all multipliers == 1".
        /// </summary>
        public void Select(MatchModifierSO modifier)
        {
            _current      = modifier;
            _hasSelection = true;
            _onModifierChanged?.Raise();
        }

        /// <summary>
        /// Clears the active selection without firing <c>_onModifierChanged</c>.
        /// Consuming systems (MatchManager, CombatStatsApplicator) will use their
        /// unmodified base values on the next match start.
        /// </summary>
        public void Reset()
        {
            _current      = null;
            _hasSelection = false;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onModifierChanged == null)
                Debug.LogWarning($"[SelectedModifierSO] '{name}': " +
                                 "_onModifierChanged is not assigned. " +
                                 "UI will not receive reactive updates on modifier change.");
        }
#endif
    }
}
