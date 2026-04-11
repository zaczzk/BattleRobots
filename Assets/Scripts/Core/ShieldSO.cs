using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a robot's current shield state.
    ///
    /// The shield absorbs incoming damage before it reaches <see cref="HealthSO"/>.
    /// Any leftover damage that the shield cannot absorb is returned to the caller
    /// (typically <see cref="BattleRobots.Physics.ShieldController"/>) for forwarding
    /// to <see cref="BattleRobots.Physics.DamageReceiver"/>.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────
    ///   1. Call <see cref="Reset"/> at match start; pass MaxShieldHP from ShieldConfig.
    ///   2. <see cref="AbsorbDamage"/> is called by ShieldController on each hit;
    ///      returns the damage amount the shield could NOT absorb.
    ///   3. <see cref="Recharge"/> is called each frame by ShieldController once the
    ///      recharge delay has elapsed (HP/s × deltaTime).
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace. No Physics or UI references.
    ///   Zero alloc hot-path: AbsorbDamage and Recharge are pure float arithmetic.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ ShieldSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/ShieldSO", order = 16)]
    public sealed class ShieldSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels")]
        [Tooltip("Fired whenever shield HP changes. Payload = current shield HP.")]
        [SerializeField] private FloatGameEvent _onShieldChanged;

        [Tooltip("Fired once when shield HP reaches zero (depleted).")]
        [SerializeField] private VoidGameEvent _onShieldDepleted;

        [Tooltip("Fired when the shield begins recharging after being fully depleted.")]
        [SerializeField] private VoidGameEvent _onShieldRecharged;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>Maximum shield HP for the current match (set via <see cref="Reset"/>).</summary>
        public float MaxHP { get; private set; }

        /// <summary>Current shield HP. Always in [0, MaxHP].</summary>
        public float CurrentHP { get; private set; }

        /// <summary>True while the shield still has HP remaining.</summary>
        public bool IsActive => CurrentHP > 0f;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the shield to full capacity. Call at match start before gameplay.
        /// Negative values are clamped to zero.
        /// Fires <see cref="_onShieldChanged"/> with the new HP value.
        /// </summary>
        /// <param name="maxHP">Maximum shield hit-points for this match.</param>
        public void Reset(float maxHP)
        {
            MaxHP     = Mathf.Max(0f, maxHP);
            CurrentHP = MaxHP;
            _onShieldChanged?.Raise(CurrentHP);
        }

        /// <summary>
        /// Absorbs as much of <paramref name="amount"/> as the shield can hold.
        /// Returns the leftover damage the shield could NOT absorb (forward to armor / HealthSO).
        ///
        /// Fires <see cref="_onShieldChanged"/> whenever CurrentHP decreases.
        /// Fires <see cref="_onShieldDepleted"/> the moment CurrentHP first reaches zero.
        /// Zero or negative amounts are returned unchanged without mutating state.
        /// </summary>
        public float AbsorbDamage(float amount)
        {
            if (amount <= 0f || !IsActive) return amount;

            float absorbed = Mathf.Min(CurrentHP, amount);
            bool  wasActive = IsActive;

            CurrentHP -= absorbed;
            _onShieldChanged?.Raise(CurrentHP);

            if (wasActive && CurrentHP <= 0f)
                _onShieldDepleted?.Raise();

            return amount - absorbed;
        }

        /// <summary>
        /// Increases shield HP by <paramref name="amount"/>, clamped to <see cref="MaxHP"/>.
        /// Fires <see cref="_onShieldChanged"/> when HP increases.
        /// Fires <see cref="_onShieldRecharged"/> when the shield recovers from zero to positive
        /// (indicating the shield came back online after full depletion).
        /// Does nothing when already at MaxHP or when amount ≤ 0.
        /// </summary>
        public void Recharge(float amount)
        {
            if (amount <= 0f || CurrentHP >= MaxHP) return;

            bool wasEmpty = CurrentHP <= 0f;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            _onShieldChanged?.Raise(CurrentHP);

            if (wasEmpty && CurrentHP > 0f)
                _onShieldRecharged?.Raise();
        }
    }
}
