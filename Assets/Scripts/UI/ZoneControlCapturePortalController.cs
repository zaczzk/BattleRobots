using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePortalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePortalSO _portalSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPortalActivated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Text       _activationLabel;
        [SerializeField] private Slider     _chargeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleActivatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleActivatedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPortalActivated?.RegisterCallback(_handleActivatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPortalActivated?.UnregisterCallback(_handleActivatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_portalSO == null) return;
            int bonus = _portalSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_portalSO == null) return;
            _portalSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _portalSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_portalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charges: {_portalSO.Charges}/{_portalSO.ChargesForActivation}";

            if (_activationLabel != null)
                _activationLabel.text = $"Activations: {_portalSO.ActivationCount}";

            if (_chargeBar != null)
                _chargeBar.value = _portalSO.ChargeProgress;
        }

        public ZoneControlCapturePortalSO PortalSO => _portalSO;
    }
}
