using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Named difficulty presets available at match setup.
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
    }

    /// <summary>
    /// ScriptableObject that encapsulates all tuneable parameters for game difficulty.
    ///
    /// <para>Consumers:</para>
    /// <list type="bullet">
    ///   <item><c>RobotFSM</c> — scales AI detection ranges and drive speed.</item>
    ///   <item><c>DamageDealer</c> — multiplies damage dealt per impact.</item>
    ///   <item><c>MatchManager</c> — scales the arena time limit.</item>
    /// </list>
    ///
    /// <para>
    /// Assets are immutable at runtime; only the designated mutator
    /// <see cref="LoadPreset"/> may write to serialised fields.
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Difficulty ▶ DifficultySO
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Difficulty/DifficultySO", order = 0)]
    public sealed class DifficultySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Display name shown in the difficulty selector UI.")]
        [SerializeField] private string _difficultyName = "Medium";

        [Header("AI Behaviour")]
        [Tooltip("How aggressively the AI pursues the player. " +
                 "Controls detection range multiplier and is used by RobotFSM.\n" +
                 "Formula: effectiveRange = baseRange × (0.5 + aggressionScale). " +
                 "At 0.5 (Medium default) ranges are unchanged; at 0.9 (Hard) they are 1.4× base.")]
        [SerializeField, Range(0.1f, 1f)] private float _aiAggressionScale = 0.5f;

        [Tooltip("Multiplier applied to the AI drive speed set in RobotFSM. " +
                 "1.0 = default inspector speed.")]
        [SerializeField, Range(0.25f, 1.5f)] private float _aiDriveSpeedScale = 1f;

        [Header("Damage")]
        [Tooltip("Multiplier applied to every DamageDealer impact in the match. " +
                 "1.0 = no change; 2.0 = double damage.")]
        [SerializeField, Range(0.5f, 2f)] private float _damageMultiplier = 1f;

        [Header("Time Limit")]
        [Tooltip("Multiplier applied to ArenaConfig.TimeLimitSeconds at match start. " +
                 "1.0 = standard time; values > 1.0 give more time (easier).")]
        [SerializeField, Range(0.5f, 2f)] private float _timeLimitScale = 1f;

        // ── Public API (read-only) ─────────────────────────────────────────────

        /// <summary>Display name for this difficulty setting, e.g. "Easy".</summary>
        public string DifficultyName     => _difficultyName;

        /// <summary>
        /// AI detection-range aggression [0.1, 1.0].
        /// RobotFSM computes: <c>effectiveRange = baseRange × (0.5 + AiAggressionScale)</c>.
        /// Medium default of 0.5 leaves base ranges unchanged.
        /// </summary>
        public float  AiAggressionScale  => _aiAggressionScale;

        /// <summary>AI drive-speed multiplier [0.25, 1.5].</summary>
        public float  AiDriveSpeedScale  => _aiDriveSpeedScale;

        /// <summary>Per-impact damage multiplier [0.5, 2.0].</summary>
        public float  DamageMultiplier   => _damageMultiplier;

        /// <summary>Time-limit scale factor [0.5, 2.0].</summary>
        public float  TimeLimitScale     => _timeLimitScale;

        // ── Designated Mutator ────────────────────────────────────────────────

        /// <summary>
        /// Overwrites all fields with values from the chosen preset.
        /// This is the only method permitted to mutate a DifficultySO asset;
        /// it should only be called from menu/settings code, never from hot paths.
        /// </summary>
        /// <param name="level">The preset to apply.</param>
        public void LoadPreset(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Easy:
                    _difficultyName    = "Easy";
                    _aiAggressionScale = 0.25f;
                    _aiDriveSpeedScale = 0.6f;
                    _damageMultiplier  = 0.6f;
                    _timeLimitScale    = 1.5f;
                    break;

                case DifficultyLevel.Medium:
                    _difficultyName    = "Medium";
                    _aiAggressionScale = 0.5f;
                    _aiDriveSpeedScale = 1f;
                    _damageMultiplier  = 1f;
                    _timeLimitScale    = 1f;
                    break;

                case DifficultyLevel.Hard:
                    _difficultyName    = "Hard";
                    _aiAggressionScale = 0.9f;
                    _aiDriveSpeedScale = 1.35f;
                    _damageMultiplier  = 1.5f;
                    _timeLimitScale    = 0.75f;
                    break;
            }
        }

        // ── Static Factory (test / bootstrapping helper) ──────────────────────

        /// <summary>
        /// Creates an in-memory <see cref="DifficultySO"/> instance pre-loaded with
        /// the given preset. Useful in tests and runtime preset spawning.
        /// The caller is responsible for <c>Object.DestroyImmediate</c> when done.
        /// </summary>
        public static DifficultySO CreatePreset(DifficultyLevel level)
        {
            var so = CreateInstance<DifficultySO>();
            so.LoadPreset(level);
            return so;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_aiAggressionScale <= 0f)
                UnityEngine.Debug.LogWarning("[DifficultySO] AiAggressionScale must be > 0.", this);
        }
#endif
    }
}
