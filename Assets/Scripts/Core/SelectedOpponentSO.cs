using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Mutable runtime SO that stores the currently-selected opponent profile
    /// and persists it across scene loads.
    ///
    /// ── Write path ────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.UI.OpponentSelectionController"/> calls
    ///   <see cref="Select"/> each time the player picks an opponent in the
    ///   pre-match lobby UI.
    ///
    /// ── Read path ─────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.Physics.RobotAIController"/> reads
    ///   <see cref="Current"/> in Awake.  When <see cref="HasSelection"/> is true
    ///   and the profile carries non-null DifficultyConfig / Personality references,
    ///   those values override any previously applied difficulty/personality settings
    ///   (they run last in the Awake override chain, so opponent values always win).
    ///   <see cref="MatchManager"/> writes <see cref="OpponentProfileSO.DisplayName"/>
    ///   into the MatchRecord so match history can show the opponent's name.
    ///
    /// ── Null / empty semantics ─────────────────────────────────────────────────
    ///   <see cref="HasSelection"/> == false (e.g. after <see cref="Reset"/>) means
    ///   no runtime override is active; RobotAIController uses its existing
    ///   inspector-driven BotDifficultyConfig / BotPersonalitySO settings.
    ///   <see cref="Reset"/> clears the selection without firing the event —
    ///   safe to call between scenes to restore inspector-driven behavior.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ SelectedOpponent.
    /// Assign the same SO instance to OpponentSelectionController (writes),
    /// every enemy RobotAIController (reads), and MatchManager (records name).
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/SelectedOpponent", order = 12)]
    public sealed class SelectedOpponentSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Raised when the player selects a new opponent. " +
                 "Subscribe in UI if a label or preview needs to update reactively.")]
        [SerializeField] private VoidGameEvent _onOpponentSelected;

        // ── Runtime state ─────────────────────────────────────────────────────

        private OpponentProfileSO _current;
        private bool              _hasSelection;

        /// <summary>The active opponent profile, or <c>null</c> before any selection is made.</summary>
        public OpponentProfileSO Current => _current;

        /// <summary>True after <see cref="Select"/> has been called at least once this session.</summary>
        public bool HasSelection => _hasSelection;

        /// <summary>
        /// The display name of the current selection.
        /// Returns "Opponent" when no selection has been made or the profile has no name.
        /// </summary>
        public string CurrentDisplayName =>
            (_hasSelection && _current != null && !string.IsNullOrWhiteSpace(_current.DisplayName))
                ? _current.DisplayName
                : "Opponent";

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the active opponent profile and fires <c>_onOpponentSelected</c>.
        /// Passing <c>null</c> is allowed — it clears the profile while still marking
        /// <see cref="HasSelection"/> true (meaning "no opponent" was explicitly chosen).
        /// Use <see cref="Reset"/> instead to fully clear the selection.
        /// </summary>
        public void Select(OpponentProfileSO profile)
        {
            _current      = profile;
            _hasSelection = true;
            _onOpponentSelected?.Raise();
        }

        /// <summary>
        /// Clears the active selection without firing <c>_onOpponentSelected</c>.
        /// AI controllers will use their inspector-level config on the next Awake call.
        /// </summary>
        public void Reset()
        {
            _current      = null;
            _hasSelection = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onOpponentSelected == null)
                Debug.LogWarning($"[SelectedOpponentSO] '{name}': " +
                                 "_onOpponentSelected is not assigned. " +
                                 "UI will not receive reactive updates on opponent change.");
        }
#endif
    }
}
