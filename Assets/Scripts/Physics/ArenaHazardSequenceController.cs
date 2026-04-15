using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Cycles through an array of <see cref="HazardZoneController"/> instances in a fixed
    /// sequence, keeping only one active at a time for a configurable duration before
    /// advancing to the next.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. On <c>_onMatchStarted</c>: deactivates all hazards, activates <c>_hazards[0]</c>,
    ///      resets the elapsed timer, and sets <see cref="CurrentIndex"/> to 0.
    ///   2. Each <see cref="Tick"/>: accumulates <c>deltaTime</c>.
    ///      When elapsed ≥ <see cref="ArenaHazardSequenceSO.CycleDuration"/>,
    ///      <see cref="AdvanceSequence"/> is called:
    ///        a. Deactivates the current hazard.
    ///        b. Increments <see cref="CurrentIndex"/> (wraps around to 0).
    ///        c. Resets the elapsed timer.
    ///        d. Activates the new current hazard.
    ///        e. Raises <see cref="ArenaHazardSequenceSO.OnSequenceAdvanced"/> and
    ///           the local <c>_onSequenceAdvanced</c> channel.
    ///   3. On <c>_onMatchEnded</c>: deactivates all hazards and stops the timer.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — references HazardZoneController.
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - <see cref="Tick"/> and <see cref="AdvanceSequence"/> are public for
    ///     EditMode test driving; Unity's Update calls Tick(Time.deltaTime).
    ///   - DisallowMultipleComponent — one sequence controller per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_config</c>             → an ArenaHazardSequenceSO asset.
    ///   2. Assign <c>_hazards</c>            → the HazardZoneControllers to cycle through.
    ///   3. Assign <c>_onMatchStarted</c>     → shared match-start VoidGameEvent.
    ///   4. Assign <c>_onMatchEnded</c>       → shared match-end VoidGameEvent.
    ///   5. Optionally assign <c>_onSequenceAdvanced</c> → extra per-controller channel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaHazardSequenceController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("SO that provides the cycle duration and optional sequence-advanced event.")]
        [SerializeField] private ArenaHazardSequenceSO _config;

        [Header("Hazards (optional)")]
        [Tooltip("HazardZoneControllers to cycle through in order. " +
                 "The sequence wraps back to index 0 after the last entry.")]
        [SerializeField] private HazardZoneController[] _hazards;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channel — Out (optional)")]
        [Tooltip("Additional 'sequence advanced' channel local to this controller. " +
                 "The config SO also carries its own OnSequenceAdvanced channel.")]
        [SerializeField] private VoidGameEvent _onSequenceAdvanced;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _matchRunning;
        private int   _currentIndex;
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
        /// Deactivates all managed hazards, activates the first one, and starts the timer.
        /// Wired to <c>_onMatchStarted</c>.
        /// No-op when <c>_hazards</c> is null or empty.
        /// </summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _currentIndex = 0;
            _elapsed      = 0f;

            DeactivateAll();
            ActivateCurrent();
        }

        /// <summary>
        /// Deactivates all managed hazards and stops the sequence timer.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
            DeactivateAll();
        }

        /// <summary>
        /// Advances the sequence timer by <paramref name="dt"/> seconds.
        /// Calls <see cref="AdvanceSequence"/> when elapsed ≥ <see cref="ArenaHazardSequenceSO.CycleDuration"/>.
        /// No-op when match is not running, <c>_config</c> is null, or <c>_hazards</c>
        /// is null or empty.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_matchRunning || _config == null) return;
            if (_hazards == null || _hazards.Length == 0) return;

            _elapsed += dt;

            if (_elapsed >= _config.CycleDuration)
                AdvanceSequence();
        }

        /// <summary>
        /// Deactivates the current hazard, advances <see cref="CurrentIndex"/> (with wrap),
        /// resets the elapsed timer, activates the new current hazard, and fires both
        /// sequence-advanced event channels.
        /// No-op when <c>_hazards</c> is null or empty.
        /// </summary>
        public void AdvanceSequence()
        {
            if (_hazards == null || _hazards.Length == 0) return;

            DeactivateCurrent();
            _currentIndex = (_currentIndex + 1) % _hazards.Length;
            _elapsed = 0f;
            ActivateCurrent();

            _config?.OnSequenceAdvanced?.Raise();
            _onSequenceAdvanced?.Raise();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Index of the currently active hazard in <c>_hazards</c>.</summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>Seconds elapsed since the last sequence advance (or match start).</summary>
        public float Elapsed => _elapsed;

        /// <summary>The assigned <see cref="ArenaHazardSequenceSO"/>. May be null.</summary>
        public ArenaHazardSequenceSO Config => _config;

        // ── Private helpers ───────────────────────────────────────────────────

        private void DeactivateAll()
        {
            if (_hazards == null) return;
            foreach (HazardZoneController hazard in _hazards)
            {
                if (hazard != null)
                    hazard.IsActive = false;
            }
        }

        private void ActivateCurrent()
        {
            if (_hazards == null || _hazards.Length == 0) return;
            HazardZoneController current = _hazards[_currentIndex];
            if (current != null)
                current.IsActive = true;
        }

        private void DeactivateCurrent()
        {
            if (_hazards == null || _hazards.Length == 0) return;
            HazardZoneController current = _hazards[_currentIndex];
            if (current != null)
                current.IsActive = false;
        }
    }
}
