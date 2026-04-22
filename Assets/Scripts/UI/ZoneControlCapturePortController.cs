using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePortController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePortSO _portSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPortOpened;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _portLabel;
        [SerializeField] private Text       _bindLabel;
        [SerializeField] private Slider     _portBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleOpenedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleOpenedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPortOpened?.RegisterCallback(_handleOpenedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPortOpened?.UnregisterCallback(_handleOpenedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_portSO == null) return;
            int bonus = _portSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_portSO == null) return;
            _portSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _portSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_portSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_portLabel != null)
                _portLabel.text = $"Ports: {_portSO.Ports}/{_portSO.PortsNeeded}";

            if (_bindLabel != null)
                _bindLabel.text = $"Binds: {_portSO.BindCount}";

            if (_portBar != null)
                _portBar.value = _portSO.PortProgress;
        }

        public ZoneControlCapturePortSO PortSO => _portSO;
    }
}
