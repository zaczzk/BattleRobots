using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLensController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLensSO _lensSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLensViewed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _facetLabel;
        [SerializeField] private Text       _viewLabel;
        [SerializeField] private Slider     _facetBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleViewedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleViewedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLensViewed?.RegisterCallback(_handleViewedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLensViewed?.UnregisterCallback(_handleViewedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lensSO == null) return;
            int bonus = _lensSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lensSO == null) return;
            _lensSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lensSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lensSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_facetLabel != null)
                _facetLabel.text = $"Facets: {_lensSO.Facets}/{_lensSO.FacetsNeeded}";

            if (_viewLabel != null)
                _viewLabel.text = $"Views: {_lensSO.ViewCount}";

            if (_facetBar != null)
                _facetBar.value = _lensSO.FacetProgress;
        }

        public ZoneControlCaptureLensSO LensSO => _lensSO;
    }
}
