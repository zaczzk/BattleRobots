using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Autonomous ability-activation companion for AI-controlled robots.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   Checks two independent conditions on a fixed interval and calls
    ///   <see cref="AbilityController.TryActivate"/> when either is met:
    ///   <list type="bullet">
    ///     <item><b>Health condition</b> — robot's health ratio falls at or below
    ///       <see cref="_useAbilityBelowHealthRatio"/> (e.g., use ability when wounded).</item>
    ///     <item><b>Distance condition</b> — target is within squared-distance
    ///       <see cref="_useAbilityBelowDistanceSqr"/> (e.g., use ability in melee range).</item>
    ///   </list>
    ///   Either condition independently triggers a <see cref="AbilityController.TryActivate"/>
    ///   call; AbilityController handles cooldown and energy guards.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB alongside <see cref="AbilityController"/> and
    ///      <see cref="RobotAIController"/> on the AI robot root.
    ///   2. Assign <c>_abilityController</c> → the robot's AbilityController.
    ///   3. Optionally assign <c>_health</c> → the robot's HealthSO to enable the
    ///      health-based trigger.
    ///   4. Optionally assign <c>_target</c> → the player robot's Transform (or call
    ///      SetTarget() at runtime from MatchFlowController / MatchManager).
    ///   5. Tune <c>_useAbilityBelowHealthRatio</c>, <c>_useAbilityBelowDistanceSqr</c>,
    ///      and <c>_checkInterval</c> in the Inspector.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.Physics namespace — no UI references.
    ///   • FixedUpdate allocates nothing — only float arithmetic and sqrMagnitude.
    ///   • AbilityController is responsible for cooldown / energy enforcement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BotAIAbilityController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Ability")]
        [Tooltip("AbilityController on this robot. Leave null to disable AI ability use.")]
        [SerializeField] private AbilityController _abilityController;

        [Header("Condition — Health")]
        [Tooltip("Optional HealthSO for this robot. When assigned, the health condition " +
                 "is active: ability triggers when CurrentHealth/MaxHealth ≤ this ratio.")]
        [SerializeField] private HealthSO _health;

        [Tooltip("Health ratio threshold [0.01–1] below which the ability is triggered. " +
                 "0.5 = trigger when at 50% health or below.")]
        [SerializeField, Range(0.01f, 1f)] private float _useAbilityBelowHealthRatio = 0.5f;

        [Header("Condition — Distance")]
        [Tooltip("Target Transform to measure distance against. When assigned, the distance " +
                 "condition is active: ability triggers when sqrMagnitude ≤ this value.")]
        [SerializeField] private Transform _target;

        [Tooltip("Squared distance threshold. Ability triggers when the target is within " +
                 "this squared distance. Default 25 = 5 m radius. Use sqrMagnitude to " +
                 "avoid a Sqrt each check.")]
        [SerializeField, Min(0f)] private float _useAbilityBelowDistanceSqr = 25f;

        [Header("Interval")]
        [Tooltip("How often (seconds) to evaluate ability conditions. " +
                 "Lower values react faster at higher CPU cost.")]
        [SerializeField, Min(0.1f)] private float _checkInterval = 1f;

        // ── Runtime state (not serialized) ────────────────────────────────────

        private float _checkTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Stagger first check to avoid all AI robots firing simultaneously on spawn.
            _checkTimer = _checkInterval;
        }

        private void FixedUpdate()
        {
            _checkTimer -= Time.fixedDeltaTime;
            if (_checkTimer > 0f) return;

            _checkTimer = _checkInterval;
            EvaluateAbility();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Checks both activation conditions. Calls TryActivate when either is met.
        /// No heap allocation — struct vector ops and float arithmetic only.
        /// </summary>
        private void EvaluateAbility()
        {
            if (_abilityController == null) return;

            bool healthLow = _health != null
                && !_health.IsDead
                && _health.MaxHealth > 0f
                && (_health.CurrentHealth / _health.MaxHealth) <= _useAbilityBelowHealthRatio;

            bool targetClose = _target != null
                && (_target.position - transform.position).sqrMagnitude <= _useAbilityBelowDistanceSqr;

            if (healthLow || targetClose)
                _abilityController.TryActivate();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sets or replaces the target Transform used by the distance condition.
        /// Call from MatchFlowController or RobotAIController when the target is assigned.
        /// Allocation-free.
        /// </summary>
        public void SetTarget(Transform target) => _target = target;
    }
}
