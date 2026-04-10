using System.Collections;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Raises the MatchStarted VoidGameEvent on <see cref="Start"/>, optionally
    /// after a short delay. Place one instance in the Arena scene.
    ///
    /// This is the sole authority that fires the MatchStarted channel; all other
    /// systems only subscribe to it:
    ///   • MatchManager.HandleMatchStarted()   — resets health, starts timer
    ///   • ArenaManager.HandleMatchStarted()   — positions robots at spawn points
    ///   • MatchFlowController                 — assembles parts, sets AI targets
    ///   • PauseManager                        — enables Escape-key pause
    ///   • CombatHUDController                 — shows HUD
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to any persistent Arena-scene GameObject
    ///      (e.g., the GameManager empty).
    ///   2. Assign <c>_matchStartedEvent</c> — the same VoidGameEvent SO used by
    ///      MatchManager, MatchFlowController, ArenaManager, PauseManager, and
    ///      CombatHUDController.
    ///   3. Optionally set <c>_startDelay</c> (seconds) to give the physics
    ///      simulation time to settle before gameplay begins.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Raises exactly once per scene load (no Update logic).
    ///   - Coroutine used only when _startDelay > 0; otherwise fires synchronously
    ///     in Start() after all Awake() subscriptions have completed.
    /// </summary>
    public sealed class MatchStarter : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel — Out")]
        [Tooltip("VoidGameEvent SO shared with MatchManager, MatchFlowController, " +
                 "ArenaManager, PauseManager, and CombatHUDController.")]
        [SerializeField] private VoidGameEvent _matchStartedEvent;

        [Header("Timing")]
        [Tooltip("Seconds to wait after Start() before raising MatchStarted. " +
                 "Use 0 for immediate start. A small value (e.g. 0.1) allows " +
                 "ArticulationBody physics to settle before movement begins.")]
        [SerializeField, Min(0f)] private float _startDelay = 0.1f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (_matchStartedEvent == null)
            {
                Debug.LogError("[MatchStarter] _matchStartedEvent is not assigned — " +
                               "MatchStarted will never fire. Assign the SO in the Inspector.");
                return;
            }

            if (_startDelay > 0f)
                StartCoroutine(RaiseAfterDelay());
            else
                RaiseMatchStarted();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private IEnumerator RaiseAfterDelay()
        {
            yield return new WaitForSeconds(_startDelay);
            RaiseMatchStarted();
        }

        private void RaiseMatchStarted()
        {
            Debug.Log("[MatchStarter] Raising MatchStarted.");
            _matchStartedEvent.Raise();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_matchStartedEvent == null)
                Debug.LogWarning("[MatchStarter] _matchStartedEvent VoidGameEvent SO not assigned.", this);
        }
#endif
    }
}
