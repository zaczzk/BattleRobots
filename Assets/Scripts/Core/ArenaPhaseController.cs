using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Drives a data-driven N-phase arena lifecycle defined by an
    /// <see cref="ArenaPhaseControllerSO"/>.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. Subscribes to <c>_onMatchStarted</c> / <c>_onMatchEnded</c> in OnEnable.
    ///   2. On <see cref="HandleMatchStarted"/>: resets to phase 0, fires the first
    ///      phase's event, and starts the timer.
    ///   3. Each <see cref="Tick"/>: accumulates <c>deltaTime</c>.
    ///      When elapsed ≥ current phase's duration, <see cref="AdvancePhase"/> is called:
    ///        a. Increments <see cref="CurrentPhase"/>.
    ///        b. Resets the elapsed timer.
    ///        c. If all phases are complete → fires <see cref="ArenaPhaseControllerSO.OnAllPhasesComplete"/>.
    ///        d. Otherwise → fires the new current phase's event.
    ///   4. On <see cref="HandleMatchEnded"/>: stops ticking.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics or UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - <see cref="Tick"/> and <see cref="AdvancePhase"/> are public for
    ///     EditMode test driving; Unity's Update calls Tick(Time.deltaTime).
    ///   - DisallowMultipleComponent — one phase controller per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_config</c>          → an ArenaPhaseControllerSO asset.
    ///   2. Assign <c>_onMatchStarted</c>  → shared match-start VoidGameEvent.
    ///   3. Assign <c>_onMatchEnded</c>    → shared match-end VoidGameEvent.
    ///   4. Wire each phase's <c>phaseEvent</c> in the SO to desired arena actions.
    ///   5. Wire <c>_onAllPhasesComplete</c> in the SO to end-of-phase-sequence actions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaPhaseController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("SO that provides the phase list, durations, and optional events.")]
        [SerializeField] private ArenaPhaseControllerSO _config;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _matchRunning;
        private int   _currentPhase;
        private float _elapsed;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _startDelegate;
        private Action _endDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _startDelegate = HandleMatchStarted;
            _endDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Resets to phase 0, fires phase 0's event, and starts the timer.
        /// Wired to <c>_onMatchStarted</c>.
        /// No-op when <c>_config</c> is null or has no phases.
        /// </summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _currentPhase = 0;
            _elapsed      = 0f;

            FireCurrentPhaseEvent();
        }

        /// <summary>
        /// Stops the phase timer.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
        }

        /// <summary>
        /// Advances the phase timer by <paramref name="dt"/> seconds.
        /// Calls <see cref="AdvancePhase"/> when elapsed ≥ current phase's duration.
        /// No-op when the match is not running, <c>_config</c> is null, all phases are
        /// done, or the phase list is empty.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_matchRunning || _config == null || _config.PhaseCount == 0) return;
            if (_currentPhase >= _config.PhaseCount) return;

            _elapsed += dt;

            if (_elapsed >= _config.GetPhaseDuration(_currentPhase))
                AdvancePhase();
        }

        /// <summary>
        /// Advances <see cref="CurrentPhase"/> by one and resets the elapsed timer.
        /// If all phases are complete, raises <see cref="ArenaPhaseControllerSO.OnAllPhasesComplete"/>.
        /// Otherwise fires the new current phase's event.
        /// </summary>
        public void AdvancePhase()
        {
            _currentPhase++;
            _elapsed = 0f;

            if (_currentPhase >= _config.PhaseCount)
                _config.OnAllPhasesComplete?.Raise();
            else
                FireCurrentPhaseEvent();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Index of the currently active phase (0-based).</summary>
        public int CurrentPhase => _currentPhase;

        /// <summary>Seconds elapsed since the current phase started.</summary>
        public float Elapsed => _elapsed;

        /// <summary>The assigned <see cref="ArenaPhaseControllerSO"/>. May be null.</summary>
        public ArenaPhaseControllerSO Config => _config;

        // ── Private helpers ───────────────────────────────────────────────────

        private void FireCurrentPhaseEvent()
        {
            _config?.GetPhaseEvent(_currentPhase)?.Raise();
        }
    }
}
