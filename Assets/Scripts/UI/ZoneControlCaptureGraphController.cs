using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGraphController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGraphSO _graphSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGraphConnected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _edgeLabel;
        [SerializeField] private Text       _connectLabel;
        [SerializeField] private Slider     _edgeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConnectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConnectedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGraphConnected?.RegisterCallback(_handleConnectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGraphConnected?.UnregisterCallback(_handleConnectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_graphSO == null) return;
            int bonus = _graphSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_graphSO == null) return;
            _graphSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _graphSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_graphSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_edgeLabel != null)
                _edgeLabel.text = $"Edges: {_graphSO.Edges}/{_graphSO.EdgesNeeded}";

            if (_connectLabel != null)
                _connectLabel.text = $"Connects: {_graphSO.ConnectCount}";

            if (_edgeBar != null)
                _edgeBar.value = _graphSO.EdgeProgress;
        }

        public ZoneControlCaptureGraphSO GraphSO => _graphSO;
    }
}
