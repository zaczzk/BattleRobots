using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime-only SO representing a robot's current hit-points.
    /// Call <see cref="Initialize"/> at match/round start to reset to max.
    ///
    /// All mutations fire SO event channels so UI and stats trackers stay decoupled:
    ///   _onHealthChanged  — FloatGameEvent   (payload = CurrentHp)
    ///   _onDamageReceived — DamageEvent      (payload = DamagePayload with amount + sourceId)
    ///   _onDeath          — VoidGameEvent    (fired once on the killing hit)
    ///
    /// HealthSO is not persisted directly; MatchRecord captures damageDone/damageTaken
    /// totals at match end.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/HealthSO", order = 0)]
    public sealed class HealthSO : ScriptableObject
    {
        // ── Config ────────────────────────────────────────────────────────────
        [Header("Stats")]
        [SerializeField, Min(1f)] private float _maxHp = 100f;

        [Header("Event Channels")]
        [Tooltip("Raised whenever HP changes. Payload = current HP.")]
        [SerializeField] private FloatGameEvent _onHealthChanged;

        [Tooltip("Raised on every successful hit. Payload = amount + sourceId.")]
        [SerializeField] private DamageEvent _onDamageReceived;

        [Tooltip("Raised once when HP reaches zero (the killing hit).")]
        [SerializeField] private VoidGameEvent _onDeath;

        // ── Runtime State ─────────────────────────────────────────────────────

        /// <summary>Configured asset max HP (serialised, never modified at runtime).</summary>
        public float MaxHp         => _maxHp;

        /// <summary>
        /// Effective max HP for the current match session.
        /// Equals <see cref="MaxHp"/> after a plain <see cref="Initialize"/>,
        /// or <c>MaxHp + bonusHp</c> after <see cref="InitializeWithBonus"/>.
        /// Resets on each Initialize call.
        /// </summary>
        public float EffectiveMaxHp { get; private set; }

        public float CurrentHp     { get; private set; }
        public bool  IsAlive       => CurrentHp > 0f;

        // ── API ───────────────────────────────────────────────────────────────

        /// <summary>Resets HP to max and broadcasts the initial value. Call at match start.</summary>
        public void Initialize()
        {
            EffectiveMaxHp = _maxHp;
            CurrentHp      = _maxHp;
            _onHealthChanged?.Raise(CurrentHp);
        }

        /// <summary>
        /// Resets HP to <c>_maxHp + bonusHp</c> for this match only.
        /// The bonus is transient — it does not modify the serialised asset value.
        /// Used by <c>RobotSpawner</c> to apply PartDefinition HP bonuses at spawn.
        /// </summary>
        /// <param name="bonusHp">Extra HP from equipped parts. Must be ≥ 0.</param>
        public void InitializeWithBonus(float bonusHp)
        {
            EffectiveMaxHp = _maxHp + Mathf.Max(0f, bonusHp);
            CurrentHp      = EffectiveMaxHp;
            _onHealthChanged?.Raise(CurrentHp);
        }

        /// <summary>
        /// Applies damage from an identified source.
        /// Ignored if the robot is already dead or <paramref name="amount"/> &lt;= 0.
        /// Fires <c>_onDamageReceived</c> then <c>_onHealthChanged</c>; fires <c>_onDeath</c>
        /// on the killing hit.
        /// </summary>
        public void TakeDamage(float amount, string sourceId = "")
        {
            if (!IsAlive || amount <= 0f) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);

            _onDamageReceived?.Raise(new DamagePayload { amount = amount, sourceId = sourceId });
            _onHealthChanged?.Raise(CurrentHp);

            if (CurrentHp <= 0f)
                _onDeath?.Raise();
        }

        /// <summary>
        /// Restores HP, clamped to MaxHp.
        /// Ignored if the robot is dead or <paramref name="amount"/> &lt;= 0.
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            CurrentHp = Mathf.Min(EffectiveMaxHp, CurrentHp + amount);
            _onHealthChanged?.Raise(CurrentHp);
        }
    }
}
