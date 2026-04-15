using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject that tracks a per-zone capture cooldown. After a zone is
    /// lost the zone is locked for <see cref="CooldownDuration"/> seconds, during
    /// which re-capture is blocked.
    ///
    /// ── State machine ─────────────────────────────────────────────────────────
    ///   StartCooldown():
    ///     • Sets IsOnCooldown = true.
    ///     • Resets RemainingCooldown = CooldownDuration.
    ///     • Raises _onCooldownStarted (optional).
    ///   Tick(dt):
    ///     • No-op when not on cooldown.
    ///     • Decrements RemainingCooldown by dt (clamped at 0).
    ///     • When RemainingCooldown reaches 0 → IsOnCooldown=false → raises
    ///       _onCooldownEnded.
    ///   Reset():
    ///     • Clears cooldown state without firing events. Safe at match start.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on domain reload.
    ///   - Zero heap allocation on all hot-path methods (float arithmetic only).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneTimer.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneTimer", order = 20)]
    public sealed class ZoneTimerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Cooldown Settings")]
        [Tooltip("Seconds a zone is locked after being lost. Must be > 0.")]
        [SerializeField, Min(0.1f)] private float _cooldownDuration = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the cooldown begins (zone just lost).")]
        [SerializeField] private VoidGameEvent _onCooldownStarted;

        [Tooltip("Raised when the cooldown expires and the zone becomes capturable again.")]
        [SerializeField] private VoidGameEvent _onCooldownEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isOnCooldown;
        private float _remaining;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Configured cooldown duration in seconds.</summary>
        public float CooldownDuration => _cooldownDuration;

        /// <summary>True while the zone is locked after being lost.</summary>
        public bool IsOnCooldown => _isOnCooldown;

        /// <summary>Seconds remaining on the current cooldown (0 when idle).</summary>
        public float RemainingCooldown => _remaining;

        /// <summary>
        /// Normalised cooldown progress in [0, 1] (1 = just started, 0 = done).
        /// Suitable for Slider.value.
        /// </summary>
        public float CooldownRatio =>
            _cooldownDuration > 0f ? Mathf.Clamp01(_remaining / _cooldownDuration) : 0f;

        /// <summary>Event raised when cooldown starts. May be null.</summary>
        public VoidGameEvent OnCooldownStarted => _onCooldownStarted;

        /// <summary>Event raised when cooldown ends. May be null.</summary>
        public VoidGameEvent OnCooldownEnded => _onCooldownEnded;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins the capture cooldown. Sets <see cref="IsOnCooldown"/> to true,
        /// resets <see cref="RemainingCooldown"/> to <see cref="CooldownDuration"/>,
        /// and raises <see cref="_onCooldownStarted"/>.
        /// Zero allocation.
        /// </summary>
        public void StartCooldown()
        {
            _isOnCooldown = true;
            _remaining    = _cooldownDuration;
            _onCooldownStarted?.Raise();
        }

        /// <summary>
        /// Advances the cooldown timer by <paramref name="dt"/> seconds.
        /// No-op when not on cooldown. When the timer expires,
        /// <see cref="IsOnCooldown"/> is cleared and <see cref="_onCooldownEnded"/>
        /// is raised.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isOnCooldown) return;

            _remaining -= dt;
            if (_remaining <= 0f)
            {
                _remaining    = 0f;
                _isOnCooldown = false;
                _onCooldownEnded?.Raise();
            }
        }

        /// <summary>
        /// Silently clears all cooldown state.
        /// Does NOT fire any events — safe to call at match start.
        /// </summary>
        public void Reset()
        {
            _isOnCooldown = false;
            _remaining    = 0f;
        }
    }
}
