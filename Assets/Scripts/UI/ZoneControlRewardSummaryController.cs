using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records per-match rewards into
    /// <see cref="ZoneControlRewardSummarySO"/> and renders session-wide stats.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _totalLabel    → "Total: N"
    ///   _averageLabel  → "Average: N"
    ///   _bestLabel     → "Best: N"
    ///   _panel         → Root panel; hidden when _summarySO is null.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onMatchEnded</c> → <see cref="HandleMatchEnded"/> which reads
    ///   <c>_rewardHistorySO.GetAverageReward()</c> and passes the rounded value to
    ///   <see cref="ZoneControlRewardSummarySO.AddMatchReward"/>.
    ///   Subscribes <c>_onSummaryUpdated</c> → <see cref="Refresh"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one reward summary HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlRewardSummaryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRewardSummarySO _summarySO;
        [SerializeField] private ZoneControlRewardHistorySO _rewardHistorySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers reward recording.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneControlRewardSummarySO._onSummaryUpdated.")]
        [SerializeField] private VoidGameEvent _onSummaryUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private Text       _averageLabel;
        [SerializeField] private Text       _bestLabel;
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
            _onSummaryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onSummaryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records this match's rolling-average reward into the summary SO.
        /// Uses <c>_rewardHistorySO.GetAverageReward()</c> when available; falls back to 0.
        /// </summary>
        public void HandleMatchEnded()
        {
            int reward = _rewardHistorySO != null
                ? Mathf.RoundToInt(_rewardHistorySO.GetAverageReward())
                : 0;
            _summarySO?.AddMatchReward(reward);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the summary labels from the current SO state.
        /// Hides the panel when <c>_summarySO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_summarySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_totalLabel != null)
                _totalLabel.text = $"Total: {_summarySO.TotalReward}";

            if (_averageLabel != null)
                _averageLabel.text = $"Average: {_summarySO.AverageReward:F0}";

            if (_bestLabel != null)
                _bestLabel.text = $"Best: {_summarySO.BestMatchReward}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound reward summary SO (may be null).</summary>
        public ZoneControlRewardSummarySO SummarySO => _summarySO;
    }
}
