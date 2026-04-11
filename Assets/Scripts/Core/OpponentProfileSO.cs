using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data bundle that describes one selectable opponent.
    ///
    /// Each profile pairs a human-readable name and description with optional
    /// SO overrides for AI difficulty and behavioral personality.  When the player
    /// selects an opponent, <see cref="SelectedOpponentSO"/> carries the active
    /// profile into the Arena scene, where <see cref="BattleRobots.Physics.RobotAIController"/>
    /// applies the bundled overrides in Awake() — after the existing
    /// BotDifficultyConfig / SelectedDifficultySO / BotPersonalitySO chain —
    /// so opponent settings always take final priority.
    ///
    /// ── Optional fields ────────────────────────────────────────────────────────
    ///   All SO reference fields are optional.  Leave any null to keep the AI
    ///   controller's inspector-level values for that aspect.
    ///   • <see cref="DifficultyConfig"/> — overrides AI tuning parameters.
    ///   • <see cref="Personality"/>     — overrides AI behavior modifiers.
    ///   • <see cref="RobotDefinition"/> — reserved for future stat-driven enemy builds.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ OpponentProfile.
    /// Add instances to an <see cref="OpponentRosterSO"/> for player-facing selection.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/OpponentProfile", order = 10)]
    public sealed class OpponentProfileSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Name shown in the opponent-selection UI and written to MatchRecord.opponentName.")]
        [SerializeField] private string _displayName = "";

        [Tooltip("Optional flavour text shown beneath the opponent name (lore, taunt, etc.).")]
        [SerializeField, TextArea(2, 4)] private string _description = "";

        [Header("AI Overrides (optional)")]
        [Tooltip("When assigned, these tuning values replace the AI controller's " +
                 "BotDifficultyConfig and SelectedDifficultySO at match start. " +
                 "Leave null to keep the AI's inspector difficulty.")]
        [SerializeField] private BotDifficultyConfig _difficultyConfig;

        [Tooltip("When assigned, these multiplier/delta values are applied on top of " +
                 "the resolved difficulty settings — identical to _botPersonality in " +
                 "RobotAIController.  Leave null for neutral behavior.")]
        [SerializeField] private BotPersonalitySO _personality;

        [Header("Robot (optional)")]
        [Tooltip("RobotDefinition SO that describes the enemy robot's part slots and base stats. " +
                 "Reserved for future stat-driven builds; not applied at runtime in this release.")]
        [SerializeField] private RobotDefinition _robotDefinition;

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>Name shown in the opponent-selection UI and saved to the MatchRecord.</summary>
        public string DisplayName => _displayName;

        /// <summary>Optional flavour/lore text. May be an empty string.</summary>
        public string Description => _description;

        /// <summary>
        /// Optional AI difficulty override.  When non-null, RobotAIController
        /// applies these values in Awake() after all other difficulty overrides.
        /// </summary>
        public BotDifficultyConfig DifficultyConfig => _difficultyConfig;

        /// <summary>
        /// Optional AI personality override.  When non-null, RobotAIController
        /// applies these modifiers in Awake() after the DifficultyConfig override.
        /// </summary>
        public BotPersonalitySO Personality => _personality;

        /// <summary>
        /// Optional RobotDefinition for the enemy robot.
        /// Reserved for future stat-driven enemy builds; not applied at runtime currently.
        /// </summary>
        public RobotDefinition RobotDefinition => _robotDefinition;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_displayName))
                Debug.LogWarning($"[OpponentProfileSO] '{name}': " +
                                 "_displayName is empty. Set a display name for this opponent.");
        }
#endif
    }
}
