using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks respawn state for a single robot:
    /// how many respawns remain and whether a cooldown is currently active.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. <see cref="OnEnable"/> (or <see cref="Reset"/>) initialises state.
    ///   2. Call <see cref="UseRespawn"/> when the robot is destroyed — decrements the
    ///      remaining count and starts the cooldown timer.
    ///   3. Tick <see cref="Tick"/> every frame (dt = Time.deltaTime) to decrement the
    ///      cooldown timer; fires <see cref="_onRespawnReady"/> when the cooldown expires.
    ///   4. Wire <see cref="_onRespawnUsed"/> to trigger respawn VFX / audio.
    ///   5. Wire <see cref="_onRespawnReady"/> to re-enable the robot.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All arithmetic is float/integer — zero heap allocation per Tick call.
    ///   - SO state resets on OnEnable so Play-mode restarts begin clean.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ RobotRespawn.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/RobotRespawn")]
    public sealed class RobotRespawnSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Respawn Settings")]
        [Tooltip("Maximum number of respawns available per match. 0 = no respawns.")]
        [SerializeField, Min(0)] private int _maxRespawns = 3;

        [Tooltip("Cooldown duration in seconds after each respawn is used.")]
        [SerializeField, Min(0.1f)] private float _respawnCooldown = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when UseRespawn() succeeds (robot destroyed, cooldown started).")]
        [SerializeField] private VoidGameEvent _onRespawnUsed;

        [Tooltip("Raised when the cooldown timer expires and the robot may re-enter.")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _respawnsRemaining;
        private float _cooldownTimer;
        private bool  _isOnCooldown;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _respawnsRemaining = _maxRespawns;
            _cooldownTimer     = 0f;
            _isOnCooldown      = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum respawns configured for this robot.</summary>
        public int MaxRespawns => _maxRespawns;

        /// <summary>Number of respawns still available.</summary>
        public int RespawnsRemaining => _respawnsRemaining;

        /// <summary>True while a respawn cooldown is counting down.</summary>
        public bool IsOnCooldown => _isOnCooldown;

        /// <summary>Seconds remaining on the active cooldown (0 when not cooling down).</summary>
        public float CooldownTimeRemaining => _cooldownTimer;

        /// <summary>
        /// Normalised cooldown progress in [0, 1].
        /// 1 = cooldown just started; 0 = cooldown finished (or not active).
        /// Suitable for driving a Slider or radial fill.
        /// </summary>
        public float CooldownRatio =>
            _isOnCooldown && _respawnCooldown > 0f
                ? Mathf.Clamp01(_cooldownTimer / _respawnCooldown)
                : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to use a respawn.
        /// Returns <c>false</c> (no-op) when no respawns remain or a cooldown is active.
        /// On success: decrements <see cref="RespawnsRemaining"/>, starts the cooldown
        /// timer, and raises <see cref="_onRespawnUsed"/>.
        /// </summary>
        public bool UseRespawn()
        {
            if (_respawnsRemaining <= 0 || _isOnCooldown) return false;

            _respawnsRemaining--;
            _isOnCooldown  = true;
            _cooldownTimer = _respawnCooldown;
            _onRespawnUsed?.Raise();
            return true;
        }

        /// <summary>
        /// Advances the cooldown timer by <paramref name="deltaTime"/> seconds.
        /// No-op when not on cooldown.
        /// Fires <see cref="_onRespawnReady"/> and clears the cooldown flag when the
        /// timer reaches zero.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isOnCooldown) return;

            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - deltaTime);

            if (_cooldownTimer <= 0f)
            {
                _isOnCooldown = false;
                _onRespawnReady?.Raise();
            }
        }

        /// <summary>
        /// Resets all runtime state to initial values (matching <see cref="OnEnable"/>).
        /// Does NOT fire any event.
        /// Call at match start via a VoidGameEventListener.
        /// </summary>
        public void Reset()
        {
            _respawnsRemaining = _maxRespawns;
            _cooldownTimer     = 0f;
            _isOnCooldown      = false;
        }
    }
}
