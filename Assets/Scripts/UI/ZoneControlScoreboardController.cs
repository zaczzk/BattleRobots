using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control live scoreboard HUD.
    /// Subscribes to player-capture and scoreboard-update events to keep the
    /// rank and score display current during a match.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _rankLabel  → "Rank: N / M" (player rank out of total competitors).
    ///   _scoreLabel → "Score: N" (player zone-capture count).
    ///   _panel      → Root panel; hidden when <c>_scoreboardSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one scoreboard panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _scoreboardSO          → ZoneControlScoreboardSO asset.
    ///   2. Assign _onPlayerCaptured      → VoidGameEvent raised on player capture.
    ///   3. Assign _onScoreboardUpdated   → scoreboardSO._onScoreboardUpdated channel.
    ///   4. Assign label / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlScoreboardController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlScoreboardSO _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when the player captures a zone; triggers RecordPlayerCapture.")]
        [SerializeField] private VoidGameEvent _onPlayerCaptured;

        [Tooltip("Wire to ZoneControlScoreboardSO._onScoreboardUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _rankLabel;
        [SerializeField] private Text _scoreLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handlePlayerCapturedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onScoreboardUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onScoreboardUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a player capture in the scoreboard SO and refreshes the HUD.
        /// No-op when <c>_scoreboardSO</c> is null.
        /// </summary>
        public void HandlePlayerCaptured()
        {
            _scoreboardSO?.RecordPlayerCapture();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds rank and score labels from the current scoreboard state.
        /// Hides the panel when <c>_scoreboardSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_scoreboardSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rankLabel != null)
                _rankLabel.text = $"Rank: {_scoreboardSO.PlayerRank} / {_scoreboardSO.TotalCompetitors}";

            if (_scoreLabel != null)
                _scoreLabel.text = $"Score: {_scoreboardSO.PlayerScore}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound scoreboard SO (may be null).</summary>
        public ZoneControlScoreboardSO ScoreboardSO => _scoreboardSO;
    }
}
