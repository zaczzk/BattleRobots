using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that evaluates the active <see cref="ZoneControlVictoryConditionSO"/>
    /// after each score update and declares victory exactly once per match.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onScoreUpdated</c> → <see cref="HandleScoreUpdated"/> which
    ///   reads <c>ZoneControlScoreboardSO.PlayerScore</c> and the accumulated elapsed
    ///   time to call <see cref="ZoneControlVictoryConditionSO.IsVictoryMet"/>.
    ///   On the first true result the SO's <c>RaiseVictory()</c> is called and
    ///   <c>_onVictoryAchieved</c> is raised; subsequent calls are ignored until the
    ///   match resets via <c>_onMatchStarted</c>.
    ///   <c>Update</c> accumulates <c>_matchElapsedTime</c> while victory is pending.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one evaluator per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _victorySO    → ZoneControlVictoryConditionSO asset.
    ///   2. Assign _scoreboardSO → ZoneControlScoreboardSO asset.
    ///   3. Assign _onScoreUpdated  → shared score-updated VoidGameEvent.
    ///   4. Assign _onMatchStarted  → shared match-started VoidGameEvent.
    ///   5. Assign _onVictoryAchieved → shared victory VoidGameEvent (out).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlVictoryEvaluatorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlVictoryConditionSO _victorySO;
        [SerializeField] private ZoneControlScoreboardSO       _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onScoreUpdated;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("Event Channels — Out (optional)")]
        [SerializeField] private VoidGameEvent _onVictoryAchieved;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isVictoryAchieved;
        private float _matchElapsedTime;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleScoreUpdatedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleScoreUpdatedDelegate = HandleScoreUpdated;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onScoreUpdated?.RegisterCallback(_handleScoreUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onScoreUpdated?.UnregisterCallback(_handleScoreUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        private void Update()
        {
            if (!_isVictoryAchieved)
                _matchElapsedTime += Time.deltaTime;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the victory condition against the current scoreboard and elapsed time.
        /// Fires victory events exactly once; subsequent calls are no-ops until
        /// <see cref="HandleMatchStarted"/> resets the flag.
        /// </summary>
        public void HandleScoreUpdated()
        {
            if (_isVictoryAchieved || _victorySO == null) return;

            int playerCaptures = _scoreboardSO != null ? _scoreboardSO.PlayerScore : 0;

            if (!_victorySO.IsVictoryMet(playerCaptures, _matchElapsedTime)) return;

            _isVictoryAchieved = true;
            _victorySO.RaiseVictory();
            _onVictoryAchieved?.Raise();
        }

        /// <summary>Resets victory state and elapsed time for a new match.</summary>
        public void HandleMatchStarted()
        {
            _isVictoryAchieved = false;
            _matchElapsedTime  = 0f;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True once victory has been declared this match.</summary>
        public bool IsVictoryAchieved => _isVictoryAchieved;

        /// <summary>The bound victory condition SO (may be null).</summary>
        public ZoneControlVictoryConditionSO VictorySO => _victorySO;
    }
}
