using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks a robot's energy pool during a match.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────
    ///   OnEnable fills _currentEnergy to _maxEnergy (no events) — this means
    ///   every time the SO is loaded into memory (Play mode start, domain reload)
    ///   the pool starts full, which is the expected per-match reset behaviour.
    ///
    ///   EnergyRecharger MB (BattleRobots.Physics) should call Recharge(Time.fixedDeltaTime)
    ///   each FixedUpdate to implement passive regeneration.
    ///
    /// ── Persistence ──────────────────────────────────────────────────────────
    ///   LoadSnapshot(float) restores energy from a persisted value without firing
    ///   any events — safe for use from GameBootstrapper or cross-wave checkpoints.
    ///   TakeSnapshot() returns the current value for persistence.
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────
    ///   • _currentEnergy mutated only through Consume / Recharge / Reset / LoadSnapshot.
    ///   • LoadSnapshot must NOT raise any events (bootstrapper context).
    ///   • All event channels are optional — null-guarded on every Raise call.
    ///   • EnergyRatio guards against divide-by-zero (_maxEnergy == 0 → ratio = 0).
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/EnergySystem")]
    public sealed class EnergySystemSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Configuration")]
        [Tooltip("Maximum energy capacity. Must be ≥ 1.")]
        [SerializeField, Min(1f)] private float _maxEnergy = 100f;

        [Tooltip("Passive recharge rate in energy units per second.")]
        [SerializeField, Min(0f)] private float _rechargeRate = 10f;

        [Header("Events")]
        [Tooltip("Raised after any change to CurrentEnergy (Consume, Recharge, Reset).")]
        [SerializeField] private VoidGameEvent _onEnergyChanged;

        [Tooltip("Raised when CurrentEnergy reaches 0 (after Consume empties the pool).")]
        [SerializeField] private VoidGameEvent _onEnergyDepleted;

        [Tooltip("Raised when CurrentEnergy reaches MaxEnergy (after Recharge fills the pool).")]
        [SerializeField] private VoidGameEvent _onEnergyFull;

        // ── Runtime state (not serialized) ────────────────────────────────────

        private float _currentEnergy;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current energy level. Always in [0, MaxEnergy].</summary>
        public float CurrentEnergy => _currentEnergy;

        /// <summary>Maximum energy capacity (inspector-configured).</summary>
        public float MaxEnergy => _maxEnergy;

        /// <summary>Passive recharge rate in units per second (inspector-configured).</summary>
        public float RechargeRate => _rechargeRate;

        /// <summary>
        /// Current energy as a fraction of MaxEnergy in [0, 1].
        /// Returns 0 when MaxEnergy ≤ 0 to guard against divide-by-zero.
        /// </summary>
        public float EnergyRatio => _maxEnergy > 0f ? _currentEnergy / _maxEnergy : 0f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Fill to max on load so every match/session begins with a full pool.
            // Intentionally does NOT raise _onEnergyChanged here — subscribers may
            // not be listening yet during domain reload.
            _currentEnergy = _maxEnergy;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to consume <paramref name="amount"/> units of energy.
        /// </summary>
        /// <param name="amount">Energy to consume. Must be ≥ 0; negative → returns false.</param>
        /// <returns>
        /// <c>true</c> if the amount was successfully consumed;
        /// <c>false</c> if <paramref name="amount"/> is negative or there is insufficient energy.
        /// </returns>
        public bool Consume(float amount)
        {
            if (amount < 0f) return false;
            if (_currentEnergy < amount) return false;

            _currentEnergy = Mathf.Max(0f, _currentEnergy - amount);
            _onEnergyChanged?.Raise();

            if (_currentEnergy <= 0f)
                _onEnergyDepleted?.Raise();

            return true;
        }

        /// <summary>
        /// Passively recharges energy based on elapsed time.
        /// Call this with <c>Time.fixedDeltaTime</c> from a FixedUpdate loop.
        /// No-op when <paramref name="deltaTime"/> ≤ 0 or the pool is already full.
        /// </summary>
        /// <param name="deltaTime">Elapsed seconds (normally Time.fixedDeltaTime).</param>
        public void Recharge(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (_currentEnergy >= _maxEnergy) return;

            _currentEnergy = Mathf.Min(_maxEnergy, _currentEnergy + _rechargeRate * deltaTime);
            _onEnergyChanged?.Raise();

            if (_currentEnergy >= _maxEnergy)
                _onEnergyFull?.Raise();
        }

        /// <summary>
        /// Restores energy from a persisted snapshot.
        /// Does NOT raise any events — safe for use from GameBootstrapper.
        /// Value is clamped to [0, MaxEnergy].
        /// </summary>
        public void LoadSnapshot(float currentEnergy)
        {
            _currentEnergy = Mathf.Clamp(currentEnergy, 0f, _maxEnergy);
        }

        /// <summary>Returns the current energy level for persistence.</summary>
        public float TakeSnapshot() => _currentEnergy;

        /// <summary>
        /// Overrides the passive recharge rate at runtime (e.g., from PassiveEffectApplier).
        /// Values below 0 are clamped to 0. Does NOT raise any events — passive configuration only.
        /// </summary>
        public void SetRechargeRate(float rate)
        {
            _rechargeRate = Mathf.Max(0f, rate);
        }

        /// <summary>
        /// Resets energy to MaxEnergy and raises <c>_onEnergyChanged</c>.
        /// Use at match start if the pool was partially consumed in a previous session.
        /// </summary>
        public void Reset()
        {
            _currentEnergy = _maxEnergy;
            _onEnergyChanged?.Raise();
        }
    }
}
