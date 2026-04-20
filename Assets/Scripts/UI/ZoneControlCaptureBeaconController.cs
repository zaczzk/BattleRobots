using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBeaconController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBeaconSO _beaconSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBeaconLit;
        [SerializeField] private VoidGameEvent _onBeaconExtinguished;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _beaconCountLabel;
        [SerializeField] private Slider     _beaconBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBeaconLitDelegate;
        private Action _handleBeaconExtinguishedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate             = HandlePlayerCaptured;
            _handleBotDelegate                = HandleBotCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _handleBeaconLitDelegate          = Refresh;
            _handleBeaconExtinguishedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBeaconLit?.RegisterCallback(_handleBeaconLitDelegate);
            _onBeaconExtinguished?.RegisterCallback(_handleBeaconExtinguishedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBeaconLit?.UnregisterCallback(_handleBeaconLitDelegate);
            _onBeaconExtinguished?.UnregisterCallback(_handleBeaconExtinguishedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_beaconSO == null) return;
            int bonus = _beaconSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_beaconSO == null) return;
            _beaconSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _beaconSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_beaconSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                _statusLabel.text = _beaconSO.IsLit
                    ? $"BEACON LIT! ({_beaconSO.CurrentDurability}/{_beaconSO.DurabilityMax})"
                    : $"Charging: {_beaconSO.ChargeCount}/{_beaconSO.CapturesForBeacon}";
            }

            if (_beaconCountLabel != null)
                _beaconCountLabel.text = $"Beacons: {_beaconSO.BeaconLitCount}";

            if (_beaconBar != null)
                _beaconBar.value = _beaconSO.IsLit
                    ? _beaconSO.DurabilityProgress
                    : _beaconSO.ChargeProgress;
        }

        public ZoneControlCaptureBeaconSO BeaconSO => _beaconSO;
    }
}
