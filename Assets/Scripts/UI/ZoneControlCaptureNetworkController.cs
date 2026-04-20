using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNetworkController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNetworkSO _networkSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNetworkFired;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _nodeLabel;
        [SerializeField] private Text       _networkCountLabel;
        [SerializeField] private Slider     _nodeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNetworkDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleNetworkDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNetworkFired?.RegisterCallback(_handleNetworkDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNetworkFired?.UnregisterCallback(_handleNetworkDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_networkSO == null) return;
            int prev  = _networkSO.NetworkCount;
            int bonus = _networkSO.RecordPlayerCapture();
            if (_networkSO.NetworkCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_networkSO == null) return;
            _networkSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _networkSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_networkSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_nodeLabel != null)
                _nodeLabel.text = $"Nodes: {_networkSO.ActiveNodes}/{_networkSO.NodeCount}";

            if (_networkCountLabel != null)
                _networkCountLabel.text = $"Networks: {_networkSO.NetworkCount}";

            if (_nodeBar != null)
                _nodeBar.value = _networkSO.NodeProgress;
        }

        public ZoneControlCaptureNetworkSO NetworkSO => _networkSO;
    }
}
