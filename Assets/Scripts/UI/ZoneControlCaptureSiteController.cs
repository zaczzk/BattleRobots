using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSiteController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSiteSO _siteSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSiteCovered;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _coveringLabel;
        [SerializeField] private Text       _coveredLabel;
        [SerializeField] private Slider     _coveringBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCoveredDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCoveredDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSiteCovered?.RegisterCallback(_handleCoveredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSiteCovered?.UnregisterCallback(_handleCoveredDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_siteSO == null) return;
            int bonus = _siteSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_siteSO == null) return;
            _siteSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _siteSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_siteSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_coveringLabel != null)
                _coveringLabel.text = $"Coverings: {_siteSO.Coverings}/{_siteSO.CoveringsNeeded}";

            if (_coveredLabel != null)
                _coveredLabel.text = $"Covered: {_siteSO.CoveringCount}";

            if (_coveringBar != null)
                _coveringBar.value = _siteSO.SiteProgress;
        }

        public ZoneControlCaptureSiteSO SiteSO => _siteSO;
    }
}
