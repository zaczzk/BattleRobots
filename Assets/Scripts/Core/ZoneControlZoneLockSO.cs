using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that temporarily locks a zone from recapture for
    /// a configurable duration after it changes hands.
    ///
    /// Call <see cref="LockZone"/> when a zone is captured.  Drive the unlock timer
    /// by calling <see cref="Tick"/> from a MonoBehaviour's Update loop.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on Tick / LockZone hot paths.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneLock.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneLock", order = 46)]
    public sealed class ZoneControlZoneLockSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Lock Settings")]
        [Tooltip("Duration in seconds a zone is locked from recapture after changing hands.")]
        [Min(0.1f)]
        [SerializeField] private float _lockDuration = 3f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when LockZone is called and the zone was previously unlocked.")]
        [SerializeField] private VoidGameEvent _onZoneLocked;

        [Tooltip("Raised when the lock timer expires and the zone becomes capturable again.")]
        [SerializeField] private VoidGameEvent _onZoneUnlocked;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool  _isLocked;
        private float _lockTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the zone is locked from recapture.</summary>
        public bool IsLocked => _isLocked;

        /// <summary>Configured lock duration in seconds (serialised).</summary>
        public float LockDuration => _lockDuration;

        /// <summary>Remaining lock time in seconds.</summary>
        public float LockTimer => _lockTimer;

        /// <summary>
        /// Normalised lock progress in [0, 1] — 1 = just locked, 0 = unlocked.
        /// Returns 0 when the zone is not locked.
        /// </summary>
        public float LockProgress =>
            (_isLocked && _lockDuration > 0f)
                ? Mathf.Clamp01(_lockTimer / _lockDuration)
                : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Locks the zone for <see cref="LockDuration"/> seconds.
        /// Resets the timer if the zone was already locked.
        /// Fires <see cref="_onZoneLocked"/> only on the first lock (not on re-lock).
        /// </summary>
        public void LockZone()
        {
            bool wasLocked = _isLocked;
            _isLocked  = true;
            _lockTimer = _lockDuration;
            if (!wasLocked)
                _onZoneLocked?.Raise();
        }

        /// <summary>
        /// Advances the lock timer by <paramref name="dt"/> seconds.
        /// Unlocks the zone and fires <see cref="_onZoneUnlocked"/> when the timer
        /// expires.  No-op when the zone is not locked.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isLocked) return;
            _lockTimer -= dt;
            if (_lockTimer <= 0f)
            {
                _isLocked  = false;
                _lockTimer = 0f;
                _onZoneUnlocked?.Raise();
            }
        }

        /// <summary>
        /// Resets lock state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _isLocked  = false;
            _lockTimer = 0f;
        }
    }
}
