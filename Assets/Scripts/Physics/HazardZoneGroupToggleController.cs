using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Automatically toggles a <see cref="HazardZoneGroupSO"/> at a fixed interval
    /// during a match, flipping the group between active and inactive every
    /// <see cref="ToggleInterval"/> seconds.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. Subscribes to <c>_onMatchStarted</c> / <c>_onMatchEnded</c> in OnEnable.
    ///   2. On <see cref="HandleMatchStarted"/>: sets <c>_matchRunning=true</c> and
    ///      resets the elapsed timer.
    ///   3. Each <see cref="Tick"/>: accumulates <c>deltaTime</c>; once elapsed ≥
    ///      <see cref="ToggleInterval"/>, calls <see cref="HazardZoneGroupSO.Toggle"/>,
    ///      resets the timer, and fires <c>_onGroupToggled</c>.
    ///   4. On <see cref="HandleMatchEnded"/>: stops ticking.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — uses HazardZoneGroupSO (Core).
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - <see cref="Tick"/> is public for EditMode test driving.
    ///   - DisallowMultipleComponent — one toggle controller per group.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_group</c>          → a HazardZoneGroupSO asset.
    ///   2. Assign <c>_onMatchStarted</c> → shared match-start VoidGameEvent.
    ///   3. Assign <c>_onMatchEnded</c>   → shared match-end VoidGameEvent.
    ///   4. Optionally assign <c>_onGroupToggled</c> → per-toggle notification channel.
    ///   5. Tune <c>_toggleInterval</c>   → seconds between each group flip.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardZoneGroupToggleController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The HazardZoneGroupSO to toggle on each interval.")]
        [SerializeField] private HazardZoneGroupSO _group;

        [Header("Toggle Settings")]
        [Tooltip("Seconds between each group toggle. Minimum 1 second.")]
        [SerializeField, Min(1f)] private float _toggleInterval = 5f;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channel — Out (optional)")]
        [Tooltip("Fired each time the group is toggled.")]
        [SerializeField] private VoidGameEvent _onGroupToggled;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _matchRunning;
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
        /// Marks the match as running and resets the elapsed timer.
        /// Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _elapsed      = 0f;
        }

        /// <summary>
        /// Stops the toggle timer.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
        }

        /// <summary>
        /// Advances the toggle timer by <paramref name="dt"/> seconds.
        /// When elapsed ≥ <see cref="ToggleInterval"/>, calls
        /// <see cref="HazardZoneGroupSO.Toggle"/>, resets the timer, and fires
        /// <c>_onGroupToggled</c>.
        /// No-op when match is not running or <c>_group</c> is null.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_matchRunning || _group == null) return;

            _elapsed += dt;

            if (_elapsed >= _toggleInterval)
            {
                _group.Toggle();
                _elapsed = 0f;
                _onGroupToggled?.Raise();
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Seconds elapsed since the last toggle (or match start).</summary>
        public float Elapsed => _elapsed;

        /// <summary>Seconds between each group toggle.</summary>
        public float ToggleInterval => _toggleInterval;

        /// <summary>The assigned <see cref="HazardZoneGroupSO"/>. May be null.</summary>
        public HazardZoneGroupSO Group => _group;
    }
}
