using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureColimitController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureColimitSO _colimitSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onColimitComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _diagramLabel;
        [SerializeField] private Text       _colimitLabel;
        [SerializeField] private Slider     _diagramBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComputedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComputedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onColimitComputed?.RegisterCallback(_handleComputedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onColimitComputed?.UnregisterCallback(_handleComputedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_colimitSO == null) return;
            int bonus = _colimitSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_colimitSO == null) return;
            _colimitSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _colimitSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_colimitSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_diagramLabel != null)
                _diagramLabel.text = $"Diagrams: {_colimitSO.Diagrams}/{_colimitSO.DiagramsNeeded}";

            if (_colimitLabel != null)
                _colimitLabel.text = $"Colimits: {_colimitSO.ColimitCount}";

            if (_diagramBar != null)
                _diagramBar.value = _colimitSO.DiagramProgress;
        }

        public ZoneControlCaptureColimitSO ColimitSO => _colimitSO;
    }
}
