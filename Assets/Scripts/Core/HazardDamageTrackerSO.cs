using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that accumulates per-type environment damage
    /// dealt by <see cref="BattleRobots.Physics.HazardZoneController"/> ticks.
    ///
    /// ── Data model ────────────────────────────────────────────────────────────
    ///   Four float accumulators (one per <see cref="HazardZoneType"/>) track total
    ///   damage received; four int counters track hit events.
    ///   <see cref="GetMostFrequentType"/> returns the type with the highest hit count,
    ///   or <c>null</c> when no damage has been recorded this match.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────
    ///   • <see cref="OnEnable"/> (or <see cref="Reset"/>) zeroes all accumulators.
    ///   • <see cref="AddDamage"/> is called by <see cref="BattleRobots.Physics.HazardZoneController"/>
    ///     on each damage tick.
    ///   • <see cref="Reset"/> is called at match start via a VoidGameEventListener.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - AddDamage is zero-allocation (float arithmetic + optional event Raise).
    ///   - SO state resets on OnEnable so Play-mode restarts begin clean.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ HazardDamageTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/HazardDamageTracker")]
    public sealed class HazardDamageTrackerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel (optional)")]
        [Tooltip("Raised each time AddDamage is called with a positive amount. " +
                 "Subscribe HazardDamageHUDController to this event for live updates.")]
        [SerializeField] private VoidGameEvent _onDamageAdded;

        // ── Runtime accumulators (not serialised) ─────────────────────────────

        private float _lavaDamage;
        private float _electricDamage;
        private float _spikesDamage;
        private float _acidDamage;

        private int _lavaHits;
        private int _electricHits;
        private int _spikesHits;
        private int _acidHits;

        // ── Static helpers ────────────────────────────────────────────────────

        private static readonly HazardZoneType[] s_allTypes =
        {
            HazardZoneType.Lava,
            HazardZoneType.Electric,
            HazardZoneType.Spikes,
            HazardZoneType.Acid
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a damage tick from a hazard of the given type.
        /// No-op when <paramref name="amount"/> is ≤ 0.
        /// Raises <c>_onDamageAdded</c> on each valid call.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void AddDamage(HazardZoneType type, float amount)
        {
            if (amount <= 0f) return;

            switch (type)
            {
                case HazardZoneType.Lava:
                    _lavaDamage += amount;
                    _lavaHits++;
                    break;
                case HazardZoneType.Electric:
                    _electricDamage += amount;
                    _electricHits++;
                    break;
                case HazardZoneType.Spikes:
                    _spikesDamage += amount;
                    _spikesHits++;
                    break;
                case HazardZoneType.Acid:
                    _acidDamage += amount;
                    _acidHits++;
                    break;
            }

            _onDamageAdded?.Raise();
        }

        /// <summary>
        /// Total damage accumulated across all hazard types this match.
        /// </summary>
        public float GetTotalDamage() =>
            _lavaDamage + _electricDamage + _spikesDamage + _acidDamage;

        /// <summary>
        /// Total damage accumulated for the specified <paramref name="type"/>.
        /// Returns 0 for unknown types.
        /// </summary>
        public float GetDamageForType(HazardZoneType type)
        {
            switch (type)
            {
                case HazardZoneType.Lava:     return _lavaDamage;
                case HazardZoneType.Electric: return _electricDamage;
                case HazardZoneType.Spikes:   return _spikesDamage;
                case HazardZoneType.Acid:     return _acidDamage;
                default:                      return 0f;
            }
        }

        /// <summary>
        /// Number of damage-tick events recorded for the specified <paramref name="type"/>.
        /// Returns 0 for unknown types.
        /// </summary>
        public int GetHitCountForType(HazardZoneType type)
        {
            switch (type)
            {
                case HazardZoneType.Lava:     return _lavaHits;
                case HazardZoneType.Electric: return _electricHits;
                case HazardZoneType.Spikes:   return _spikesHits;
                case HazardZoneType.Acid:     return _acidHits;
                default:                      return 0;
            }
        }

        /// <summary>
        /// Returns the <see cref="HazardZoneType"/> with the highest hit count this match,
        /// or <c>null</c> when no damage has been recorded (all counts are zero).
        /// Ties are broken by the iteration order of <see cref="HazardZoneType"/> values.
        /// </summary>
        public HazardZoneType? GetMostFrequentType()
        {
            int              maxHits = 0;
            HazardZoneType?  result  = null;

            foreach (HazardZoneType t in s_allTypes)
            {
                int hits = GetHitCountForType(t);
                if (hits > maxHits)
                {
                    maxHits = hits;
                    result  = t;
                }
            }

            return result;
        }

        /// <summary>
        /// Resets all damage and hit-count accumulators to zero.
        /// Does NOT fire any event.
        /// Call at match start via a VoidGameEventListener.
        /// </summary>
        public void Reset()
        {
            _lavaDamage     = 0f;
            _electricDamage = 0f;
            _spikesDamage   = 0f;
            _acidDamage     = 0f;

            _lavaHits     = 0;
            _electricHits = 0;
            _spikesHits   = 0;
            _acidHits     = 0;
        }
    }
}
