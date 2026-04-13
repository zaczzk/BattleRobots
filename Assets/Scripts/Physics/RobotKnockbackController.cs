using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that applies an ArticulationBody impulse to this robot whenever
    /// it receives a damage hit via the SO event bus.
    ///
    /// ── Knockback rules ──────────────────────────────────────────────────────────
    ///   On each damage event the impulse magnitude is computed as:
    ///     magnitude = Clamp(info.amount × _knockbackForcePerDamage, 0, _maxKnockbackForce)
    ///   Direction = (transform.position − info.hitPoint).normalized when hitPoint is
    ///   non-zero (projectile or AoE hit); falls back to −transform.forward when the
    ///   hit-point is Vector3.zero (melee / event-only damage with no world position).
    ///   The impulse is applied via <c>ArticulationBody.AddForce(ForceMode.Impulse)</c>
    ///   on the root ArticulationBody, producing a physics-accurate shove proportional
    ///   to the incoming damage.
    ///
    /// ── Integration ──────────────────────────────────────────────────────────────
    ///   1. Add <c>RobotKnockbackController</c> to the robot root GameObject (same as
    ///      <c>DamageReceiver</c> and the root <c>ArticulationBody</c>).
    ///   2. Assign <c>_articulationBody</c> → the root ArticulationBody.
    ///   3. Assign <c>_onDamageTaken</c> → the same DamageGameEvent channel that
    ///      DamageReceiver's DamageGameEventListener subscribes to.
    ///   4. Tune <c>_knockbackForcePerDamage</c> and <c>_maxKnockbackForce</c> per
    ///      robot mass (heavier robots need higher values to feel the shove).
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   • ArticulationBody only (no Rigidbody). Impulse is applied to the root body;
    ///     child joints propagate the force naturally via the solver.
    ///   • BattleRobots.Physics namespace; no UI references.
    ///   • Zero heap allocation on the hot path — struct DamageInfo param, cached delegate.
    ///   • DisallowMultipleComponent — one knockback source per robot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotKnockbackController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Physics Body")]
        [Tooltip("Root ArticulationBody of this robot. The impulse is applied here; " +
                 "child joints propagate it naturally. Leave null to disable knockback " +
                 "(backwards-compatible — safe no-op).")]
        [SerializeField] private ArticulationBody _articulationBody;

        [Header("Event Channel (optional)")]
        [Tooltip("DamageGameEvent channel this robot subscribes to for incoming hits. " +
                 "Should be the same channel wired to the DamageReceiver. Leave null to " +
                 "disable event-driven knockback.")]
        [SerializeField] private DamageGameEvent _onDamageTaken;

        [Header("Knockback Tuning")]
        [Tooltip("Impulse force units applied per point of incoming damage. " +
                 "E.g., 1.0 means a 20-damage hit applies 20 N·s impulse (before clamping).")]
        [SerializeField, Min(0f)] private float _knockbackForcePerDamage = 1f;

        [Tooltip("Maximum impulse magnitude regardless of damage amount. " +
                 "Prevents single hits from launching robots into orbit.")]
        [SerializeField, Min(0f)] private float _maxKnockbackForce = 20f;

        // ── Private state ─────────────────────────────────────────────────────

        private System.Action<DamageInfo> _knockbackDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _knockbackDelegate = OnDamageTaken;
        }

        private void OnEnable()
        {
            _onDamageTaken?.RegisterCallback(_knockbackDelegate);
        }

        private void OnDisable()
        {
            _onDamageTaken?.UnregisterCallback(_knockbackDelegate);
        }

        // ── Event handler ─────────────────────────────────────────────────────

        /// <summary>
        /// Receives a DamageInfo hit and applies an ArticulationBody impulse.
        /// No-op when <see cref="_articulationBody"/> is null.
        /// Zero allocation — DamageInfo is a struct; all arithmetic is value-type.
        /// </summary>
        private void OnDamageTaken(DamageInfo info)
        {
            if (_articulationBody == null) return;

            // Compute knockback direction: away from the hit point, or −forward as fallback.
            Vector3 direction;
            if (info.hitPoint != Vector3.zero)
            {
                Vector3 away = transform.position - info.hitPoint;
                direction = away.sqrMagnitude > 0.0001f
                    ? away.normalized
                    : -transform.forward;
            }
            else
            {
                direction = -transform.forward;
            }

            float magnitude = Mathf.Clamp(
                info.amount * _knockbackForcePerDamage,
                0f,
                _maxKnockbackForce);

            _articulationBody.AddForce(direction * magnitude, ForceMode.Impulse);
        }

        // ── Public API (inspector-exposed tuning) ──────────────────────────────

        /// <summary>Current force-per-damage tuning value.</summary>
        public float KnockbackForcePerDamage => _knockbackForcePerDamage;

        /// <summary>Current maximum impulse magnitude cap.</summary>
        public float MaxKnockbackForce => _maxKnockbackForce;
    }
}
