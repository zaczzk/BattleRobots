using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Physics MonoBehaviour that drives a <see cref="ControlZoneSO"/> using Unity's
    /// trigger collider system.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. On <c>_onMatchStarted</c>: resets the zone, clears occupancy and timer.
    ///   2. <c>OnTriggerEnter</c>: marks the zone as occupied (first collider wins).
    ///   3. <c>OnTriggerExit</c>: clears occupancy and calls <see cref="ControlZoneSO.Lose"/>.
    ///   4. Each <see cref="Tick"/>:
    ///        • While occupied: calls <see cref="ControlZoneSO.CaptureProgress"/>.
    ///        • While not occupied AND zone is captured: calls <see cref="ControlZoneSO.Lose"/>.
    ///        • Accumulates <c>_tickElapsed</c>; calls <see cref="ControlZoneSO.ScoreTick"/>
    ///          once per <c>_tickInterval</c> seconds.
    ///   5. On <c>_onMatchEnded</c>: resets the zone and clears occupancy state.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — owns the trigger collider logic.
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - <see cref="Tick"/> is public for EditMode test driving.
    ///   - DisallowMultipleComponent — one controller per zone collider.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_zone</c>          → a ControlZoneSO asset.
    ///   2. Assign <c>_onMatchStarted</c> → shared match-start VoidGameEvent.
    ///   3. Assign <c>_onMatchEnded</c>   → shared match-end VoidGameEvent.
    ///   4. Add a Trigger Collider to this GameObject or a child.
    ///   5. Optionally adjust <c>_tickInterval</c> for score cadence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ControlZoneController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The ControlZoneSO that owns capture state and event channels.")]
        [SerializeField] private ControlZoneSO _zone;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Score Tick Settings")]
        [Tooltip("Interval in seconds between score-tick events while the zone is captured.")]
        [SerializeField, Min(0.1f)] private float _tickInterval = 1f;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _matchRunning;
        private bool  _isOccupied;
        private float _tickElapsed;

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

        // ── Trigger events ────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!_matchRunning) return;
            _isOccupied = true;
        }

        private void OnTriggerExit(Collider other)
        {
            _isOccupied = false;
            _zone?.Lose();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Resets the zone, clears occupancy, and marks the match as running.
        /// Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _isOccupied   = false;
            _tickElapsed  = 0f;
            _zone?.Reset();
        }

        /// <summary>
        /// Resets the zone, clears occupancy, and stops the match.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
            _isOccupied   = false;
            _zone?.Reset();
        }

        /// <summary>
        /// Advances capture progress, handles zone loss on vacancy, and fires score ticks.
        /// Called from <c>Update</c>; public for EditMode test driving.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_matchRunning) return;

            if (_isOccupied)
            {
                _zone?.CaptureProgress(dt);
            }
            else if (_zone != null && _zone.IsCaptured)
            {
                _zone.Lose();
            }

            _tickElapsed += dt;
            if (_tickElapsed >= _tickInterval)
            {
                _zone?.ScoreTick();
                _tickElapsed = 0f;
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ControlZoneSO"/>. May be null.</summary>
        public ControlZoneSO Zone => _zone;

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Score tick interval in seconds.</summary>
        public float TickInterval => _tickInterval;

        /// <summary>True when a trigger collider is currently inside the zone.</summary>
        public bool IsOccupied => _isOccupied;
    }
}
