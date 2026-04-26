using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLinearLogicController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLinearLogicSO _linearLogicSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLinearLogicCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _resourceConsumptionLabel;
        [SerializeField] private Text       _consumptionCountLabel;
        [SerializeField] private Slider     _resourceConsumptionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLinearLogicCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLinearLogicCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_linearLogicSO == null) return;
            int bonus = _linearLogicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_linearLogicSO == null) return;
            _linearLogicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _linearLogicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_linearLogicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_resourceConsumptionLabel != null)
                _resourceConsumptionLabel.text =
                    $"Resource Consumptions: {_linearLogicSO.ResourceConsumptions}/{_linearLogicSO.ResourceConsumptionsNeeded}";

            if (_consumptionCountLabel != null)
                _consumptionCountLabel.text = $"Consumptions: {_linearLogicSO.ConsumptionCount}";

            if (_resourceConsumptionBar != null)
                _resourceConsumptionBar.value = _linearLogicSO.ResourceConsumptionProgress;
        }

        public ZoneControlCaptureLinearLogicSO LinearLogicSO => _linearLogicSO;
    }
}
