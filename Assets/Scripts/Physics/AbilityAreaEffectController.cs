using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that executes an area-of-effect blast when an ability activates.
    ///
    /// ── Flow ─────────────────────────────────────────────────────────────────────
    ///   1. OnEnable  subscribes <see cref="TriggerAreaEffect"/> to
    ///      <c>_onAbilityActivated</c> (the same VoidGameEvent fired by
    ///      <see cref="AbilityController"/> on a successful activation).
    ///   2. TriggerAreaEffect() calls <c>Physics.OverlapSphereNonAlloc</c> with a
    ///      pre-allocated <c>Collider[]</c> buffer (zero GC on the hot path).
    ///   3. For each unique root <see cref="DamageReceiver"/> found within the radius:
    ///        a. Skips dead targets.
    ///        b. Calls <c>TakeDamage(DamageInfo)</c> with config.Damage and sourceId.
    ///        c. If config.StatusEffect is non-null, calls
    ///           <c>TriggerStatusEffect(config.StatusEffect)</c>.
    ///   4. Raises <c>AbilityAreaEffectConfig._onEffectTriggered</c> once after all
    ///      targets are processed (use for VFX / audio).
    ///   3. OnDisable unsubscribes.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — may reference Core; must NOT reference UI.
    ///   • ArticulationBody-only project: no Rigidbody.
    ///   • Zero GC allocation on <see cref="TriggerAreaEffect"/> hot path
    ///     (fixed-size Collider[] buffer allocated once in Awake;
    ///     no LINQ, no List, no temporary array creation).
    ///   • All inspector fields optional — any null reference short-circuits silently.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the same root GameObject as <see cref="AbilityController"/>.
    ///   2. Assign _config → an <see cref="AbilityAreaEffectConfig"/> asset.
    ///   3. Assign _onAbilityActivated → the same VoidGameEvent wired to
    ///      AbilityController._onAbilityActivated.
    ///   4. Optionally assign _aoeCenter → a child Transform marking the blast origin.
    ///      When null, the component's own transform.position is used.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityAreaEffectController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────

        // Pre-allocated buffer size. 16 covers typical arena densities without
        // over-allocating. Targets beyond the 16th hit within the radius are skipped.
        private const int k_BufferSize = 16;

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("Defines the AoE radius, damage, optional status effect, and event channel. " +
                 "Leave null to disable the AoE effect entirely (no-op on activation).")]
        [SerializeField] private AbilityAreaEffectConfig _config;

        [Header("Event Channel — In")]
        [Tooltip("VoidGameEvent raised by AbilityController._onAbilityActivated. " +
                 "This controller subscribes to this event and fires TriggerAreaEffect() " +
                 "when it is raised. Leave null — nothing fires without this channel wired.")]
        [SerializeField] private VoidGameEvent _onAbilityActivated;

        [Header("AoE Origin (optional)")]
        [Tooltip("Transform used as the centre of the sphere overlap. " +
                 "When null, the component's own transform.position is used.")]
        [SerializeField] private Transform _aoeCenter;

        // ── Private state ─────────────────────────────────────────────────────

        // Fixed-size buffer for OverlapSphereNonAlloc — allocated once in Awake.
        private Collider[] _overlapBuffer;

        // Cached delegate reference — required for correct Register/Unregister pairing.
        private Action _triggerDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _overlapBuffer  = new Collider[k_BufferSize];
            _triggerDelegate = TriggerAreaEffect;
        }

        private void OnEnable()
        {
            _onAbilityActivated?.RegisterCallback(_triggerDelegate);
        }

        private void OnDisable()
        {
            _onAbilityActivated?.UnregisterCallback(_triggerDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Executes the AoE blast at the current position of <see cref="_aoeCenter"/>
        /// (or <c>transform.position</c> when null).
        ///
        /// • Performs a <c>Physics.OverlapSphereNonAlloc</c> query (zero allocation).
        /// • Applies <see cref="AbilityAreaEffectConfig.Damage"/> + optional status
        ///   effect to each living <see cref="DamageReceiver"/> found.
        /// • Raises <see cref="AbilityAreaEffectConfig.RaiseEffectTriggered"/> once.
        ///
        /// Safe to call with a null <see cref="_config"/> (immediate no-op).
        /// Also usable as a direct method call from other code (e.g. boss scripts).
        /// </summary>
        public void TriggerAreaEffect()
        {
            if (_config == null) return;

            Vector3 centre = _aoeCenter != null ? _aoeCenter.position : transform.position;
            float   radius = _config.Radius;
            float   damage = _config.Damage;
            string  source = _config.DamageSourceId;

            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(
                centre, radius, _overlapBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null) continue;

                DamageReceiver dr = col.GetComponentInParent<DamageReceiver>();
                if (dr == null || dr.IsDead) continue;

                // Deliver damage (and optional status effect embedded in DamageInfo).
                if (damage > 0f)
                {
                    dr.TakeDamage(new DamageInfo(
                        damage,
                        source,
                        col.transform.position,
                        _config.StatusEffect));
                }
                else if (_config.StatusEffect != null)
                {
                    // Zero-damage AoE — apply status effect directly without a damage call.
                    dr.TriggerStatusEffect(_config.StatusEffect);
                }
            }

            _config.RaiseEffectTriggered();
        }
    }
}
