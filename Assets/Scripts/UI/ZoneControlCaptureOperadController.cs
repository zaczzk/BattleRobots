using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureOperadController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureOperadSO _operadSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOperadComposed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _operationLabel;
        [SerializeField] private Text       _composeLabel;
        [SerializeField] private Slider     _operationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComposedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComposedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOperadComposed?.RegisterCallback(_handleComposedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOperadComposed?.UnregisterCallback(_handleComposedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_operadSO == null) return;
            int bonus = _operadSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_operadSO == null) return;
            _operadSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _operadSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_operadSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_operationLabel != null)
                _operationLabel.text = $"Operations: {_operadSO.Operations}/{_operadSO.OperationsNeeded}";

            if (_composeLabel != null)
                _composeLabel.text = $"Compositions: {_operadSO.ComposeCount}";

            if (_operationBar != null)
                _operationBar.value = _operadSO.OperationProgress;
        }

        public ZoneControlCaptureOperadSO OperadSO => _operadSO;
    }
}
