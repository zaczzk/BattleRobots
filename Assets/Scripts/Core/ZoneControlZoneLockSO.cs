using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that temporarily locks a zone from capture for a
    /// configurable duration each time it changes hands.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="LockZone"/> when a zone is captured.
    ///   • Call <see cref="Tick(float)"/> each frame (via the controller's Update)
    ///     to advance the unlock timer.
    ///   • <see cref="IsLocked"/> gates recapture logic in the arena controller.
    ///   • <see cref="LockProgress"/> (0→1) can drive a UI cooldown bar.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneLock.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneLock", order = 46)]
    public sealed class ZoneControlZoneLockSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Lock Settings")]
        [Tooltip("Duration in seconds that a zone remains locked after changing hands.")]
        [Min(0.1f)]
        [SerializeField] private float _lockDuration = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when a zone becomes locked.")]
        [SerializeField] private VoidGameEvent _onZoneLocked;

        [Tooltip("Raised when the lock expires and the zone becomes capturable again.")]
        [SerializeField] private VoidGameEvent _onZoneUnlocked;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool  _isLocked;
        private float _lockTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the zone is in its post-capture lock period.</summary>
        public bool IsLocked => _isLocked;

        /// <summary>
        /// Lock countdown progress in the range [0, 1], where 1 = just locked
        /// and 0 = lock expired (or not locked).
        /// Returns 0 when the zone is not locked.
        /// </summary>
        public float LockProgress =>
            _isLocked && _lockDuration > 0f ? _lockTimer / _lockDuration : 0f;

        /// <summary>Configured lock duration in seconds.</summary>
        public float LockDuration => _lockDuration;

        /// <summary>Remaining lock time in seconds.</summary>
        public float LockTimer => _lockTimer;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Locks the zone for <see cref="LockDuration"/> seconds and fires
        /// <see cref="_onZoneLocked"/>.
        /// Calling while already locked resets the timer (extends the lock).
        /// </summary>
        public void LockZone()
        {
            _isLocked  = true;
            _lockTimer = _lockDuration;
            _onZoneLocked?.Raise();
        }

        /// <summary>
        /// Advances the lock timer by <paramref name="deltaTime"/> seconds.
        /// No-op when the zone is not currently locked.
        /// Fires <see cref="_onZoneUnlocked"/> when the timer reaches zero.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isLocked) return;

            _lockTimer -= deltaTime;
            if (_lockTimer <= 0f)
            {
                _isLocked  = false;
                _lockTimer = 0f;
                _onZoneUnlocked?.Raise();
            }
        }

        /// <summary>
        /// Clears the lock state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _isLocked  = false;
            _lockTimer = 0f;
        }
    }
}
