using System;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Maps legacy Input axes to <see cref="HingeJointAB.SetTargetVelocity"/> calls,
    /// driving the player-controlled robot's joints from keyboard / gamepad input.
    ///
    /// Architecture:
    ///   - All input reads and joint writes happen in FixedUpdate only.
    ///   - Zero heap allocation: _bindings is a serialised struct array; Input.GetAxis
    ///     returns a float; ref readonly avoids struct copies in the hot loop.
    ///   - Input is gated by MatchStarted/MatchEnded VoidGameEvent channels so the
    ///     player cannot steer before or after a round.
    ///   - BattleRobots.Physics namespace; references only Core for SO event types.
    ///
    /// Scene wiring:
    ///   1. Add one RobotInputController to the root of the player robot hierarchy.
    ///   2. Populate _bindings: for each driven joint, assign the HingeJointAB,
    ///      axis name (e.g. "Horizontal", "Vertical"), and a max speed.
    ///   3. Assign _onMatchStarted and _onMatchEnded SO channels to gate input.
    ///
    /// Default axes use Unity's legacy Input Manager settings:
    ///   "Horizontal" → A/D keys or left stick X.
    ///   "Vertical"   → W/S keys or left stick Y.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotInputController : MonoBehaviour
    {
        // ── Inner types ────────────────────────────────────────────────────────

        [Serializable]
        public struct JointBinding
        {
            [Tooltip("HingeJointAB on this robot to drive.")]
            public HingeJointAB joint;

            [Tooltip("Legacy Input axis name (e.g. \"Horizontal\"). Must match Project Settings ▶ Input.")]
            public string axisName;

            [Tooltip("Maximum drive velocity (degrees/s) when the axis reads ±1.")]
            [Min(0f)] public float maxVelocityDegPerSec;

            [Tooltip("Flip the axis direction for this joint (useful for mirrored joints).")]
            public bool invert;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Joint Bindings")]
        [Tooltip("One entry per driven joint. Each joint may bind a different axis.")]
        [SerializeField] private JointBinding[] _bindings;

        [Header("Match Gate — Event Channels")]
        [Tooltip("Optional VoidGameEvent that enables player input (e.g. MatchStarted).")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Optional VoidGameEvent that disables player input (e.g. MatchEnded).")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Input begins enabled so robots respond immediately in test scenes
        // without needing a MatchStarted event.
        private bool _inputEnabled = true;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(EnableInput);
            _onMatchEnded?.RegisterCallback(DisableInput);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(EnableInput);
            _onMatchEnded?.UnregisterCallback(DisableInput);
        }

        private void FixedUpdate()
        {
            if (!_inputEnabled || _bindings == null) return;

            for (int i = 0; i < _bindings.Length; i++)
            {
                // ref readonly: avoids copying the struct; safe because we only read fields.
                ref readonly JointBinding b = ref _bindings[i];

                if (b.joint == null || string.IsNullOrEmpty(b.axisName)) continue;

                float axis  = Input.GetAxis(b.axisName);                  // float — no alloc
                float speed = axis * b.maxVelocityDegPerSec;
                if (b.invert) speed = -speed;

                b.joint.SetTargetVelocity(speed);
            }
        }

        // ── Callbacks (registered to SO channels) ─────────────────────────────

        private void EnableInput()  => _inputEnabled = true;
        private void DisableInput() => _inputEnabled = false;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_bindings == null) return;
            for (int i = 0; i < _bindings.Length; i++)
            {
                if (_bindings[i].joint == null)
                    Debug.LogWarning($"[RobotInputController] '{name}': binding[{i}].joint is not assigned.");
                if (string.IsNullOrEmpty(_bindings[i].axisName))
                    Debug.LogWarning($"[RobotInputController] '{name}': binding[{i}].axisName is empty.");
            }
        }
#endif
    }
}
