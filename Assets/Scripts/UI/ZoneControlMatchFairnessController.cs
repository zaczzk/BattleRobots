using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlMatchFairnessSO"/> and
    /// displays the catch-up state to the player.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <c>_onScoreboardUpdated</c>: reads player/bot scores from
    ///   <c>_scoreboardSO</c>, calls <c>EvaluateFairness</c>, refreshes.
    ///   <c>_onMatchStarted</c>: resets fairness SO and refreshes.
    ///   <c>_onCatchUpActivated/_onCatchUpDeactivated</c>: refreshes display.
    ///   <see cref="Refresh"/>: shows "Catch-Up Active!" or "Fair Match" and
    ///   the current bonus amount; hides panel when <c>_fairnessSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchFairnessController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchFairnessSO _fairnessSO;
        [SerializeField] private ZoneControlScoreboardSO    _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCatchUpActivated;
        [SerializeField] private VoidGameEvent _onCatchUpDeactivated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleScoreboardUpdatedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleScoreboardUpdatedDelegate = HandleScoreboardUpdated;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _refreshDelegate                 = Refresh;
        }

        private void OnEnable()
        {
            _onScoreboardUpdated?.RegisterCallback(_handleScoreboardUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCatchUpActivated?.RegisterCallback(_refreshDelegate);
            _onCatchUpDeactivated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onScoreboardUpdated?.UnregisterCallback(_handleScoreboardUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCatchUpActivated?.UnregisterCallback(_refreshDelegate);
            _onCatchUpDeactivated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleScoreboardUpdated()
        {
            if (_fairnessSO == null || _scoreboardSO == null) return;
            int playerScore = _scoreboardSO.PlayerScore;
            int botScore    = _scoreboardSO.GetBotScore(0);
            _fairnessSO.EvaluateFairness(playerScore, botScore);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fairnessSO?.Reset();
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>Updates the fairness status panel.</summary>
        public void Refresh()
        {
            if (_fairnessSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _fairnessSO.IsCatchUpActive ? "Catch-Up Active!" : "Fair Match";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_fairnessSO.GetCatchUpBonus()}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        public ZoneControlMatchFairnessSO FairnessSO   => _fairnessSO;
        public ZoneControlScoreboardSO    ScoreboardSO => _scoreboardSO;
    }
}
