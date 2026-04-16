using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that implements a per-zone countdown timer.
    /// Blocks zone capture until the countdown expires.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="StartCountdown"/> to arm the timer.
    ///   Call <see cref="Tick(float)"/> each frame (via controller Update) to advance it.
    ///   Fires <see cref="_onCountdownExpired"/> and clears <see cref="IsActive"/>
    ///   when the timer reaches zero.
    ///   <see cref="Progress"/> is normalised [0→1] while active, 0 when idle.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneCountdown.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneCountdown", order = 56)]
    public sealed class ZoneControlZoneCountdownSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Countdown duration in seconds before the zone becomes capturable.")]
        [Min(0.1f)]
        [SerializeField] private float _duration = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the countdown reaches zero.")]
        [SerializeField] private VoidGameEvent _onCountdownExpired;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool  _isActive;
        private float _timer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the countdown is running.</summary>
        public bool IsActive => _isActive;

        /// <summary>Configured countdown duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>
        /// Normalised countdown progress [0→1] while active.
        /// 1 immediately after <see cref="StartCountdown"/>;
        /// approaches 0 as the timer drains; always 0 when idle.
        /// </summary>
        public float Progress => _isActive ? Mathf.Clamp01(_timer / _duration) : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Arms the countdown timer.
        /// Resets the timer to <see cref="Duration"/> and sets <see cref="IsActive"/> true.
        /// </summary>
        public void StartCountdown()
        {
            _isActive = true;
            _timer    = _duration;
        }

        /// <summary>
        /// Advances the countdown by <paramref name="deltaTime"/> seconds.
        /// Fires <see cref="_onCountdownExpired"/> and clears <see cref="IsActive"/>
        /// when the timer reaches zero.
        /// No-op when not active.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isActive) return;

            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                _timer    = 0f;
                _isActive = false;
                _onCountdownExpired?.Raise();
            }
        }

        /// <summary>
        /// Clears the countdown state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _isActive = false;
            _timer    = 0f;
        }

        private void OnValidate()
        {
            _duration = Mathf.Max(0.1f, _duration);
        }
    }
}
