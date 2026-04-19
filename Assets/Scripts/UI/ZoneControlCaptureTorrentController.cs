using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTorrentController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTorrentSO _torrentSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTorrentPayout;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _poolLabel;
        [SerializeField] private Text       _payoutsLabel;
        [SerializeField] private Slider     _torrentProgressBar;
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
            _handlePayoutDelegate       = HandleTorrentPayout;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTorrentPayout?.RegisterCallback(_handlePayoutDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTorrentPayout?.UnregisterCallback(_handlePayoutDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_torrentSO == null) return;
            _torrentSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_torrentSO == null) return;
            int payout = _torrentSO.RecordBotCapture();
            if (payout > 0)
                _wallet?.AddFunds(payout);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _torrentSO?.Reset();
            Refresh();
        }

        private void HandleTorrentPayout() => Refresh();

        public void Refresh()
        {
            if (_torrentSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_poolLabel != null)
                _poolLabel.text = $"Pool: {_torrentSO.TorrentPool}";

            if (_payoutsLabel != null)
                _payoutsLabel.text = $"Payouts: {_torrentSO.TorrentPayouts}";

            if (_torrentProgressBar != null)
                _torrentProgressBar.value = _torrentSO.TorrentProgress;
        }

        public ZoneControlCaptureTorrentSO TorrentSO => _torrentSO;
    }
}
