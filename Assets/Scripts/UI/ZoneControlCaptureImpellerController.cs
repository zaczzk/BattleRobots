using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureImpellerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureImpellerSO _impellerSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onImpellerPumped;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _vaneLabel;
        [SerializeField] private Text       _pumpLabel;
        [SerializeField] private Slider     _vaneBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePumpedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePumpedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onImpellerPumped?.RegisterCallback(_handlePumpedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onImpellerPumped?.UnregisterCallback(_handlePumpedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_impellerSO == null) return;
            int bonus = _impellerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_impellerSO == null) return;
            _impellerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _impellerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_impellerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_vaneLabel != null)
                _vaneLabel.text = $"Vanes: {_impellerSO.Vanes}/{_impellerSO.VanesNeeded}";

            if (_pumpLabel != null)
                _pumpLabel.text = $"Pumps: {_impellerSO.PumpCount}";

            if (_vaneBar != null)
                _vaneBar.value = _impellerSO.VaneProgress;
        }

        public ZoneControlCaptureImpellerSO ImpellerSO => _impellerSO;
    }
}
