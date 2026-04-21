using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBannerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBannerSO _bannerSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBannerRaised;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _emblemLabel;
        [SerializeField] private Text       _bannerLabel;
        [SerializeField] private Slider     _emblemBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRaisedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRaisedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBannerRaised?.RegisterCallback(_handleRaisedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBannerRaised?.UnregisterCallback(_handleRaisedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bannerSO == null) return;
            int bonus = _bannerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bannerSO == null) return;
            _bannerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bannerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bannerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_emblemLabel != null)
                _emblemLabel.text = $"Emblems: {_bannerSO.Emblems}/{_bannerSO.EmblemsNeeded}";

            if (_bannerLabel != null)
                _bannerLabel.text = $"Banners: {_bannerSO.BannerCount}";

            if (_emblemBar != null)
                _emblemBar.value = _bannerSO.EmblemProgress;
        }

        public ZoneControlCaptureBannerSO BannerSO => _bannerSO;
    }
}
