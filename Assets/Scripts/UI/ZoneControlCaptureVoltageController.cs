using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureVoltageController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureVoltageSO _voltageSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onDischarge;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _voltageLabel;
        [SerializeField] private Text       _dischargeLabel;
        [SerializeField] private Slider     _voltageBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleDischargeDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleDischargeDelegate    = HandleDischarge;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onDischarge?.RegisterCallback(_handleDischargeDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onDischarge?.UnregisterCallback(_handleDischargeDelegate);
        }

        private void Update()
        {
            if (_voltageSO == null) return;
            _voltageSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_voltageSO == null) return;
            int prev = _voltageSO.DischargeCount;
            _voltageSO.RecordCapture();
            if (_voltageSO.DischargeCount > prev)
                _wallet?.AddFunds(_voltageSO.BonusOnDischarge);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _voltageSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _voltageSO?.Reset();
            Refresh();
        }

        private void HandleDischarge()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_voltageSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_voltageLabel != null)
                _voltageLabel.text = $"Voltage: {_voltageSO.VoltageProgress * 100f:F1}%";

            if (_dischargeLabel != null)
                _dischargeLabel.text = $"Discharges: {_voltageSO.DischargeCount}";

            if (_voltageBar != null)
                _voltageBar.value = _voltageSO.VoltageProgress;
        }

        public ZoneControlCaptureVoltageSO VoltageSO => _voltageSO;
    }
}
