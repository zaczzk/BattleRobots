using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject tracking the current health of a single combatant
    /// (player robot or enemy). Lives outside the GameObject hierarchy so the UI
    /// and physics layers can both read it without creating a dependency between them.
    ///
    /// Lifecycle:
    ///   1. Call Reset() at match/spawn start.
    ///   2. Call ApplyDamage() whenever damage is dealt (from DamageReceiver).
    ///   3. Call Heal() for repair pickups / pre-match bonuses.
    ///
    /// Events fired via SO channels — no direct component references needed.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Combat ▶ HealthSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/HealthSO", order = 0)]
    public sealed class HealthSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Stats")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;

        [Header("Event Channels")]
        [Tooltip("Fired on every health change. Payload = current health (float).")]
        [SerializeField] private FloatGameEvent _onHealthChanged;

        [Tooltip("Fired once when health reaches zero.")]
        [SerializeField] private VoidGameEvent _onDeath;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Not serialised — set at runtime by InitForMatch(); resets to -1 on CreateInstance.
        private float _runtimeMaxHealth = -1f;

        /// <summary>
        /// Effective maximum health for the current match.
        /// Returns the runtime override (set by <see cref="InitForMatch"/>) when positive;
        /// falls back to the SO-asset default (<see cref="_maxHealth"/>) otherwise.
        /// </summary>
        public float MaxHealth => _runtimeMaxHealth > 0f ? _runtimeMaxHealth : _maxHealth;

        /// <summary>Current health. Always in [0, MaxHealth].</summary>
        public float CurrentHealth { get; private set; }

        /// <summary>True once CurrentHealth has reached zero. Reset by calling Reset().</summary>
        public bool IsDead => CurrentHealth <= 0f;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Overrides the effective max health for this match without modifying the SO asset.
        /// Must be called before <see cref="Reset()"/> so the new cap is applied immediately.
        ///
        /// Intended usage:
        ///   <c>health.InitForMatch(combatStats.TotalMaxHealth); health.Reset();</c>
        ///
        /// Values below 1 are clamped to 1. Not persisted between play sessions.
        /// </summary>
        public void InitForMatch(float maxHealth)
        {
            _runtimeMaxHealth = Mathf.Max(1f, maxHealth);
        }

        /// <summary>
        /// Restores health to <see cref="MaxHealth"/> and fires _onHealthChanged.
        /// Call once per match/spawn before gameplay begins.
        /// </summary>
        public void Reset()
        {
            CurrentHealth = MaxHealth;
            _onHealthChanged?.Raise(CurrentHealth);
        }

        /// <summary>
        /// Reduces health by <paramref name="amount"/>.
        /// Clamps to 0. Fires _onHealthChanged; fires _onDeath exactly once on kill.
        /// No-ops if already dead or amount ≤ 0.
        /// </summary>
        public void ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            _onHealthChanged?.Raise(CurrentHealth);

            if (IsDead)
                _onDeath?.Raise();
        }

        /// <summary>
        /// Increases health by <paramref name="amount"/>, capped at MaxHealth.
        /// No-ops if dead or amount ≤ 0.
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            _onHealthChanged?.Raise(CurrentHealth);
        }
    }
}
