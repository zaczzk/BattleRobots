using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that bridges <see cref="ZoneControlScoreboardSO"/> to
    /// <see cref="ZoneControlMatchPressureSO"/>, reading whether bots lead the
    /// player after each scoreboard update and adjusting the pressure SO accordingly.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Subscribes to <c>_onScoreboardUpdated</c>.
    ///   • On each event: reads <c>_scoreboardSO.PlayerRank &gt; 1</c> to determine
    ///     whether bots lead; calls <c>_pressureSO.EvaluatePressure(botLeads)</c>.
    ///   • Subscribes to <c>_onMatchStarted</c> to reset the pressure SO.
    ///   • No-op when any required SO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one pressure controller per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _pressureSO          → ZoneControlMatchPressureSO asset.
    ///   2. Assign _scoreboardSO        → ZoneControlScoreboardSO asset.
    ///   3. Assign _onScoreboardUpdated → ZoneControlScoreboardSO._onScoreboardUpdated.
    ///   4. Assign _onMatchStarted      → shared MatchStarted VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchPressureController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchPressureSO _pressureSO;
        [SerializeField] private ZoneControlScoreboardSO    _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlScoreboardSO._onScoreboardUpdated.")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;

        [Tooltip("Raised at match start; resets the pressure SO state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleScoreboardDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleScoreboardDelegate    = HandleScoreboardUpdated;
            _handleMatchStartedDelegate  = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onScoreboardUpdated?.RegisterCallback(_handleScoreboardDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onScoreboardUpdated?.UnregisterCallback(_handleScoreboardDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current player rank from the scoreboard and evaluates pressure.
        /// No-op when either SO is null.
        /// </summary>
        public void HandleScoreboardUpdated()
        {
            if (_pressureSO == null || _scoreboardSO == null) return;

            bool botLeads = _scoreboardSO.PlayerRank > 1;
            _pressureSO.EvaluatePressure(botLeads);
        }

        /// <summary>
        /// Resets the pressure SO at match start.
        /// No-op when <c>_pressureSO</c> is null.
        /// </summary>
        public void HandleMatchStarted()
        {
            _pressureSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound pressure SO (may be null).</summary>
        public ZoneControlMatchPressureSO PressureSO => _pressureSO;

        /// <summary>The bound scoreboard SO (may be null).</summary>
        public ZoneControlScoreboardSO ScoreboardSO => _scoreboardSO;
    }
}
