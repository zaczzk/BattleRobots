using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that drives a <see cref="MatchPaceSO"/> by subscribing to
    /// configurable event sources and ticking the window timer while the match runs.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. On <c>_onMatchStarted</c>: sets <see cref="IsMatchRunning"/> to true
    ///      and calls <see cref="MatchPaceSO.Reset"/> to clear the window.
    ///   2. Each <c>Update</c>: calls <see cref="MatchPaceSO.Tick"/> while running.
    ///   3. Each entry in <c>_paceEventSources</c> is wired to
    ///      <see cref="IncrementEvent"/> — any of those channels firing counts as
    ///      one match event toward the pace window.
    ///   4. On <c>_onMatchEnded</c>: sets <see cref="IsMatchRunning"/> to false.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI dependencies.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_paceSO</c>            → a MatchPaceSO asset.
    ///   2. Assign <c>_onMatchStarted</c>    → shared match-start VoidGameEvent.
    ///   3. Assign <c>_onMatchEnded</c>      → shared match-end VoidGameEvent.
    ///   4. Populate <c>_paceEventSources</c> with VoidGameEvents whose firing
    ///      should count toward match pace (e.g., _onDamageDealt, _onCapture).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchPaceController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The MatchPaceSO that accumulates event counts and fires pace channels.")]
        [SerializeField] private MatchPaceSO _paceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Pace Event Sources (optional)")]
        [Tooltip("Each VoidGameEvent in this array is subscribed at OnEnable. " +
                 "Any one of them firing counts as one pace event for the current window.")]
        [SerializeField] private VoidGameEvent[] _paceEventSources;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _matchRunning;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _startDelegate;
        private Action _endDelegate;
        private Action _incrementDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _startDelegate     = HandleMatchStarted;
            _endDelegate       = HandleMatchEnded;
            _incrementDelegate = IncrementEvent;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);

            if (_paceEventSources != null)
            {
                foreach (VoidGameEvent src in _paceEventSources)
                    src?.RegisterCallback(_incrementDelegate);
            }
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);

            if (_paceEventSources != null)
            {
                foreach (VoidGameEvent src in _paceEventSources)
                    src?.UnregisterCallback(_incrementDelegate);
            }
        }

        private void Update()
        {
            if (_matchRunning)
                _paceSO?.Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Marks the match as running and resets the pace window.
        /// Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _paceSO?.Reset();
        }

        /// <summary>
        /// Marks the match as no longer running (stops the Tick drive).
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
        }

        /// <summary>
        /// Counts one match event toward the current pace window.
        /// Wired to each entry in <c>_paceEventSources</c>.
        /// Zero allocation.
        /// </summary>
        public void IncrementEvent()
        {
            _paceSO?.IncrementEvent();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchPaceSO"/>. May be null.</summary>
        public MatchPaceSO PaceSO => _paceSO;

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;
    }
}
