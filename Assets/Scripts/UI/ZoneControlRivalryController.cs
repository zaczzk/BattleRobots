using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records the player-vs-bot match result into
    /// <see cref="ZoneControlRivalrySO"/> at match end and displays the
    /// running rivalry standing.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>: reads <c>_scoreboardSO</c> to determine the
    ///   winner, calls <see cref="ZoneControlRivalrySO.RecordResult"/>, then
    ///   refreshes the display.
    ///   On <c>_onRivalryUpdated</c>: refreshes the display.
    ///   <see cref="Refresh"/> sets the rivalry label and rivalry bar value.
    ///   Panel is hidden when <c>_rivalrySO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one rivalry controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlRivalryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRivalrySO    _rivalrySO;
        [SerializeField] private ZoneControlScoreboardSO _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onRivalryUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _rivalryLabel;
        [SerializeField] private Slider     _rivalryBar;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onRivalryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onRivalryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Determines the match winner from the scoreboard and records the result.
        /// Falls back to a bot win when the scoreboard is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_rivalrySO == null) { Refresh(); return; }

            bool playerWon = _scoreboardSO != null
                             && _scoreboardSO.PlayerScore > _scoreboardSO.GetBotScore(0);
            _rivalrySO.RecordResult(playerWon);
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the rivalry label and bar.
        /// Hides the panel when <c>_rivalrySO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_rivalrySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rivalryLabel != null)
                _rivalryLabel.text = _rivalrySO.RivalryDescription();

            if (_rivalryBar != null)
            {
                int total         = _rivalrySO.PlayerWins + _rivalrySO.BotWins;
                _rivalryBar.value = total > 0
                    ? (float)_rivalrySO.PlayerWins / total
                    : 0.5f;
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound rivalry SO (may be null).</summary>
        public ZoneControlRivalrySO RivalrySO => _rivalrySO;
    }
}
