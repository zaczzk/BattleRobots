using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTransistorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTransistorSO _transistorSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTransistorSwitched;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gateLabel;
        [SerializeField] private Text       _switchLabel;
        [SerializeField] private Slider     _gateBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSwitchedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSwitchedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTransistorSwitched?.RegisterCallback(_handleSwitchedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTransistorSwitched?.UnregisterCallback(_handleSwitchedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_transistorSO == null) return;
            int bonus = _transistorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_transistorSO == null) return;
            _transistorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _transistorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_transistorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gateLabel != null)
                _gateLabel.text = $"Gates: {_transistorSO.Gates}/{_transistorSO.GatesNeeded}";

            if (_switchLabel != null)
                _switchLabel.text = $"Switches: {_transistorSO.SwitchCount}";

            if (_gateBar != null)
                _gateBar.value = _transistorSO.GateProgress;
        }

        public ZoneControlCaptureTransistorSO TransistorSO => _transistorSO;
    }
}
