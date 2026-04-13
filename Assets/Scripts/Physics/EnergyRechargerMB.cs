using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that passively recharges a robot's <see cref="EnergySystemSO"/> pool
    /// each physics tick by calling <see cref="EnergySystemSO.Recharge"/> with
    /// <see cref="Time.fixedDeltaTime"/>.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the robot's root GameObject (alongside AbilityController).
    ///   2. Assign _energySystem → the robot's EnergySystemSO asset.
    ///   3. The recharge rate is configured on the EnergySystemSO asset itself
    ///      (_rechargeRate field). No extra configuration needed here.
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — no UI references.
    ///   • FixedUpdate allocates nothing — one null-check + one method call.
    ///   • _energySystem is optional; leave null to disable passive regen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnergyRechargerMB : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Energy pool to recharge each FixedUpdate. Leave null to disable passive regen.")]
        [SerializeField] private EnergySystemSO _energySystem;

        // ── Unity messages ────────────────────────────────────────────────────

        private void FixedUpdate()
        {
            _energySystem?.Recharge(Time.fixedDeltaTime);
        }
    }
}
