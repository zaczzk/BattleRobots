using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBridgeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBridgeSO _bridgeSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBridgeComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _plankLabel;
        [SerializeField] private Text       _bridgeLabel;
        [SerializeField] private Slider     _plankBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBridgeCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleBridgeCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBridgeComplete?.RegisterCallback(_handleBridgeCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBridgeComplete?.UnregisterCallback(_handleBridgeCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bridgeSO == null) return;
            int bonus = _bridgeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bridgeSO == null) return;
            _bridgeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bridgeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bridgeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_plankLabel != null)
                _plankLabel.text = $"Planks: {_bridgeSO.Planks}/{_bridgeSO.PlanksNeeded}";

            if (_bridgeLabel != null)
                _bridgeLabel.text = $"Bridges: {_bridgeSO.BridgeCount}";

            if (_plankBar != null)
                _plankBar.value = _bridgeSO.PlankProgress;
        }

        public ZoneControlCaptureBridgeSO BridgeSO => _bridgeSO;
    }
}
