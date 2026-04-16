using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that drives <see cref="ZoneControlMatchPressureSO"/> by
    /// reading the current player rank from <see cref="ZoneControlScoreboardSO"/>
    /// and advancing the pressure simulation each frame.
    ///
    /// ── Flow ────────────────────────────────────────────────────────────────────
    ///   • <c>_onScoreboardUpdated</c> → reads PlayerRank; sets <c>_botIsLeading</c>
    ///     true when rank > 1 (at least one bot outscores the player).
    ///   • <c>Update</c>               → calls Tick(dt, _botIsLeading) while running.
    ///   • <c>_onMatchStarted</c>      → resets pressure SO and arms the timer.
    ///   • <c>_onMatchEnded</c>        → disarms the timer.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one pressure driver per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _pressureSO          → ZoneControlMatchPressureSO asset.
    ///   2. Assign _scoreboardSO        → ZoneControlScoreboardSO asset.
    ///   3. Assign _onScoreboardUpdated → VoidGameEvent raised on any score change.
    ///   4. Assign _onMatchStarted / _onMatchEnded channels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchPressureController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchPressureSO _pressureSO;
        [SerializeField] private ZoneControlScoreboardSO    _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised on any score update; triggers bot-leading re-evaluation.")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;

        [Tooltip("Raised at match start; resets pressure and arms the tick timer.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised at match end; disarms the tick timer.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleScoreboardUpdatedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _botIsLeading;
        private bool _isRunning;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleScoreboardUpdatedDelegate = HandleScoreboardUpdated;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleMatchEndedDelegate        = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onScoreboardUpdated?.RegisterCallback(_handleScoreboardUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onScoreboardUpdated?.UnregisterCallback(_handleScoreboardUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning) return;
            _pressureSO?.Tick(Time.deltaTime, _botIsLeading);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Re-evaluates bot-leading status from the current scoreboard rank.
        /// A player rank above 1 means at least one bot is outscoring the player.
        /// </summary>
        public void HandleScoreboardUpdated()
        {
            _botIsLeading = _scoreboardSO != null && _scoreboardSO.PlayerRank > 1;
        }

        /// <summary>Resets pressure, clears bot-leading flag, and arms the tick timer.</summary>
        public void HandleMatchStarted()
        {
            _botIsLeading = false;
            _pressureSO?.Reset();
            _isRunning = true;
        }

        /// <summary>Disarms the tick timer at match end.</summary>
        public void HandleMatchEnded()
        {
            _isRunning = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the pressure tick loop is active.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>True when the scoreboard indicates at least one bot leads the player.</summary>
        public bool BotIsLeading => _botIsLeading;

        /// <summary>The bound pressure SO (may be null).</summary>
        public ZoneControlMatchPressureSO PressureSO => _pressureSO;
    }
}
