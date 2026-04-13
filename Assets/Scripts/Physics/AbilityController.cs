using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that manages a single activatable special ability on a robot part.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   • Enforces cooldown and energy prerequisites via <see cref="TryActivate"/>.
    ///   • Decrements the cooldown timer each FixedUpdate (zero heap allocations).
    ///   • Raises event channels on activation success / failure.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the robot part's GameObject.
    ///   2. Assign _ability → a PartAbilitySO asset.
    ///   3. Optionally assign _energySystem → the robot's EnergySystemSO.
    ///      When null, energy is not required (ability activates freely if off cooldown).
    ///   4. Optionally assign _onAbilityActivated / _onAbilityFailed VoidGameEvent SOs.
    ///   5. Call TryActivate() from an input handler or AI controller.
    ///
    /// ARCHITECTURE RULES:
    ///   • ArticulationBody-only project — no Rigidbody.
    ///   • BattleRobots.UI must NOT reference this class.
    ///   • FixedUpdate allocates nothing — only float arithmetic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Ability definition — cost, cooldown, id. Leave null to disable the ability slot.")]
        [SerializeField] private PartAbilitySO _ability;

        [Tooltip("Robot's energy pool. When null, energy is not required for activation.")]
        [SerializeField] private EnergySystemSO _energySystem;

        [Header("Event Channels Out")]
        [Tooltip("Raised when TryActivate succeeds. Listeners can trigger VFX, SFX, etc.")]
        [SerializeField] private VoidGameEvent _onAbilityActivated;

        [Tooltip("Raised when TryActivate fails (on cooldown or insufficient energy).")]
        [SerializeField] private VoidGameEvent _onAbilityFailed;

        // ── Runtime state (not serialized) ────────────────────────────────────

        private float _remainingCooldown;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True when the ability is still in its post-activation cooldown window.</summary>
        public bool IsOnCooldown => _remainingCooldown > 0f;

        /// <summary>Seconds remaining in the current cooldown. 0 when ready.</summary>
        public float RemainingCooldown => _remainingCooldown;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to activate the ability.
        /// Fails (returns <c>false</c> and raises <c>_onAbilityFailed</c>) when:
        /// <list type="bullet">
        ///   <item><c>_ability</c> is null.</item>
        ///   <item>The ability is still on cooldown.</item>
        ///   <item><c>_energySystem</c> is assigned and has insufficient energy.</item>
        /// </list>
        /// On success: consumes energy, starts the cooldown, raises <c>_onAbilityActivated</c>.
        /// </summary>
        /// <returns><c>true</c> if the ability activated successfully.</returns>
        public bool TryActivate()
        {
            if (_ability == null)
                return false;

            if (IsOnCooldown)
            {
                _onAbilityFailed?.Raise();
                return false;
            }

            if (_energySystem != null && !_energySystem.Consume(_ability.EnergyCost))
            {
                _onAbilityFailed?.Raise();
                return false;
            }

            _remainingCooldown = _ability.CooldownDuration;
            _onAbilityActivated?.Raise();
            return true;
        }

        // ── Unity messages ────────────────────────────────────────────────────

        private void FixedUpdate()
        {
            if (_remainingCooldown > 0f)
                _remainingCooldown = Mathf.Max(0f, _remainingCooldown - Time.fixedDeltaTime);
        }
    }
}
