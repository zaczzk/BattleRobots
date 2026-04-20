using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAqueductController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAqueductSO _aqueductSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFlow;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _waterLabel;
        [SerializeField] private Text       _flowLabel;
        [SerializeField] private Slider     _waterBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFlowDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFlowDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFlow?.RegisterCallback(_handleFlowDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFlow?.UnregisterCallback(_handleFlowDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_aqueductSO == null) return;
            int bonus = _aqueductSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_aqueductSO == null) return;
            _aqueductSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _aqueductSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_aqueductSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_waterLabel != null)
                _waterLabel.text = $"Water: {_aqueductSO.WaterLevel}/{_aqueductSO.CapturesForFlow}";

            if (_flowLabel != null)
                _flowLabel.text = $"Flows: {_aqueductSO.FlowCount}";

            if (_waterBar != null)
                _waterBar.value = _aqueductSO.WaterProgress;
        }

        public ZoneControlCaptureAqueductSO AqueductSO => _aqueductSO;
    }
}
