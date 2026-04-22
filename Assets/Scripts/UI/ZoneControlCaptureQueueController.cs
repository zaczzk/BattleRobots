using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureQueueController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureQueueSO _queueSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onQueueDispatched;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _messageLabel;
        [SerializeField] private Text       _dispatchLabel;
        [SerializeField] private Slider     _messageBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDispatchedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDispatchedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onQueueDispatched?.RegisterCallback(_handleDispatchedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onQueueDispatched?.UnregisterCallback(_handleDispatchedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_queueSO == null) return;
            int bonus = _queueSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_queueSO == null) return;
            _queueSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _queueSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_queueSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_messageLabel != null)
                _messageLabel.text = $"Messages: {_queueSO.Messages}/{_queueSO.MessagesNeeded}";

            if (_dispatchLabel != null)
                _dispatchLabel.text = $"Dispatches: {_queueSO.DispatchCount}";

            if (_messageBar != null)
                _messageBar.value = _queueSO.MessageProgress;
        }

        public ZoneControlCaptureQueueSO QueueSO => _queueSO;
    }
}
