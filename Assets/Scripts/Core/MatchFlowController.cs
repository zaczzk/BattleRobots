using System.Collections.Generic;
using BattleRobots.Physics;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Scene-level coordinator that wires together the full match flow:
    ///   MatchManager → ArenaManager → RobotAssembler → AI/Locomotion → CameraRig.
    ///
    /// Responsibilities
    ///   On MatchStarted:
    ///     1. Triggers RobotAssembler.Assemble() on all registered assemblers.
    ///     2. Assigns the player robot's Transform as the target for every AI controller.
    ///     3. Calls CameraRig.SnapToTarget() to prevent lerp-in artefact on scene load.
    ///   On MatchEnded:
    ///     1. Disables every AI controller (→ Idle, Halt locomotion).
    ///     2. Halts every locomotion controller directly (safety net).
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to any persistent GameObject in the Arena scene.
    ///   2. Assign _matchStartedEvent and _matchEndedEvent SO channels
    ///      (same assets used by MatchManager and ArenaManager).
    ///   3. Assign _playerRobotRoot — the root Transform of the player-controlled robot.
    ///   4. Populate _aiControllers and _locomotionControllers with all combatants.
    ///   5. Assign _assemblers for each robot that uses RobotAssembler.
    ///   6. Assign _cameraRig (Main Camera's CameraRig component).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace. May reference BattleRobots.Physics.
    ///   • BattleRobots.UI must NOT reference this class.
    ///   • Zero heap allocations in hot paths: subscription callbacks are delegates
    ///     cached as fields in Awake, list iteration is value-type loops.
    ///   • All cross-component calls happen in event callbacks (cold path).
    /// </summary>
    public sealed class MatchFlowController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised by MatchManager when a round begins. " +
                 "MatchFlowController uses RegisterCallback, not a listener component.")]
        [SerializeField] private VoidGameEvent _matchStartedEvent;

        [Tooltip("Raised by MatchManager when the round ends (win or loss).")]
        [SerializeField] private VoidGameEvent _matchEndedEvent;

        [Header("Player Robot")]
        [Tooltip("Root Transform of the player-controlled robot. " +
                 "Assigned as the chase target for every AI controller on match start.")]
        [SerializeField] private Transform _playerRobotRoot;

        [Header("Camera")]
        [Tooltip("CameraRig on the Main Camera. SnapToTarget() called at match start.")]
        [SerializeField] private CameraRig _cameraRig;

        [Header("Assemblers")]
        [Tooltip("RobotAssembler components for all robots that should have parts instantiated " +
                 "at match start. Order does not matter.")]
        [SerializeField] private List<RobotAssembler> _assemblers = new List<RobotAssembler>();

        [Header("AI Controllers")]
        [Tooltip("All AI controllers in the scene. SetTarget(_playerRobotRoot) called at match start; " +
                 "Disable() called at match end.")]
        [SerializeField] private List<RobotAIController> _aiControllers = new List<RobotAIController>();

        [Header("Locomotion Controllers")]
        [Tooltip("All locomotion controllers in the scene. Halt() called at match end " +
                 "as a safety net alongside AI Disable().")]
        [SerializeField] private List<RobotLocomotionController> _locomotionControllers
            = new List<RobotLocomotionController>();

        // ── Cached delegate refs (prevents per-frame allocation) ──────────────

        private System.Action _onMatchStarted;
        private System.Action _onMatchEnded;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache delegates once so OnEnable/OnDisable never allocate.
            _onMatchStarted = HandleMatchStarted;
            _onMatchEnded   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _matchStartedEvent?.RegisterCallback(_onMatchStarted);
            _matchEndedEvent?.RegisterCallback(_onMatchEnded);
        }

        private void OnDisable()
        {
            _matchStartedEvent?.UnregisterCallback(_onMatchStarted);
            _matchEndedEvent?.UnregisterCallback(_onMatchEnded);
        }

        // ── Match event handlers ──────────────────────────────────────────────

        /// <summary>
        /// Called when the MatchStarted SO event fires.
        /// Assembles parts, sets AI targets, snaps camera.
        /// </summary>
        private void HandleMatchStarted()
        {
            // 1. Assemble robot parts.
            foreach (RobotAssembler assembler in _assemblers)
            {
                if (assembler != null)
                    assembler.Assemble();
            }

            // 2. Assign player root as the AI target.
            if (_playerRobotRoot != null)
            {
                foreach (RobotAIController ai in _aiControllers)
                {
                    if (ai != null)
                        ai.SetTarget(_playerRobotRoot);
                }
            }
            else
            {
                Debug.LogWarning("[MatchFlowController] _playerRobotRoot not assigned; AI targets not set.");
            }

            // 3. Snap camera to avoid lerp-in artefact.
            if (_cameraRig != null)
                _cameraRig.SnapToTarget();

            Debug.Log("[MatchFlowController] Match started: parts assembled, AI targets set, camera snapped.");
        }

        /// <summary>
        /// Called when the MatchEnded SO event fires.
        /// Disables all AI FSMs and halts all locomotion controllers.
        /// </summary>
        private void HandleMatchEnded()
        {
            foreach (RobotAIController ai in _aiControllers)
            {
                if (ai != null)
                    ai.Disable();
            }

            foreach (RobotLocomotionController loco in _locomotionControllers)
            {
                if (loco != null)
                    loco.Halt();
            }

            Debug.Log("[MatchFlowController] Match ended: all AI disabled, all locomotion halted.");
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_matchStartedEvent == null)
                Debug.LogWarning("[MatchFlowController] _matchStartedEvent not assigned.", this);
            if (_matchEndedEvent == null)
                Debug.LogWarning("[MatchFlowController] _matchEndedEvent not assigned.", this);
            if (_playerRobotRoot == null)
                Debug.LogWarning("[MatchFlowController] _playerRobotRoot not assigned.", this);
            if (_cameraRig == null)
                Debug.LogWarning("[MatchFlowController] _cameraRig not assigned.", this);
        }
#endif
    }
}
