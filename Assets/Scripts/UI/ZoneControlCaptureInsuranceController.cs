using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInsuranceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInsuranceSO _insuranceSO;
        [SerializeField] private PlayerWalletSO                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInsurancePayout;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _poolLabel;
        [SerializeField] private Text       _payoutLabel;
        [SerializeField] private Slider     _poolBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInsurancePayout?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInsurancePayout?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_insuranceSO == null) return;
            _insuranceSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_insuranceSO == null) return;
            int prevTotal = _insuranceSO.TotalPaidOut;
            _insuranceSO.RecordBotCapture();
            int earned = _insuranceSO.TotalPaidOut - prevTotal;
            if (earned > 0) _wallet?.AddFunds(earned);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _insuranceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_insuranceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_poolLabel != null)
                _poolLabel.text = $"Pool: {_insuranceSO.Pool}";

            if (_payoutLabel != null)
                _payoutLabel.text = $"Last Payout: {_insuranceSO.LastPayout}";

            if (_poolBar != null)
                _poolBar.value = _insuranceSO.PoolProgress;
        }

        public ZoneControlCaptureInsuranceSO InsuranceSO => _insuranceSO;
    }
}
