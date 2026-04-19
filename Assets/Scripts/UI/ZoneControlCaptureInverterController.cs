using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInverterController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInverterSO _inverterSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInversion;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Text       _inversionCountLabel;
        [SerializeField] private Slider     _inverterProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleBotDelegate;
        private Action _handlePlayerDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInversionDelegate;

        private void Awake()
        {
            _handleBotDelegate          = HandleBotCaptured;
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleInversionDelegate    = HandleInversion;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInversion?.RegisterCallback(_handleInversionDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInversion?.UnregisterCallback(_handleInversionDelegate);
        }

        private void HandleBotCaptured()
        {
            if (_inverterSO == null) return;
            _inverterSO.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_inverterSO == null) return;
            int prev   = _inverterSO.InversionCount;
            int bonus  = _inverterSO.RecordPlayerCapture();
            if (_inverterSO.InversionCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _inverterSO?.Reset();
            Refresh();
        }

        private void HandleInversion() => Refresh();

        public void Refresh()
        {
            if (_inverterSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charge: {_inverterSO.BotChargeCount}/{_inverterSO.ChargeThreshold}";

            if (_inversionCountLabel != null)
                _inversionCountLabel.text = $"Inversions: {_inverterSO.InversionCount}";

            if (_inverterProgressBar != null)
                _inverterProgressBar.value = _inverterSO.InversionProgress;
        }

        public ZoneControlCaptureInverterSO InverterSO => _inverterSO;
    }
}
