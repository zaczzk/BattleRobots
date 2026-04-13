using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that polls player input each Update and forwards activation
    /// requests to an <see cref="AbilityController"/>.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────
    ///   Update → Input.GetKeyDown(_activationKey)
    ///          → AbilityController.TryActivate()
    ///             ✓ success : ability fires (no extra action here)
    ///             ✗ failure : optional _onInputBlocked raised for UI feedback
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the player robot's root GameObject.
    ///   2. Assign _abilityController → the AbilityController on the same robot.
    ///   3. (Optional) Set _activationKey — default is Space.
    ///   4. (Optional) Assign _onInputBlocked → a VoidGameEvent channel for "can't use"
    ///      feedback (e.g., flash a "NOT READY" label via a UI listener).
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — no UI references.
    ///   • Update allocates nothing — Input.GetKeyDown is zero-alloc.
    ///   • All fields optional and null-guarded; missing _abilityController is a no-op.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityInputController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Ability")]
        [Tooltip("AbilityController to activate when the player presses _activationKey.")]
        [SerializeField] private AbilityController _abilityController;

        [Header("Input")]
        [Tooltip("Key the player presses to activate the ability. Default: Space.")]
        [SerializeField] private KeyCode _activationKey = KeyCode.Space;

        [Header("Event Channels Out")]
        [Tooltip("Raised when the player presses the key but TryActivate fails " +
                 "(on cooldown or insufficient energy). Use for 'NOT READY' UI feedback.")]
        [SerializeField] private VoidGameEvent _onInputBlocked;

        // ── Unity messages ────────────────────────────────────────────────────

        private void Update()
        {
            if (!Input.GetKeyDown(_activationKey)) return;
            if (_abilityController == null) return;
            if (!_abilityController.TryActivate())
                _onInputBlocked?.Raise();
        }
    }
}
