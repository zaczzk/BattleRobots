using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchScoreGateController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchScoreGateSO _gateSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGatePassed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gateLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _gateBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGatePassedDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGatePassedDelegate   = HandleGatePassed;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGatePassed?.RegisterCallback(_handleGatePassedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGatePassed?.UnregisterCallback(_handleGatePassedDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_gateSO == null) return;
            bool wasPassed = _gateSO.GatePassed;
            _gateSO.RecordCapture();
            if (!wasPassed && _gateSO.GatePassed)
                _wallet?.AddFunds(_gateSO.BonusOnPass);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gateSO?.Reset();
            Refresh();
        }

        private void HandleGatePassed()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_gateSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gateLabel != null)
                _gateLabel.text = $"Gate: {_gateSO.CaptureCount}/{_gateSO.GateTarget}";

            if (_statusLabel != null)
                _statusLabel.text = _gateSO.GatePassed ? "PASSED!" : "Pending";

            if (_gateBar != null)
                _gateBar.value = _gateSO.GateProgress;
        }

        public ZoneControlMatchScoreGateSO GateSO => _gateSO;
    }
}
