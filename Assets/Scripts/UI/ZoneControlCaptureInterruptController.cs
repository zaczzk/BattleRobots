using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInterruptController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInterruptSO _interruptSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInterruptHandled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _irqLabel;
        [SerializeField] private Text       _isrLabel;
        [SerializeField] private Slider     _irqBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHandledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleHandledDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInterruptHandled?.RegisterCallback(_handleHandledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInterruptHandled?.UnregisterCallback(_handleHandledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_interruptSO == null) return;
            int bonus = _interruptSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_interruptSO == null) return;
            _interruptSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _interruptSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_interruptSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_irqLabel != null)
                _irqLabel.text = $"IRQs: {_interruptSO.Irqs}/{_interruptSO.IrqsNeeded}";

            if (_isrLabel != null)
                _isrLabel.text = $"ISRs: {_interruptSO.IsrCount}";

            if (_irqBar != null)
                _irqBar.value = _interruptSO.IrqProgress;
        }

        public ZoneControlCaptureInterruptSO InterruptSO => _interruptSO;
    }
}
