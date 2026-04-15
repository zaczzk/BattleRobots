using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that orchestrates the full zone-control match lifecycle:
    /// resets all zone-control state at match start and evaluates the zone objective
    /// at match end, raising a win or loss event.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onMatchStarted fires → HandleMatchStarted():
    ///     • _catalogSO?.ResetAll()       — zones uncaptured.
    ///     • _dominanceSO?.Reset()        — player zone count zeroed.
    ///     • _objectiveSO?.Reset()        — objective flag cleared.
    ///     • _timerSO?.Reset()            — any zone cooldown cleared.
    ///
    ///   _onMatchEnded fires → HandleMatchEnded():
    ///     • _objectiveSO?.Evaluate(_dominanceSO?.PlayerZoneCount ?? 0).
    ///     • If _objectiveSO.IsComplete → _onMatchWon?.Raise().
    ///     • Else                        → _onMatchLost?.Raise().
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one manager per match.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _catalogSO      → ControlZoneCatalogSO (all zone SOs to reset).
    ///   _dominanceSO    → ZoneDominanceSO (zone count source for evaluation).
    ///   _objectiveSO    → ZoneObjectiveSO (win condition definition).
    ///   _timerSO        → ZoneTimerSO (optional cooldown to reset at match start).
    ///   _onMatchStarted → VoidGameEvent raised by MatchManager at match start.
    ///   _onMatchEnded   → VoidGameEvent raised by MatchManager at match end.
    ///   _onMatchWon     → VoidGameEvent raised when objective is complete.
    ///   _onMatchLost    → VoidGameEvent raised when objective is not complete.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchManager : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Catalog of all ControlZoneSOs. ResetAll() is called at match start.")]
        [SerializeField] private ControlZoneCatalogSO _catalogSO;

        [Tooltip("Zone dominance SO. Reset() at match start; PlayerZoneCount read at match end.")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        [Tooltip("Zone objective SO. Reset() at match start; Evaluate() at match end.")]
        [SerializeField] private ZoneObjectiveSO _objectiveSO;

        [Tooltip("Zone timer SO. Reset() at match start to clear any leftover cooldown.")]
        [SerializeField] private ZoneTimerSO _timerSO;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by MatchManager when the match begins. Resets zone-control state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised by MatchManager when the match ends. Evaluates the zone objective.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channels — Out (optional)")]
        [Tooltip("Raised when the player meets the zone objective at match end.")]
        [SerializeField] private VoidGameEvent _onMatchWon;

        [Tooltip("Raised when the player does NOT meet the zone objective at match end.")]
        [SerializeField] private VoidGameEvent _onMatchLost;

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

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Resets all zone-control state so the match starts clean.
        /// Null-safe on every data ref.
        /// </summary>
        public void HandleMatchStarted()
        {
            _catalogSO?.ResetAll();
            _dominanceSO?.Reset();
            _objectiveSO?.Reset();
            _timerSO?.Reset();
        }

        /// <summary>
        /// Evaluates the zone objective using the current player zone count,
        /// then fires <see cref="_onMatchWon"/> or <see cref="_onMatchLost"/>.
        /// Null-safe: a null _objectiveSO raises _onMatchLost.
        /// </summary>
        public void HandleMatchEnded()
        {
            int playerCount = _dominanceSO != null ? _dominanceSO.PlayerZoneCount : 0;
            _objectiveSO?.Evaluate(playerCount);

            bool won = _objectiveSO != null && _objectiveSO.IsComplete;
            if (won)
                _onMatchWon?.Raise();
            else
                _onMatchLost?.Raise();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ControlZoneCatalogSO"/>. May be null.</summary>
        public ControlZoneCatalogSO CatalogSO => _catalogSO;

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;

        /// <summary>The assigned <see cref="ZoneObjectiveSO"/>. May be null.</summary>
        public ZoneObjectiveSO ObjectiveSO => _objectiveSO;

        /// <summary>The assigned <see cref="ZoneTimerSO"/>. May be null.</summary>
        public ZoneTimerSO TimerSO => _timerSO;
    }
}
