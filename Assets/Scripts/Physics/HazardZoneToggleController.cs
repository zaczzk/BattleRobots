using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Enables or disables a <see cref="HazardZoneController"/> at match start and end,
    /// with optional timed activation (activate after N seconds into the match).
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • If <c>_timedEnable</c> is false: sets <see cref="HazardZoneController.IsActive"/>
    ///     to <c>_enableOnMatchStart</c> immediately when <c>_onMatchStarted</c> fires;
    ///     raises <c>_onHazardActivated</c> when IsActive becomes true.
    ///   • If <c>_timedEnable</c> is true: keeps the hazard inactive at match start, then
    ///     activates it (and fires <c>_onHazardActivated</c>) once <c>_enableDelay</c>
    ///     seconds have elapsed via <see cref="Tick"/>.
    ///   • On match end: always deactivates the hazard regardless of mode.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — references HazardZoneController.
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - <see cref="Tick"/> is public for EditMode test driving; Unity's
    ///     <c>Update</c> calls <c>Tick(Time.deltaTime)</c> — zero allocation.
    ///   - DisallowMultipleComponent — one toggle controller per hazard.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_hazardZone</c> → the HazardZoneController to toggle.
    ///   2. Assign <c>_onMatchStarted</c> / <c>_onMatchEnded</c> event channels.
    ///   3. Set <c>_enableOnMatchStart</c>; optionally enable <c>_timedEnable</c>
    ///      and configure <c>_enableDelay</c> for deferred activation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardZoneToggleController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Target (optional)")]
        [Tooltip("HazardZoneController whose IsActive flag this controller drives.")]
        [SerializeField] private HazardZoneController _hazardZone;

        [Header("Config")]
        [Tooltip("When true the hazard is activated at match start (or after a delay if " +
                 "_timedEnable is set). When false the hazard is deactivated at match start.")]
        [SerializeField] private bool _enableOnMatchStart = true;

        [Tooltip("When true the hazard activation is deferred by _enableDelay seconds " +
                 "rather than activating immediately at match start.")]
        [SerializeField] private bool _timedEnable = false;

        [Tooltip("Seconds after match start before the hazard activates. " +
                 "Only used when _timedEnable is true.")]
        [SerializeField, Min(0f)] private float _enableDelay = 10f;

        [Header("Event Channels — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager when the match starts.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("VoidGameEvent raised by MatchManager when the match ends.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channel — Out (optional)")]
        [Tooltip("Raised when the hazard zone transitions to active " +
                 "(immediately at match start or after the timed delay).")]
        [SerializeField] private VoidGameEvent _onHazardActivated;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _matchRunning;
        private float _elapsed;
        private bool  _timedActivated;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _matchStartDelegate;
        private Action _matchEndDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchStartDelegate = HandleMatchStarted;
            _matchEndDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_matchStartDelegate);
            _onMatchEnded?.RegisterCallback(_matchEndDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_matchStartDelegate);
            _onMatchEnded?.UnregisterCallback(_matchEndDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the timed-enable countdown by <paramref name="deltaTime"/> seconds.
        /// No-op when <c>_timedEnable</c> is false, the match is not running, or the
        /// hazard has already been timed-activated this match.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_timedEnable || !_matchRunning || _timedActivated) return;

            _elapsed += deltaTime;

            if (_elapsed >= _enableDelay)
            {
                _timedActivated = true;
                if (_hazardZone != null)
                    _hazardZone.IsActive = _enableOnMatchStart;
                if (_enableOnMatchStart)
                    _onHazardActivated?.Raise();
            }
        }

        /// <summary>
        /// Sets hazard state at match start. When <c>_timedEnable</c> is false the
        /// hazard is toggled immediately; otherwise it stays inactive until <see cref="Tick"/>
        /// accumulates <c>_enableDelay</c> seconds.
        /// </summary>
        public void HandleMatchStarted()
        {
            _matchRunning   = true;
            _elapsed        = 0f;
            _timedActivated = false;

            if (_timedEnable)
            {
                // Hazard stays inactive until the delay elapses via Tick.
                if (_hazardZone != null)
                    _hazardZone.IsActive = false;
            }
            else
            {
                if (_hazardZone != null)
                    _hazardZone.IsActive = _enableOnMatchStart;
                if (_enableOnMatchStart)
                    _onHazardActivated?.Raise();
            }
        }

        /// <summary>
        /// Deactivates the hazard and stops the timed-enable countdown.
        /// </summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
            if (_hazardZone != null)
                _hazardZone.IsActive = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="HazardZoneController"/>. May be null.</summary>
        public HazardZoneController HazardZone => _hazardZone;

        /// <summary>Whether the hazard should be enabled at match start.</summary>
        public bool EnableOnMatchStart => _enableOnMatchStart;

        /// <summary>Whether timed activation is enabled.</summary>
        public bool TimedEnable => _timedEnable;

        /// <summary>Seconds after match start before timed activation fires.</summary>
        public float EnableDelay => _enableDelay;

        /// <summary>True while a match is running.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Seconds elapsed since the last match started (timed-enable timer).</summary>
        public float Elapsed => _elapsed;

        /// <summary>True once the timed-enable delay has fired in the current match.</summary>
        public bool IsTimedActivated => _timedActivated;
    }
}
