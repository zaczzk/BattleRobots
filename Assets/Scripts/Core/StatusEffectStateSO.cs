using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime blackboard SO that mirrors the active status-effect state of a single robot.
    ///
    /// ── Purpose ────────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.Physics.StatusEffectController"/> (Physics namespace) cannot
    ///   be referenced by UI classes. This SO acts as an intermediary: the controller
    ///   calls <see cref="UpdateState"/> whenever effects change, and the UI reads the
    ///   cached values on the next event-driven Refresh without ever touching Physics types.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   StatusEffectController ──UpdateState()──► StatusEffectStateSO
    ///   StatusEffectStateSO    ──_onEffectsChanged──► StatusEffectHUDController
    ///   StatusEffectHUDController reads IsBurnActive / BurnTimeRemaining etc. from this SO.
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   • Immutable at runtime via designated mutator: only UpdateState() and Reset()
    ///     may write fields — consistent with SO architecture rules.
    ///   • Zero heap allocation on the hot path — all value types.
    ///   • BattleRobots.Core only — no Physics / UI namespace references.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/StatusEffectState")]
    public sealed class StatusEffectStateSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel (optional)")]
        [Tooltip("Raised inside UpdateState() after state is written. " +
                 "StatusEffectHUDController subscribes to this to reactively refresh the UI. " +
                 "Can be the same VoidGameEvent as StatusEffectController._onEffectsChanged, " +
                 "or a dedicated display-only event. Leave null to skip (no UI reaction).")]
        [SerializeField] private VoidGameEvent _onEffectsChanged;

        // ── Runtime state (not serialised — pure blackboard) ──────────────────

        private bool  _isBurnActive;
        private float _burnTimeRemaining;

        private bool  _isStunActive;
        private float _stunTimeRemaining;

        private bool  _isSlowActive;
        private float _slowTimeRemaining;
        private float _currentSlowFactor = 1f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while a Burn effect is active.</summary>
        public bool  IsBurnActive      => _isBurnActive;

        /// <summary>Seconds remaining on the active Burn effect (0 when inactive).</summary>
        public float BurnTimeRemaining => _burnTimeRemaining;

        /// <summary>True while a Stun effect is active.</summary>
        public bool  IsStunActive      => _isStunActive;

        /// <summary>Seconds remaining on the active Stun effect (0 when inactive).</summary>
        public float StunTimeRemaining => _stunTimeRemaining;

        /// <summary>True while a Slow effect is active.</summary>
        public bool  IsSlowActive      => _isSlowActive;

        /// <summary>Seconds remaining on the active Slow effect (0 when inactive).</summary>
        public float SlowTimeRemaining => _slowTimeRemaining;

        /// <summary>
        /// Speed multiplier from any active Slow effect ([0.01, 1]).
        /// Returns 1.0 when no Slow is active (no slowdown).
        /// </summary>
        public float CurrentSlowFactor => _currentSlowFactor;

        /// <summary>True when at least one of Burn, Stun, or Slow is active.</summary>
        public bool AnyEffectActive    => _isBurnActive || _isStunActive || _isSlowActive;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <c>StatusEffectController</c> whenever its slot array changes
        /// (ApplyEffect, Clear, and post-expiry compact in FixedUpdate).
        /// Writes the six state values then fires <see cref="_onEffectsChanged"/> once
        /// so subscribed UI reacts without polling.
        /// Zero allocation — all value-type parameters.
        /// </summary>
        public void UpdateState(
            bool  burnActive,  float burnTime,
            bool  stunActive,  float stunTime,
            bool  slowActive,  float slowTime,  float slowFactor)
        {
            _isBurnActive      = burnActive;
            _burnTimeRemaining = burnActive  ? burnTime  : 0f;

            _isStunActive      = stunActive;
            _stunTimeRemaining = stunActive  ? stunTime  : 0f;

            _isSlowActive      = slowActive;
            _slowTimeRemaining = slowActive  ? slowTime  : 0f;
            _currentSlowFactor = slowActive  ? slowFactor : 1f;

            _onEffectsChanged?.Raise();
        }

        /// <summary>
        /// Silently clears all cached state without raising <see cref="_onEffectsChanged"/>.
        /// Call at match start or robot death before the controller's own Clear()
        /// so the HUD reflects the reset without receiving a spurious event.
        /// </summary>
        public void Reset()
        {
            _isBurnActive      = false;
            _burnTimeRemaining = 0f;

            _isStunActive      = false;
            _stunTimeRemaining = 0f;

            _isSlowActive      = false;
            _slowTimeRemaining = 0f;
            _currentSlowFactor = 1f;
        }
    }
}
