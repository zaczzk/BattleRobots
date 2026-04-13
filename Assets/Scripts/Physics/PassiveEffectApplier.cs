using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Applies a single passive part bonus to one or more robot systems at match start.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   Subscribes to <c>_onMatchStarted</c> and calls <see cref="Apply"/> once per match.
    ///   Depending on the <see cref="PassiveStatType"/> in the linked
    ///   <see cref="PassivePartEffectSO"/>, Apply routes the value to:
    ///   <list type="bullet">
    ///     <item><b>DamageReduction</b> — <see cref="DamageReceiver.SetArmorRating"/> (+flat armor).</item>
    ///     <item><b>MaxHealthBonus</b>  — <see cref="HealthSO.InitForMatch"/> (+flat max HP) then Reset.</item>
    ///     <item><b>RechargeRateBonus</b> — <see cref="EnergySystemSO.SetRechargeRate"/> (+flat rate).</item>
    ///   </list>
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the robot part's GameObject.
    ///   2. Assign <c>_effect</c> → a PassivePartEffectSO asset.
    ///   3. Assign the relevant target field for the chosen stat type:
    ///      • DamageReduction   → <c>_damageReceiver</c>
    ///      • MaxHealthBonus    → <c>_health</c>
    ///      • RechargeRateBonus → <c>_energySystem</c>
    ///   4. Assign <c>_onMatchStarted</c> → the shared match-start VoidGameEvent
    ///      (same channel used by MatchManager).
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.Physics namespace — no UI references.
    ///   • Action delegate cached in Awake — no heap allocation in OnEnable/OnDisable.
    ///   • Apply() is public for direct EditMode testing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PassiveEffectApplier : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Passive effect definition. Leave null to disable this applier.")]
        [SerializeField] private PassivePartEffectSO _effect;

        [Header("Targets")]
        [Tooltip("Required for DamageReduction stat type. Receives the armor-rating bonus.")]
        [SerializeField] private DamageReceiver _damageReceiver;

        [Tooltip("Required for MaxHealthBonus stat type. Receives the max-health increase " +
                 "via InitForMatch + Reset.")]
        [SerializeField] private HealthSO _health;

        [Tooltip("Required for RechargeRateBonus stat type. Receives the recharge-rate bonus " +
                 "via SetRechargeRate.")]
        [SerializeField] private EnergySystemSO _energySystem;

        [Header("Event Channel In")]
        [Tooltip("Fired by MatchManager at match start. When raised, Apply() is called. " +
                 "Leave null to suppress automatic application (call Apply() manually).")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _applyDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = Apply;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_applyDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_applyDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies the passive bonus to the appropriate target system.
        /// Exposed as public to support direct EditMode testing and manual
        /// invocation from other controllers.
        ///
        /// No-op when <c>_effect</c> is null or the relevant target field is unassigned.
        /// Allocation-free — only arithmetic and property reads.
        /// </summary>
        public void Apply()
        {
            if (_effect == null) return;

            switch (_effect.StatType)
            {
                case PassiveStatType.DamageReduction:
                    if (_damageReceiver != null)
                    {
                        int newRating = Mathf.Clamp(
                            _damageReceiver.ArmorRating + Mathf.RoundToInt(_effect.Value),
                            0, 100);
                        _damageReceiver.SetArmorRating(newRating);
                    }
                    break;

                case PassiveStatType.MaxHealthBonus:
                    if (_health != null)
                    {
                        _health.InitForMatch(_health.MaxHealth + _effect.Value);
                        _health.Reset();
                    }
                    break;

                case PassiveStatType.RechargeRateBonus:
                    _energySystem?.SetRechargeRate(_energySystem.RechargeRate + _effect.Value);
                    break;
            }
        }
    }
}
