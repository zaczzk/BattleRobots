using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSessionTypesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSessionTypesSO _sessionTypesSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSessionTypesCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _protocolStepLabel;
        [SerializeField] private Text       _sessionCountLabel;
        [SerializeField] private Slider     _protocolStepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSessionTypesCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSessionTypesCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sessionTypesSO == null) return;
            int bonus = _sessionTypesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sessionTypesSO == null) return;
            _sessionTypesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sessionTypesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sessionTypesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_protocolStepLabel != null)
                _protocolStepLabel.text =
                    $"Protocol Steps: {_sessionTypesSO.ProtocolSteps}/{_sessionTypesSO.ProtocolStepsNeeded}";

            if (_sessionCountLabel != null)
                _sessionCountLabel.text = $"Sessions: {_sessionTypesSO.SessionCount}";

            if (_protocolStepBar != null)
                _protocolStepBar.value = _sessionTypesSO.ProtocolStepProgress;
        }

        public ZoneControlCaptureSessionTypesSO SessionTypesSO => _sessionTypesSO;
    }
}
