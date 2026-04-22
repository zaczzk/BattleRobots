using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLinkController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLinkSO _linkSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onListFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _connectionLabel;
        [SerializeField] private Text       _listLabel;
        [SerializeField] private Slider     _connectionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleListFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleListFormedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onListFormed?.RegisterCallback(_handleListFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onListFormed?.UnregisterCallback(_handleListFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_linkSO == null) return;
            int bonus = _linkSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_linkSO == null) return;
            _linkSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _linkSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_linkSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_connectionLabel != null)
                _connectionLabel.text = $"Connections: {_linkSO.Connections}/{_linkSO.ConnectionsNeeded}";

            if (_listLabel != null)
                _listLabel.text = $"Lists: {_linkSO.ListCount}";

            if (_connectionBar != null)
                _connectionBar.value = _linkSO.ConnectionProgress;
        }

        public ZoneControlCaptureLinkSO LinkSO => _linkSO;
    }
}
