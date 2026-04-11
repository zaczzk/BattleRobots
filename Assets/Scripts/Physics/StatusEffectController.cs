using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Manages a stack of runtime status effects (Burn / Stun / Slow) applied to a robot.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Maintains a fixed-size array of up to three simultaneously active effects
    ///     (one slot per <see cref="StatusEffectType"/>).
    ///   • Each FixedUpdate tick:
    ///       – Decrements remaining durations.
    ///       – Applies Burn damage via the target's <see cref="DamageReceiver"/>.
    ///       – Removes expired effects (in-place compact, no heap alloc).
    ///       – Recomputes <see cref="IsStunned"/> and <see cref="CurrentSlowFactor"/>.
    ///       – Propagates stun and slow signals to <see cref="RobotLocomotionController"/>.
    ///
    /// ── Stacking rule ─────────────────────────────────────────────────────────
    ///   Calling <see cref="ApplyEffect"/> with an effect whose
    ///   <see cref="StatusEffectSO.Type"/> already matches an active slot will
    ///   replace the duration only if the new duration is longer than the time
    ///   already remaining (take-maximum rule). This prevents spamming short effects
    ///   to endlessly refresh the timer.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to the robot's root GameObject alongside DamageReceiver.
    ///   2. Assign _target  → the robot's DamageReceiver (receives periodic Burn damage).
    ///   3. Assign _locomotion → the robot's RobotLocomotionController (Stun / Slow signals).
    ///   4. On DamageReceiver, assign _statusEffectController → this component so that
    ///      TakeDamage(DamageInfo) can automatically route any attached StatusEffectSO.
    ///   5. Call Clear() at match start / on robot death to wipe all active effects.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • ArticulationBody-only project — no Rigidbody.
    ///   • Zero heap allocations on the FixedUpdate hot path — fixed-size struct array,
    ///     no LINQ, no List, no Dictionary.
    ///   • BattleRobots.UI must NOT reference this class.
    ///   • Cross-component signalling via optional SO event channel (_onEffectsChanged).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusEffectController : MonoBehaviour
    {
        // One slot per StatusEffectType value — Burn(0), Stun(1), Slow(2).
        private const int k_MaxEffects = 3;

        /// <summary>
        /// Private struct stored inside the fixed-capacity array.
        /// Kept small (two fields) to minimize cache misses during the FixedUpdate tick.
        /// </summary>
        private struct EffectSlot
        {
            public StatusEffectSO Effect;
            public float          TimeRemaining;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Targets (optional)")]
        [Tooltip("DamageReceiver on this robot. Receives periodic Burn damage each FixedUpdate tick. " +
                 "Leave null to skip damage application (Stun and Slow still work).")]
        [SerializeField] private DamageReceiver _target;

        [Tooltip("Locomotion controller on this robot. Receives SetStunned / SetSlowFactor calls " +
                 "each FixedUpdate tick to enforce Stun and Slow effects. " +
                 "Leave null to skip locomotion override (Burn still works).")]
        [SerializeField] private RobotLocomotionController _locomotion;

        [Header("Events (optional)")]
        [Tooltip("Raised whenever any effect starts, refreshes, or expires.")]
        [SerializeField] private VoidGameEvent _onEffectsChanged;

        // ── Private state ─────────────────────────────────────────────────────

        // Fixed-capacity struct array — no GC pressure on the hot path.
        private EffectSlot[] _slots;
        private int          _activeCount;

        // Cached output properties, recomputed by RecalculateDerived() whenever
        // the slot array changes (in ApplyEffect, Clear, and after FixedUpdate expiry compact).
        private bool  _isStunned;
        private float _currentSlowFactor;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while a <see cref="StatusEffectType.Stun"/> effect is active.</summary>
        public bool IsStunned => _isStunned;

        /// <summary>
        /// Current speed multiplier from any active <see cref="StatusEffectType.Slow"/> effect.
        /// Returns 1.0 when no Slow effect is active (no slowdown).
        /// </summary>
        public float CurrentSlowFactor => _currentSlowFactor;

        /// <summary>Number of currently active status effects (0–3).</summary>
        public int ActiveEffectCount => _activeCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _slots             = new EffectSlot[k_MaxEffects];
            _activeCount       = 0;
            _isStunned         = false;
            _currentSlowFactor = 1f;
        }

        private void FixedUpdate()
        {
            if (_activeCount == 0) return;

            float dt         = Time.fixedDeltaTime;
            bool  anyExpired = false;

            // ── Tick all active slots ──────────────────────────────────────────
            for (int i = 0; i < _activeCount; i++)
            {
                _slots[i].TimeRemaining -= dt;

                // Apply Burn damage every tick (DamagePerSecond × dt = damage this frame).
                // Allocation-free: TakeDamage(float) accepts a value-type parameter.
                if (_slots[i].Effect != null
                    && _slots[i].Effect.Type == StatusEffectType.Burn
                    && _target != null)
                {
                    _target.TakeDamage(_slots[i].Effect.DamagePerSecond * dt);
                }

                if (_slots[i].TimeRemaining <= 0f)
                    anyExpired = true;
            }

            // ── Compact: shift active slots to the front ───────────────────────
            if (anyExpired)
            {
                int write = 0;
                for (int read = 0; read < _activeCount; read++)
                {
                    if (_slots[read].TimeRemaining > 0f)
                        _slots[write++] = _slots[read];
                }
                // Clear vacated tail slots to release SO references.
                for (int i = write; i < _activeCount; i++)
                    _slots[i] = default;

                _activeCount = write;
                RecalculateDerived();
                _onEffectsChanged?.Raise();
            }
            else
            {
                // Even without expiry, recalculate in case a tick changed derived state.
                // (Stun and Slow are time-invariant between ticks, but this keeps the path
                // consistent and is cheaper than a branch-heavy special case.)
                RecalculateDerived();
            }

            // ── Propagate to locomotion controller ────────────────────────────
            _locomotion?.SetStunned(_isStunned);
            _locomotion?.SetSlowFactor(_currentSlowFactor);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Apply a status effect to this robot.
        ///
        /// Stacking rule: if an effect of the same <see cref="StatusEffectSO.Type"/> is already
        /// active, its duration is replaced only when the new
        /// <see cref="StatusEffectSO.DurationSeconds"/> exceeds the time still remaining
        /// (take-maximum). No new slot is added in that case.
        ///
        /// When the maximum capacity (<c>k_MaxEffects = 3</c>) is already reached and no matching
        /// type is found, the call is silently dropped (design intent: one effect per type).
        ///
        /// Immediately updates <see cref="IsStunned"/> and <see cref="CurrentSlowFactor"/>
        /// so callers can read the new state without waiting for the next FixedUpdate tick.
        ///
        /// Zero allocation on this path — no heap activity.
        /// </summary>
        /// <param name="effect">Effect to apply. Null is a no-op.</param>
        public void ApplyEffect(StatusEffectSO effect)
        {
            if (effect == null) return;

            // Search for an existing slot of the same type.
            for (int i = 0; i < _activeCount; i++)
            {
                if (_slots[i].Effect == null) continue;
                if (_slots[i].Effect.Type != effect.Type) continue;

                // Take-maximum: replace only if incoming duration is longer.
                if (effect.DurationSeconds > _slots[i].TimeRemaining)
                {
                    _slots[i].Effect        = effect;
                    _slots[i].TimeRemaining = effect.DurationSeconds;
                }

                RecalculateDerived();
                _onEffectsChanged?.Raise();
                return;
            }

            // No existing slot found — add to the next free slot if capacity allows.
            if (_activeCount < k_MaxEffects)
            {
                _slots[_activeCount].Effect        = effect;
                _slots[_activeCount].TimeRemaining = effect.DurationSeconds;
                _activeCount++;
                RecalculateDerived();
                _onEffectsChanged?.Raise();
            }
        }

        /// <summary>
        /// Remove all active status effects immediately.
        /// Resets <see cref="IsStunned"/> to false and <see cref="CurrentSlowFactor"/> to 1.
        /// Propagates the reset to the locomotion controller (if assigned).
        /// Raises <see cref="_onEffectsChanged"/> once.
        /// Safe to call when no effects are active (no-op except for the event raise).
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _activeCount; i++)
                _slots[i] = default;

            _activeCount       = 0;
            _isStunned         = false;
            _currentSlowFactor = 1f;

            _locomotion?.SetStunned(false);
            _locomotion?.SetSlowFactor(1f);
            _onEffectsChanged?.Raise();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Recomputes <see cref="_isStunned"/> and <see cref="_currentSlowFactor"/> from the
        /// current contents of <c>_slots</c>. Called whenever the slot array changes.
        /// Zero allocation — pure value-type loop.
        /// </summary>
        private void RecalculateDerived()
        {
            _isStunned         = false;
            _currentSlowFactor = 1f;

            for (int i = 0; i < _activeCount; i++)
            {
                if (_slots[i].Effect == null) continue;

                switch (_slots[i].Effect.Type)
                {
                    case StatusEffectType.Stun:
                        _isStunned = true;
                        break;

                    case StatusEffectType.Slow:
                        // Use the strongest (lowest) slow factor.
                        // Only one Slow effect can be active at a time (type-uniqueness rule),
                        // but Mathf.Min is defensive and costs nothing.
                        _currentSlowFactor = Mathf.Min(_currentSlowFactor,
                                                        _slots[i].Effect.SlowFactor);
                        break;
                }
            }
        }
    }
}
