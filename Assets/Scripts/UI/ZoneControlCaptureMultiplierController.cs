using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMultiplierController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMultiplierSO _multiplierSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Config")]
        [SerializeField, Min(0)] private int _baseRewardPerCapture = 50;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMultiplierUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _multiplierLabel;
        [SerializeField] private Text       _totalEarnedLabel;
        [SerializeField] private GameObject _panel;

        private int _totalEarned;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMultiplierUpdatedDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate      = HandleZoneCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleMultiplierUpdatedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMultiplierUpdated?.RegisterCallback(_handleMultiplierUpdatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMultiplierUpdated?.UnregisterCallback(_handleMultiplierUpdatedDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_multiplierSO == null) return;
            int reward = _multiplierSO.RewardForCapture(_baseRewardPerCapture);
            _multiplierSO.RecordCapture();
            _wallet?.AddFunds(reward);
            _totalEarned += reward;
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _multiplierSO?.Reset();
            _totalEarned = 0;
            Refresh();
        }

        public void Refresh()
        {
            if (_multiplierSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_multiplierLabel != null)
                _multiplierLabel.text = $"Multiplier: x{_multiplierSO.CurrentMultiplier:F1}";

            if (_totalEarnedLabel != null)
                _totalEarnedLabel.text = $"Total Earned: {_totalEarned}";
        }

        public ZoneControlCaptureMultiplierSO MultiplierSO => _multiplierSO;
    }
}
