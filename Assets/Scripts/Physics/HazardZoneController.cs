using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that turns a trigger collider into a damage-dealing arena hazard.
    ///
    /// ── How it works ──────────────────────────────────────────────────────────
    ///   Any robot with a <see cref="DamageReceiver"/> component that overlaps this
    ///   GameObject's trigger collider accumulates time each physics frame. Once the
    ///   accumulated time meets or exceeds <see cref="HazardZoneSO.TickInterval"/>,
    ///   <see cref="DamageReceiver.TakeDamage(DamageInfo)"/> is called and the
    ///   optional <see cref="_onHazardTriggered"/> event is raised (for audio/VFX).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • ArticulationBody only — this MB never touches Rigidbody.
    ///   • <see cref="ProcessOverlap"/> is public so EditMode tests can drive
    ///     tick logic directly without requiring physics simulation.
    ///   • Per-target time accumulators are keyed by Collider.GetInstanceID() —
    ///     integer keys avoid string allocations and remain valid while the
    ///     collider exists.
    ///   • No heap allocation per frame: Dictionary lookup/set on value types only.
    ///   • BattleRobots.UI must not reference this class.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to a GameObject with a Collider set to "Is Trigger".
    ///   2. Assign _config → a HazardZoneSO asset.
    ///   3. Optionally assign _onHazardTriggered → a VoidGameEvent SO (audio/VFX hook).
    ///   4. Toggle _isActive via <see cref="IsActive"/> or a VoidGameEventListener
    ///      to enable / disable the hazard mid-match (e.g. switch on after countdown).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardZoneController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("HazardZoneSO that defines damage amount, tick interval, and hazard type.")]
        [SerializeField] private HazardZoneSO _config;

        [Header("State")]
        [Tooltip("When false the hazard deals no damage. Toggle at runtime via IsActive " +
                 "or a VoidGameEventListener response.")]
        [SerializeField] private bool _isActive = true;

        [Header("Events (optional)")]
        [Tooltip("Raised each time a damage tick fires. Wire an AudioManager or VFX handler " +
                 "to play a sizzle/spark effect without coupling to this component.")]
        [SerializeField] private VoidGameEvent _onHazardTriggered;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Accumulated seconds since the last damage tick, keyed by Collider InstanceID.
        // Populated by OnTriggerEnter; cleared by OnTriggerExit and OnDisable.
        private readonly Dictionary<int, float> _accumulators = new Dictionary<int, float>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// When false no damage ticks fire even while robots are inside the zone.
        /// Safe to toggle mid-match.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        /// <summary>
        /// Core tick logic — accumulates <paramref name="deltaTime"/> for the given target
        /// and fires a damage event once the configured interval is reached.
        ///
        /// Exposed as public so EditMode tests can call it directly without physics simulation.
        /// In play, called from <see cref="OnTriggerStay"/>.
        ///
        /// No-ops when:
        ///   • <see cref="IsActive"/> is false.
        ///   • <see cref="_config"/> is null.
        ///   • <paramref name="dr"/> is null or its <see cref="DamageReceiver.IsDead"/> is true.
        /// </summary>
        /// <param name="targetId">
        ///   Stable integer key for the target (typically <c>Collider.GetInstanceID()</c>).
        ///   Must be non-zero to allow meaningful per-target tracking.
        /// </param>
        /// <param name="dr">The DamageReceiver to hit when a tick fires.</param>
        /// <param name="deltaTime">Seconds elapsed since the last call for this target.</param>
        public void ProcessOverlap(int targetId, DamageReceiver dr, float deltaTime)
        {
            if (!_isActive || _config == null || dr == null || dr.IsDead) return;

            _accumulators.TryGetValue(targetId, out float acc);
            acc += deltaTime;

            if (acc >= _config.TickInterval)
            {
                // Subtract one interval to carry over any remainder (prevents drift).
                acc -= _config.TickInterval;
                var info = new DamageInfo(_config.DamagePerTick, _config.DamageSourceId);
                dr.TakeDamage(info);
                _onHazardTriggered?.Raise();
            }

            _accumulators[targetId] = acc;
        }

        /// <summary>
        /// Removes the accumulator entry for the given target, resetting its tick timer.
        /// Called by <see cref="OnTriggerExit"/> and exposed for tests.
        /// </summary>
        /// <param name="targetId">The same key passed to <see cref="ProcessOverlap"/>.</param>
        public void ClearTracking(int targetId)
        {
            _accumulators.Remove(targetId);
        }

        // ── Unity trigger callbacks ────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            // Pre-initialise the accumulator so the first OnTriggerStay call
            // has a defined starting value without a dictionary miss.
            int id = other.GetInstanceID();
            if (!_accumulators.ContainsKey(id))
                _accumulators[id] = 0f;
        }

        private void OnTriggerStay(Collider other)
        {
            // Look up DamageReceiver on the colliding object. GetComponent is a
            // native call with no heap allocation.
            var dr = other.GetComponent<DamageReceiver>();
            if (dr == null) return;

            ProcessOverlap(other.GetInstanceID(), dr, Time.deltaTime);
        }

        private void OnTriggerExit(Collider other)
        {
            ClearTracking(other.GetInstanceID());
        }

        private void OnDisable()
        {
            // Purge all accumulators so stale timers don't carry over if the
            // hazard is re-enabled later.
            _accumulators.Clear();
        }
    }
}
