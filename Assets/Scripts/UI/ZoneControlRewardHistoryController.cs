using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records per-match rewards into
    /// <see cref="ZoneControlRewardHistorySO"/> and renders a bar chart.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _rewardBars    → Image[] bar chart; fillAmount = reward / _maxDisplayReward.
    ///   _averageLabel  → "Avg Reward: N".
    ///   _panel         → Root panel; hidden when _rewardHistorySO is null.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onMatchEnded</c> → <see cref="HandleMatchEnded"/> which reads
    ///   <c>_captureBonusSO.TotalBonusAwarded</c> and records via
    ///   <see cref="ZoneControlRewardHistorySO.AddReward"/>.
    ///   Subscribes <c>_onHistoryUpdated</c> → <see cref="Refresh"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one reward history HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlRewardHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRewardHistorySO _rewardHistorySO;
        [SerializeField] private ZoneControlCaptureBonusSO  _captureBonusSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers reward recording.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneControlRewardHistorySO._onHistoryUpdated.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("UI Refs (optional)")]
        [Tooltip("Bar images; fillAmount driven by reward / maxDisplayReward.")]
        [SerializeField] private Image[] _rewardBars;
        [SerializeField] private Text    _averageLabel;

        [Tooltip("Reward value that corresponds to a full bar (fillAmount = 1).")]
        [SerializeField] private float   _maxDisplayReward = 1000f;

        [Header("UI Refs — Panel (optional)")]
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
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records this match's bonus reward into the history SO and refreshes the HUD.
        /// Uses <c>_captureBonusSO.TotalBonusAwarded</c> when available; falls back to 0.
        /// </summary>
        public void HandleMatchEnded()
        {
            int reward = _captureBonusSO != null ? _captureBonusSO.TotalBonusAwarded : 0;
            _rewardHistorySO?.AddReward(reward);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the bar chart and average label from the current history.
        /// Hides the panel when <c>_rewardHistorySO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_rewardHistorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            float max = Mathf.Max(1f, _maxDisplayReward);

            if (_rewardBars != null)
            {
                for (int i = 0; i < _rewardBars.Length; i++)
                {
                    if (_rewardBars[i] == null) continue;
                    float reward = i < _rewardHistorySO.EntryCount
                        ? _rewardHistorySO.GetReward(i)
                        : 0f;
                    _rewardBars[i].fillAmount = Mathf.Clamp01(reward / max);
                }
            }

            if (_averageLabel != null)
                _averageLabel.text =
                    $"Avg Reward: {Mathf.RoundToInt(_rewardHistorySO.GetAverageReward())}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound reward history SO (may be null).</summary>
        public ZoneControlRewardHistorySO RewardHistorySO => _rewardHistorySO;
    }
}
