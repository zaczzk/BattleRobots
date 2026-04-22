using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMonadController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMonadSO _monadSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMonadChained;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _operationLabel;
        [SerializeField] private Text       _chainLabel;
        [SerializeField] private Slider     _operationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMonadChainedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMonadChainedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMonadChained?.RegisterCallback(_handleMonadChainedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMonadChained?.UnregisterCallback(_handleMonadChainedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_monadSO == null) return;
            int bonus = _monadSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_monadSO == null) return;
            _monadSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _monadSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_monadSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_operationLabel != null)
                _operationLabel.text = $"Operations: {_monadSO.Operations}/{_monadSO.OperationsNeeded}";

            if (_chainLabel != null)
                _chainLabel.text = $"Chains: {_monadSO.ChainCount}";

            if (_operationBar != null)
                _operationBar.value = _monadSO.OperationProgress;
        }

        public ZoneControlCaptureMonadSO MonadSO => _monadSO;
    }
}
