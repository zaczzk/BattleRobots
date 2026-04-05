using UnityEngine;
using UnityEngine.InputSystem;
using BattleRobots.Core;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Drives gamepad haptic feedback (rumble) in response to robot combat events.
    ///
    /// How it works:
    ///   1. Wire <see cref="_onDamageReceived"/> to the player robot's HealthSO
    ///      DamageEvent channel via a <see cref="DamageEventListener"/> component
    ///      (or call <see cref="OnDamageReceived"/> directly from a UnityEvent).
    ///   2. Wire <see cref="_onRobotDeath"/> to the player robot's VoidGameEvent
    ///      onDeath channel for the destruction burst.
    ///   3. Attach this component to any persistent scene GameObject.
    ///
    /// Architecture rules:
    ///   - <c>BattleRobots.Physics</c> namespace; uses Core via event-channel types only.
    ///   - No allocations in hot paths — motor-speed structs are value types.
    ///   - Haptic calls are gated behind null-checks for first-connected gamepad.
    ///   - Rumble is cancelled in <c>OnDisable</c> so it never gets stuck.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamepadRumble : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Hit Rumble")]
        [Tooltip("Low-frequency (heavy) motor intensity for a hit. [0, 1]")]
        [SerializeField, Range(0f, 1f)] private float _hitLowFreq  = 0.4f;

        [Tooltip("High-frequency (light) motor intensity for a hit. [0, 1]")]
        [SerializeField, Range(0f, 1f)] private float _hitHighFreq = 0.6f;

        [Tooltip("Duration in seconds that the hit rumble plays.")]
        [SerializeField, Min(0f)] private float _hitDuration = 0.12f;

        [Tooltip("Damage threshold below which no rumble is triggered (filters tiny scrapes).")]
        [SerializeField, Min(0f)] private float _minDamageThreshold = 1f;

        [Tooltip("Damage value at which rumble reaches maximum intensity.")]
        [SerializeField, Min(1f)] private float _maxDamageForFullRumble = 40f;

        [Header("Death Burst")]
        [Tooltip("Low-frequency motor intensity for the destruction burst.")]
        [SerializeField, Range(0f, 1f)] private float _deathLowFreq  = 1f;

        [Tooltip("High-frequency motor intensity for the destruction burst.")]
        [SerializeField, Range(0f, 1f)] private float _deathHighFreq = 0.8f;

        [Tooltip("Duration in seconds of the death burst rumble.")]
        [SerializeField, Min(0f)] private float _deathDuration = 0.4f;

        // ── Runtime ───────────────────────────────────────────────────────────

        private float _rumbleEndTime;   // Time.time when the current rumble should stop
        private bool  _rumbling;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            // Stop rumble when the timer expires — no allocations; all value-type ops.
            if (_rumbling && Time.time >= _rumbleEndTime)
            {
                StopRumble();
            }
        }

        private void OnDisable()
        {
            // Always cancel any in-flight rumble when the component is disabled
            // (scene unload, robot destruction) to prevent the gamepad staying locked.
            StopRumble();
        }

        // ── Public API (wired via DamageEventListener / VoidGameEventListener) ─

        /// <summary>
        /// Trigger hit rumble. Wire to the player robot's DamageEvent channel.
        /// Intensity is scaled proportionally to <paramref name="payload"/>.amount.
        /// </summary>
        public void OnDamageReceived(DamagePayload payload)
        {
            if (payload.amount < _minDamageThreshold) return;

            float t         = Mathf.Clamp01(payload.amount / _maxDamageForFullRumble);
            float lowFreq   = _hitLowFreq  * t;
            float highFreq  = _hitHighFreq * t;

            StartRumble(lowFreq, highFreq, _hitDuration);
        }

        /// <summary>
        /// Trigger destruction burst. Wire to the player robot's onDeath VoidGameEvent.
        /// </summary>
        public void OnRobotDeath()
        {
            StartRumble(_deathLowFreq, _deathHighFreq, _deathDuration);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Sets motor speeds on the first connected Gamepad, replacing any active rumble.
        /// No-ops gracefully when no gamepad is connected.
        /// </summary>
        private void StartRumble(float lowFreq, float highFreq, float duration)
        {
            Gamepad pad = Gamepad.current;
            if (pad == null) return;

            pad.SetMotorSpeeds(lowFreq, highFreq);

            _rumbleEndTime = Time.time + duration;
            _rumbling      = true;
        }

        /// <summary>Zeros all motor speeds on the current gamepad (if any).</summary>
        private void StopRumble()
        {
            _rumbling = false;
            Gamepad pad = Gamepad.current;
            if (pad != null)
                pad.SetMotorSpeeds(0f, 0f);
        }
    }
}
