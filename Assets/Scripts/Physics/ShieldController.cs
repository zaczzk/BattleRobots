using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that manages a robot's energy shield.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Initialises <see cref="ShieldSO"/> at match-start via <see cref="ResetShield"/>.
    ///   • Intercepts incoming damage via <see cref="AbsorbDamage"/>; called by
    ///     <see cref="DamageReceiver"/> BEFORE armor reduction and HealthSO are touched.
    ///     Returns the leftover damage that bypassed the shield.
    ///   • Drives per-frame shield recharge in <see cref="Update"/> once the
    ///     recharge delay (from <see cref="ShieldConfig"/>) has elapsed since the last hit.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to the robot's root GameObject alongside DamageReceiver.
    ///   2. Assign _shield → a ShieldSO asset (one per robot; not shared).
    ///   3. Assign _config → a ShieldConfig asset (can be shared across robots).
    ///   4. Assign DamageReceiver._shield → this component.
    ///   5. Call ResetShield() at match start (e.g., from MatchFlowController).
    ///
    /// ARCHITECTURE RULES:
    ///   • ArticulationBody-only project — no Rigidbody.
    ///   • BattleRobots.UI must NOT reference this class.
    ///   • Update allocates nothing — only float arithmetic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShieldController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Shield Data")]
        [Tooltip("Runtime SO that stores current shield HP and fires change events.")]
        [SerializeField] private ShieldSO _shield;

        [Tooltip("Immutable config: max HP, recharge rate, recharge delay.")]
        [SerializeField] private ShieldConfig _config;

        // ── Private state ─────────────────────────────────────────────────────

        // Counts down to zero; recharge only resumes when this reaches 0.
        private float _rechargeTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            ResetShield();
        }

        private void Update()
        {
            if (_shield == null || _config == null)           return;
            if (_shield.CurrentHP >= _shield.MaxHP)           return;   // already full

            if (_rechargeTimer > 0f)
            {
                _rechargeTimer -= Time.deltaTime;
                return;
            }

            _shield.Recharge(_config.RechargeRate * Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Absorbs as much of <paramref name="amount"/> as the current shield can hold.
        /// Resets the recharge delay timer whenever the shield absorbs any damage.
        /// Returns the leftover damage that bypassed the shield (≥ 0).
        /// Returns <paramref name="amount"/> unchanged when no ShieldSO is assigned.
        /// Allocation-free — pure float arithmetic + delegate invocation.
        /// </summary>
        public float AbsorbDamage(float amount)
        {
            if (_shield == null) return amount;

            float leftover = _shield.AbsorbDamage(amount);

            // Only restart the recharge delay when the shield actually absorbed something.
            if (leftover < amount && _config != null)
                _rechargeTimer = _config.RechargeDelay;

            return leftover;
        }

        /// <summary>
        /// Resets the shield to full capacity and clears the recharge delay timer.
        /// Call at match start (e.g., from MatchFlowController.HandleMatchStarted).
        /// Safe to call when either <see cref="_shield"/> or <see cref="_config"/> is null.
        /// </summary>
        public void ResetShield()
        {
            if (_shield == null || _config == null) return;
            _shield.Reset(_config.MaxShieldHP);
            _rechargeTimer = 0f;
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>Current shield HP; 0 when no ShieldSO is assigned.</summary>
        public float CurrentShieldHP => _shield != null ? _shield.CurrentHP : 0f;

        /// <summary>True when the shield still has HP remaining.</summary>
        public bool IsShieldActive => _shield != null && _shield.IsActive;
    }
}
