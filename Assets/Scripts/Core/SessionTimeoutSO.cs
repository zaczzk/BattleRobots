using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pure-data ScriptableObject that implements a configurable countdown timer.
    /// Designed for use by the network reconnection flow: when a disconnect is
    /// detected, start the timer; if it expires before reconnection succeeds,
    /// fire <see cref="_onTimeout"/> and let the caller (e.g. a MonoBehaviour
    /// driver in FixedUpdate/Update) call <see cref="Tick"/> each frame.
    ///
    /// This SO has no MonoBehaviour — the caller is responsible for driving
    /// <see cref="Tick(float)"/> (typically with <c>Time.deltaTime</c>).
    ///
    /// Lifecycle:
    ///   1. Call <see cref="Reset"/> to start (or restart) the countdown.
    ///   2. Call <see cref="Tick(float)"/> every frame (pass <c>Time.deltaTime</c>).
    ///   3. <see cref="_onTimeout"/> fires exactly once when the timer expires.
    ///   4. Call <see cref="Stop"/> to cancel without firing the event.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Network ▶ SessionTimeoutSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/SessionTimeoutSO", order = 1)]
    public sealed class SessionTimeoutSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Timeout Settings")]
        [Tooltip("Duration of the countdown in seconds.")]
        [SerializeField, Min(0.1f)] private float _timeoutDuration = 10f;

        [Header("Event Channel")]
        [Tooltip("Fired once when the countdown reaches zero.")]
        [SerializeField] private VoidGameEvent _onTimeout;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>Seconds remaining in the current countdown. Zero when not running.</summary>
        public float RemainingTime { get; private set; }

        /// <summary>True while the countdown is active (between Reset and expiry/Stop).</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Configured timeout duration (read-only view of the SO field).</summary>
        public float TimeoutDuration => _timeoutDuration;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// (Re)starts the countdown from <see cref="TimeoutDuration"/>.
        /// Safe to call when already running — restarts from the full duration.
        /// </summary>
        public void Reset()
        {
            RemainingTime = _timeoutDuration;
            IsRunning     = true;
        }

        /// <summary>
        /// Advances the countdown by <paramref name="deltaTime"/> seconds.
        /// Fires <see cref="_onTimeout"/> exactly once when the countdown reaches zero.
        /// No-op when <see cref="IsRunning"/> is false.
        /// </summary>
        /// <param name="deltaTime">Elapsed time this frame (pass <c>Time.deltaTime</c>).</param>
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            RemainingTime -= deltaTime;

            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                IsRunning     = false;
                _onTimeout?.Raise();
            }
        }

        /// <summary>
        /// Stops the countdown without firing the timeout event.
        /// Use when reconnection succeeds or the player cancels manually.
        /// </summary>
        public void Stop()
        {
            IsRunning     = false;
            RemainingTime = 0f;
        }
    }
}
