using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates the tie-break condition at match end and
    /// shows a tie-break panel when scores are equal.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>: reads <c>ZoneControlScoreboardSO.PlayerScore</c> and
    ///   <c>GetBotScore(0)</c>, calls <see cref="ZoneControlTieBreakSO.EvaluateTie"/>,
    ///   then calls <see cref="Refresh"/>.
    ///   On <c>_onTieBreakTriggered</c>: calls <see cref="Refresh"/> to update the HUD.
    ///   On <c>_onMatchStarted</c>: resets the SO and hides the panel.
    ///   <see cref="Refresh"/>: hides panel when SO is null or not active; shows panel
    ///   and sets the label to <c>TieBreakDescription</c> when a tie is active.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one tie-break controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _tieBreakSO   → ZoneControlTieBreakSO asset.
    ///   2. Assign _scoreboardSO → ZoneControlScoreboardSO asset.
    ///   3. Assign _onMatchEnded → shared MatchEnded VoidGameEvent.
    ///   4. Assign _tieBreakPanel / _tieBreakLabel → HUD elements.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlTieBreakController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlTieBreakSO    _tieBreakSO;
        [SerializeField] private ZoneControlScoreboardSO  _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onTieBreakTriggered;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tieBreakLabel;
        [SerializeField] private GameObject _tieBreakPanel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _handleTieBreakTriggeredDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate       = HandleMatchEnded;
            _handleTieBreakTriggeredDelegate = Refresh;
            _handleMatchStartedDelegate      = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onTieBreakTriggered?.RegisterCallback(_handleTieBreakTriggeredDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onTieBreakTriggered?.UnregisterCallback(_handleTieBreakTriggeredDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether the current scores are tied and refreshes the display.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_tieBreakSO != null)
            {
                int playerScore = _scoreboardSO != null ? _scoreboardSO.PlayerScore : 0;
                int botScore    = _scoreboardSO != null ? _scoreboardSO.GetBotScore(0) : 0;
                _tieBreakSO.EvaluateTie(playerScore, botScore);
            }
            Refresh();
        }

        /// <summary>Resets tie-break state and hides the panel at match start.</summary>
        public void HandleMatchStarted()
        {
            _tieBreakSO?.Reset();
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the tie-break panel visibility and label.
        /// Hides the panel when the SO is null or no tie is active.
        /// </summary>
        public void Refresh()
        {
            if (_tieBreakSO == null || !_tieBreakSO.IsActive)
            {
                _tieBreakPanel?.SetActive(false);
                return;
            }

            _tieBreakPanel?.SetActive(true);

            if (_tieBreakLabel != null)
                _tieBreakLabel.text = _tieBreakSO.TieBreakDescription;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound tie-break SO (may be null).</summary>
        public ZoneControlTieBreakSO TieBreakSO => _tieBreakSO;
    }
}
