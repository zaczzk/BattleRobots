using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks respawn availability and cooldown for a robot.
    ///
    /// ── Typical flow ─────────────────────────────────────────────────────────
    ///   1. At match start, call <see cref="Reset"/> (or wire to _onMatchStarted).
    ///   2. When a robot is destroyed, the match system calls <see cref="RequestRespawn"/>.
    ///      Returns <c>true</c> if a respawn slot is available; <c>false</c> when exhausted.
    ///   3. <see cref="RespawnCountdownController"/> calls <see cref="Tick"/> each Update
    ///      while on cooldown. When the timer expires, <c>_onRespawnReady</c> fires.
    ///
    /// ── Mutators ─────────────────────────────────────────────────────────────
    ///   <see cref="RequestRespawn"/> — consumes one respawn slot and starts the timer.
    ///   <see cref="Tick"/>           — decrements the cooldown timer each frame.
    ///   <see cref="Reset"/>          — clears used-count and timer (silent, no events).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is not serialised to the SO asset.
    ///   - <see cref="Reset"/> is silent (bootstrapper / match-start safe).
    ///   - <see cref="Tick"/> fires <c>_onRespawnReady</c> exactly once per cooldown expiry.
    ///   - Zero allocation: all logic is integer / float arithmetic.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ RobotRespawn.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/RobotRespawn", fileName = "RobotRespawnSO")]
    public sealed class RobotRespawnSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum number of times the robot may respawn during a match. " +
                 "Set to 0 for no respawns (standard permadeath).")]
        [SerializeField, Min(0)] private int _maxRespawns = 3;

        [Tooltip("Seconds the robot must wait between respawns. " +
                 "The countdown starts immediately after RequestRespawn() is called.")]
        [SerializeField, Min(0.1f)] private float _respawnCooldown = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when a respawn slot is consumed by RequestRespawn().")]
        [SerializeField] private VoidGameEvent _onRespawnUsed;

        [Tooltip("Raised when the respawn cooldown reaches zero after RequestRespawn().")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int   _respawnsUsed;
        private float _cooldownTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            _respawnsUsed  = 0;
            _cooldownTimer = 0f;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of respawns configured for this robot.</summary>
        public int MaxRespawns => _maxRespawns;

        /// <summary>Configured cooldown duration in seconds between respawns.</summary>
        public float RespawnCooldown => _respawnCooldown;

        /// <summary>How many respawn slots have been consumed so far this match.</summary>
        public int RespawnsUsed => _respawnsUsed;

        /// <summary>How many respawn slots remain (clamped at 0).</summary>
        public int RespawnsRemaining => Mathf.Max(0, _maxRespawns - _respawnsUsed);

        /// <summary>True while the respawn cooldown is running.</summary>
        public bool IsOnCooldown => _cooldownTimer > 0f;

        /// <summary>Remaining cooldown time in seconds. Zero when not on cooldown.</summary>
        public float CooldownTimer => _cooldownTimer;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to consume a respawn slot and begin the cooldown.
        /// <para>
        /// Returns <c>false</c> and does nothing if all slots are exhausted.
        /// On success: increments <see cref="RespawnsUsed"/>, sets the cooldown timer,
        /// and raises <c>_onRespawnUsed</c>.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if a slot was available and consumed; otherwise <c>false</c>.</returns>
        public bool RequestRespawn()
        {
            if (_respawnsUsed >= _maxRespawns) return false;

            _respawnsUsed++;
            _cooldownTimer = _respawnCooldown;
            _onRespawnUsed?.Raise();
            return true;
        }

        /// <summary>
        /// Advances the respawn cooldown by <paramref name="dt"/> seconds.
        /// When the timer reaches zero, raises <c>_onRespawnReady</c> once.
        /// No-op when not on cooldown.
        /// </summary>
        /// <param name="dt">Elapsed time in seconds (typically <c>Time.deltaTime</c>).</param>
        public void Tick(float dt)
        {
            if (_cooldownTimer <= 0f) return;

            _cooldownTimer -= dt;
            if (_cooldownTimer <= 0f)
            {
                _cooldownTimer = 0f;
                _onRespawnReady?.Raise();
            }
        }

        /// <summary>
        /// Resets respawn count and cooldown timer to zero without raising any events.
        /// Call at match start.
        /// </summary>
        public void Reset()
        {
            _respawnsUsed  = 0;
            _cooldownTimer = 0f;
        }
    }
}
