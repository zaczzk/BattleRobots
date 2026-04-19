using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureReverberationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureReverberationSO _reverberationSO;
        [SerializeField] private PlayerWalletSO                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onReverberationPayout;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _multiplierLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePayoutDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePayoutDelegate       = HandlePayout;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onReverberationPayout?.RegisterCallback(_handlePayoutDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onReverberationPayout?.UnregisterCallback(_handlePayoutDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_reverberationSO == null) return;
            _reverberationSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_reverberationSO == null) return;
            int payout = _reverberationSO.RecordBotCapture();
            if (payout > 0)
                _wallet?.AddFunds(payout);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _reverberationSO?.Reset();
            Refresh();
        }

        private void HandlePayout()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_reverberationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_multiplierLabel != null)
                _multiplierLabel.text = $"Reverb: {_reverberationSO.CurrentMultiplier}\u00d7";

            if (_totalLabel != null)
                _totalLabel.text = $"Total: {_reverberationSO.TotalEarned}";
        }

        public ZoneControlCaptureReverberationSO ReverberationSO => _reverberationSO;
    }
}
