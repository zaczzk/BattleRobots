using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Mutable runtime SO that stores the currently-selected
    /// <see cref="BotDifficultyConfig"/> and persists it across scene loads.
    ///
    /// ── Write path ────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.UI.DifficultySelectionController"/> calls
    ///   <see cref="Select"/> each time the player picks a preset in the pre-match UI.
    ///
    /// ── Read path ─────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.Physics.RobotAIController"/> reads
    ///   <see cref="Current"/> in Awake.  When non-null it overrides the
    ///   inspector-level <c>_difficultyConfig</c> field, so the AI applies
    ///   the player's chosen difficulty automatically on scene load.
    ///
    /// ── Null semantics ────────────────────────────────────────────────────────
    ///   <see cref="Current"/> == null means no runtime override has been set;
    ///   RobotAIController falls back to its inspector BotDifficultyConfig.
    ///   <see cref="Reset"/> clears the override without raising the event,
    ///   e.g. to restore inspector-driven difficulty between matches.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ AI ▶ SelectedDifficulty.
    /// Assign the same SO instance to DifficultySelectionController and to
    /// every enemy RobotAIController that should honour the player's choice.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/AI/SelectedDifficulty", order = 2)]
    public sealed class SelectedDifficultySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Raised when the player picks a new difficulty. " +
                 "Subscribe in UI if a label or preview needs to update reactively.")]
        [SerializeField] private VoidGameEvent _onDifficultyChanged;

        // ── Runtime state ─────────────────────────────────────────────────────

        private BotDifficultyConfig _current;

        /// <summary>
        /// The currently-selected difficulty config, or <c>null</c> if no
        /// runtime selection has been made (AI uses its inspector config).
        /// </summary>
        public BotDifficultyConfig Current => _current;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the active difficulty config and fires <c>_onDifficultyChanged</c>.
        /// Passing <c>null</c> is allowed — it clears the override so AI controllers
        /// fall back to their inspector <c>_difficultyConfig</c> field.
        /// </summary>
        public void Select(BotDifficultyConfig config)
        {
            _current = config;
            _onDifficultyChanged?.Raise();
        }

        /// <summary>
        /// Clears the active selection without firing <c>_onDifficultyChanged</c>.
        /// AI controllers will use their inspector <c>_difficultyConfig</c> on the
        /// next scene load / Awake call.
        /// </summary>
        public void Reset()
        {
            _current = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onDifficultyChanged == null)
                Debug.LogWarning($"[SelectedDifficultySO] '{name}': " +
                                 "_onDifficultyChanged is not assigned. " +
                                 "UI will not receive reactive updates on difficulty change.");
        }
#endif
    }
}
