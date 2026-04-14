using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime blackboard ScriptableObject that accumulates per-match combat statistics.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Records total damage dealt by the player and total damage taken.
    ///   • Tracks hit count (successful attacks) and hits received.
    ///   • Accumulates per-type damage dealt (Physical / Energy / Thermal / Shock)
    ///     when using the <see cref="RecordDamageDealt(DamageInfo)"/> overload.
    ///   • Provides a <see cref="DamageEfficiency"/> ratio in [0, 1] for a quick
    ///     performance summary on the post-match screen.
    ///   • Fires optional <see cref="_onStatisticsUpdated"/> after every record call.
    ///   • Resets cleanly at the start of every match via <see cref="Reset"/>.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   This SO is a pure data container — it does not subscribe to any events.
    ///   Drive it from the Arena scene via DamageGameEventListener components:
    ///
    ///   1. Add a DamageGameEventListener GameObject to the Arena scene.
    ///      Event = the DamageGameEvent channel the player's attacks raise
    ///      (i.e. the channel the <em>enemy</em> DamageReceiver listens to).
    ///      Response → MatchStatisticsSO.RecordDamageDealt(DamageInfo).
    ///
    ///   2. Add a second DamageGameEventListener.
    ///      Event = the channel enemy attacks raise
    ///      (the channel the <em>player</em> DamageReceiver listens to).
    ///      Response → MatchStatisticsSO.RecordDamageTaken(DamageInfo).
    ///
    ///   3. Assign this same SO to MatchManager._matchStatistics so that
    ///      EndMatch() uses the accurate accumulated values instead of the
    ///      end-of-match health-difference approximation.
    ///
    ///   4. Wire _onStatisticsUpdated to any live HUD controller
    ///      (e.g. MatchStatisticsHUDController) that wants reactive refreshes.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - Zero alloc hot path: RecordDamage* methods are pure float accumulation.
    ///   - Per-type accumulators are runtime-only (not serialized).
    ///   - RecordDamageDealt(float) does NOT route to type buckets — only the
    ///     DamageInfo overload has type information.
    ///   - Reset() clears all accumulators including type totals.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchStatisticsSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/MatchStatisticsSO", order = 6)]
    public sealed class MatchStatisticsSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel (optional)")]
        [Tooltip("Raised after every RecordDamageDealt or RecordDamageTaken call. " +
                 "Wire to MatchStatisticsHUDController for live HUD updates.")]
        [SerializeField] private VoidGameEvent _onStatisticsUpdated;

        // ── Read-only statistics ───────────────────────────────────────────────

        /// <summary>Total damage the player dealt to the enemy this match.</summary>
        public float TotalDamageDealt { get; private set; }

        /// <summary>Total damage the player received from the enemy this match.</summary>
        public float TotalDamageTaken { get; private set; }

        /// <summary>Number of times the player successfully hit the enemy.</summary>
        public int HitCount { get; private set; }

        /// <summary>Number of times the player was hit by the enemy.</summary>
        public int HitsReceived { get; private set; }

        // Per-type damage-dealt accumulators — runtime only, not serialized.
        private float _physicalDealt;
        private float _energyDealt;
        private float _thermalDealt;
        private float _shockDealt;

        /// <summary>
        /// Ratio of damage dealt to total damage exchanged.
        /// Returns a value in [0, 1]:  1 = only dealt, 0 = only taken / none dealt.
        /// Returns 0 when both totals are zero (no hits recorded).
        /// </summary>
        public float DamageEfficiency
        {
            get
            {
                float total = TotalDamageDealt + TotalDamageTaken;
                return total > 0f ? TotalDamageDealt / total : 0f;
            }
        }

        /// <summary>
        /// Returns the total damage the player dealt this match that was of the given type.
        /// Only populated when using <see cref="RecordDamageDealt(DamageInfo)"/>.
        /// Returns 0 for unknown types.
        /// </summary>
        public float GetDealtByType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalDealt;
                case DamageType.Energy:   return _energyDealt;
                case DamageType.Thermal:  return _thermalDealt;
                case DamageType.Shock:    return _shockDealt;
                default:                  return 0f;
            }
        }

        /// <summary>
        /// Fraction of <see cref="TotalDamageDealt"/> that came from the given type, in [0, 1].
        /// Returns 0 when no damage has been dealt yet (safe division — never NaN).
        /// </summary>
        public float DamageTypeRatio(DamageType type)
        {
            return TotalDamageDealt > 0f ? GetDealtByType(type) / TotalDamageDealt : 0f;
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records one successful player attack by raw amount only.
        /// Does NOT route to a per-type accumulator — use the DamageInfo overload
        /// when type information is available.
        /// Ignores zero or negative values.
        /// </summary>
        public void RecordDamageDealt(float amount)
        {
            if (amount <= 0f) return;
            TotalDamageDealt += amount;
            HitCount++;
            _onStatisticsUpdated?.Raise();
        }

        /// <summary>
        /// Records one successful enemy attack against the player.
        /// Ignores zero or negative values.
        /// </summary>
        public void RecordDamageTaken(float amount)
        {
            if (amount <= 0f) return;
            TotalDamageTaken += amount;
            HitsReceived++;
            _onStatisticsUpdated?.Raise();
        }

        /// <summary>
        /// Records a player attack using the full <see cref="DamageInfo"/> payload.
        /// Accumulates the amount into <see cref="TotalDamageDealt"/> AND the correct
        /// per-type bucket determined by <see cref="DamageInfo.damageType"/>.
        /// Enables direct wiring from a <c>DamageGameEventListener</c> UnityEvent.
        /// </summary>
        public void RecordDamageDealt(DamageInfo info)
        {
            if (info.amount <= 0f) return;
            TotalDamageDealt += info.amount;
            HitCount++;

            switch (info.damageType)
            {
                case DamageType.Physical: _physicalDealt += info.amount; break;
                case DamageType.Energy:   _energyDealt   += info.amount; break;
                case DamageType.Thermal:  _thermalDealt  += info.amount; break;
                case DamageType.Shock:    _shockDealt    += info.amount; break;
            }

            _onStatisticsUpdated?.Raise();
        }

        /// <summary>
        /// Records an enemy attack using the full <see cref="DamageInfo"/> payload.
        /// Extracts <see cref="DamageInfo.amount"/> and delegates to
        /// <see cref="RecordDamageTaken(float)"/>.
        /// Enables direct wiring from a <c>DamageGameEventListener</c> UnityEvent.
        /// </summary>
        public void RecordDamageTaken(DamageInfo info) => RecordDamageTaken(info.amount);

        /// <summary>
        /// Clears all accumulated statistics including per-type totals.
        /// Call at the start of every match before gameplay begins.
        /// </summary>
        public void Reset()
        {
            TotalDamageDealt = 0f;
            TotalDamageTaken = 0f;
            HitCount         = 0;
            HitsReceived     = 0;
            _physicalDealt   = 0f;
            _energyDealt     = 0f;
            _thermalDealt    = 0f;
            _shockDealt      = 0f;
        }
    }
}
