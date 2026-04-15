using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that drives a <see cref="ZoneTimerSO"/> per-zone cooldown:
    /// starts the cooldown when a zone is lost and ticks it each frame.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onZoneLost fires → HandleZoneLost() → _timerSO?.StartCooldown().
    ///   _onMatchStarted fires → HandleMatchStarted() → _timerSO?.Reset().
    ///   Update() → _timerSO?.Tick(Time.deltaTime) when active.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one timer controller per zone.
    ///   - Tick is public for EditMode test driving.
    ///
    /// Scene wiring:
    ///   _timerSO         → ZoneTimerSO asset (cooldown config + state).
    ///   _onZoneLost      → VoidGameEvent raised by ControlZoneSO._onLost.
    ///   _onMatchStarted  → VoidGameEvent raised by MatchManager at match start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneTimerController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Timer SO that owns the cooldown state and events for this zone.")]
        [SerializeField] private ZoneTimerSO _timerSO;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by ControlZoneSO._onLost when the zone is lost. Starts the cooldown.")]
        [SerializeField] private VoidGameEvent _onZoneLost;

        [Tooltip("Raised by MatchManager at match start. Resets the cooldown state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _zoneLostDelegate;
        private Action _matchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _zoneLostDelegate     = HandleZoneLost;
            _matchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onZoneLost?.RegisterCallback(_zoneLostDelegate);
            _onMatchStarted?.RegisterCallback(_matchStartedDelegate);
        }

        private void OnDisable()
        {
            _onZoneLost?.UnregisterCallback(_zoneLostDelegate);
            _onMatchStarted?.UnregisterCallback(_matchStartedDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the capture cooldown on the timer SO.
        /// Wired to <c>_onZoneLost</c>.
        /// </summary>
        public void HandleZoneLost()
        {
            _timerSO?.StartCooldown();
        }

        /// <summary>
        /// Resets the timer SO at match start so the zone is capturable immediately.
        /// Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            _timerSO?.Reset();
        }

        /// <summary>
        /// Advances the cooldown timer. Called from <c>Update</c>;
        /// public for EditMode test driving. Zero allocation.
        /// </summary>
        public void Tick(float dt)
        {
            _timerSO?.Tick(dt);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneTimerSO"/>. May be null.</summary>
        public ZoneTimerSO TimerSO => _timerSO;
    }
}
