using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlAdaptiveRewardController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAdaptiveRewardSO    _rewardSO;
        [SerializeField] private ZoneControlCaptureEfficiencySO _efficiencySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onScaleChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _scaleLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onScaleChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onScaleChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchEnded()
        {
            if (_rewardSO == null) return;
            float efficiency = _efficiencySO != null ? _efficiencySO.Efficiency : 0f;
            _rewardSO.SetPerformanceRatio(efficiency);
            Refresh();
        }

        public void Refresh()
        {
            if (_rewardSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_scaleLabel != null)
                _scaleLabel.text = $"Reward Scale: {_rewardSO.CurrentScaleFactor:F2}x";
        }

        public ZoneControlAdaptiveRewardSO RewardSO => _rewardSO;
    }
}
