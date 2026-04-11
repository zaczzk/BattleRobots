using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Describes the behavioral archetype of a bot opponent.  Supplies delta / multiplier
    /// adjustments that are applied on top of (i.e. after) any <see cref="BotDifficultyConfig"/>
    /// settings inside <see cref="BattleRobots.Physics.RobotAIController"/> Awake.
    ///
    /// ── Personality archetypes ────────────────────────────────────────────────
    ///   Balanced  — neutral modifiers (all defaults).  Equivalent to no personality assigned.
    ///   Aggressive — lower attack cooldown + negative detection/attack range deltas.
    ///                Rushes in fast and attacks at maximum frequency.
    ///   Defensive  — raised detection range + raised attack range.  Keeps its distance;
    ///                engages from a safer position at a slower tempo.
    ///   Berserker  — very low attack cooldown + widened facing threshold.
    ///                Charges with reduced precision; trades accuracy for relentless pressure.
    ///   Tactical   — widened facing threshold + positive attack range delta.
    ///                Engages from variable angles with a longer effective reach.
    ///
    /// ── How each modifier is applied (in RobotAIController.Awake) ─────────────
    ///   AttackCooldownMultiplier  → final = resolved × multiplier  (Mathf.Max 0.1)
    ///   DetectionRangeDelta       → final = resolved + delta        (Mathf.Max 0)
    ///   AttackRangeDelta          → final = resolved + delta        (Mathf.Max 0)
    ///   FacingThresholdMultiplier → final = resolved × multiplier  (Mathf.Max 1)
    ///
    ///   "Resolved" = the value after BotDifficultyConfig has been applied (or the
    ///   Inspector default when no difficulty config is assigned).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO is immutable at runtime — all fields are read-only properties.
    ///   - OnValidate clamps multipliers to their minimum values so the Inspector
    ///     cannot store illegal values even if [Min] is bypassed.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ BotPersonality.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/BotPersonality",
        fileName = "BotPersonalitySO")]
    public sealed class BotPersonalitySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Behavioral archetype label.  Informational only — the four modifier " +
                 "fields below control the actual runtime effect.  Configure them to " +
                 "match the chosen archetype, or supply entirely custom values.")]
        [SerializeField] private BotPersonalityType _personalityType = BotPersonalityType.Balanced;

        [Tooltip("Multiplier applied to the resolved attack cooldown after difficulty. " +
                 "Values < 1 produce faster attacks; values > 1 slow the attack rate. " +
                 "Clamped to ≥ 0.1 (RobotAIController also enforces this floor).")]
        [SerializeField, Min(0.1f)] private float _attackCooldownMultiplier = 1f;

        [Tooltip("Signed delta (world-units) added to the resolved detection range after " +
                 "difficulty.  Positive = wider awareness; negative = shorter awareness. " +
                 "RobotAIController clamps the final value to ≥ 0.")]
        [SerializeField] private float _detectionRangeDelta;

        [Tooltip("Signed delta (world-units) added to the resolved attack range after " +
                 "difficulty.  Positive = attacks from further away; " +
                 "negative = must close in tighter.  Final value clamped to ≥ 0.")]
        [SerializeField] private float _attackRangeDelta;

        [Tooltip("Multiplier applied to the resolved facing threshold after difficulty. " +
                 "Values > 1 widen the aiming cone (looser targeting); " +
                 "values < 1 would be clamped to 1 (minimum, effectively neutral). " +
                 "Clamped to ≥ 1.")]
        [SerializeField, Min(1f)] private float _facingThresholdMultiplier = 1f;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>Behavioral archetype label.  Informational; does not drive logic.</summary>
        public BotPersonalityType PersonalityType => _personalityType;

        /// <summary>
        /// Multiplier applied to the attack cooldown after difficulty is resolved.
        /// Always ≥ 0.1 (enforced by <c>[Min(0.1f)]</c> and <see cref="OnValidate"/>).
        /// </summary>
        public float AttackCooldownMultiplier => _attackCooldownMultiplier;

        /// <summary>
        /// Signed delta added to the detection range after difficulty.
        /// The final range is clamped to ≥ 0 by <see cref="BattleRobots.Physics.RobotAIController"/>.
        /// </summary>
        public float DetectionRangeDelta => _detectionRangeDelta;

        /// <summary>
        /// Signed delta added to the attack range after difficulty.
        /// The final range is clamped to ≥ 0 by <see cref="BattleRobots.Physics.RobotAIController"/>.
        /// </summary>
        public float AttackRangeDelta => _attackRangeDelta;

        /// <summary>
        /// Multiplier applied to the facing threshold after difficulty is resolved.
        /// Always ≥ 1 (enforced by <c>[Min(1f)]</c> and <see cref="OnValidate"/>).
        /// </summary>
        public float FacingThresholdMultiplier => _facingThresholdMultiplier;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_attackCooldownMultiplier < 0.1f)
            {
                _attackCooldownMultiplier = 0.1f;
                Debug.LogWarning(
                    "[BotPersonalitySO] AttackCooldownMultiplier clamped to minimum 0.1.", this);
            }

            if (_facingThresholdMultiplier < 1f)
            {
                _facingThresholdMultiplier = 1f;
                Debug.LogWarning(
                    "[BotPersonalitySO] FacingThresholdMultiplier clamped to minimum 1.0.", this);
            }
        }
#endif
    }

    /// <summary>
    /// Behavioral archetype label for <see cref="BotPersonalitySO"/>.
    /// Informational — the personality's modifier fields drive the actual effect.
    /// </summary>
    public enum BotPersonalityType
    {
        /// <summary>Neutral modifiers — equivalent to no personality assigned.</summary>
        Balanced,

        /// <summary>Reduced cooldown, closes distance fast; attacks at max frequency.</summary>
        Aggressive,

        /// <summary>Increased detection / attack range; engages from a safer distance.</summary>
        Defensive,

        /// <summary>Very low cooldown, wide facing cone; charges relentlessly.</summary>
        Berserker,

        /// <summary>Wide facing threshold, extended attack range; variable engagement angle.</summary>
        Tactical,
    }
}
