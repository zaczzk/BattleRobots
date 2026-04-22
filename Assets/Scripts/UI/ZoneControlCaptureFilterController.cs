using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFilterController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFilterSO _filterSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFilterApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bandLabel;
        [SerializeField] private Text       _filterLabel;
        [SerializeField] private Slider     _bandBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFilterDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFilterDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFilterApplied?.RegisterCallback(_handleFilterDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFilterApplied?.UnregisterCallback(_handleFilterDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_filterSO == null) return;
            int bonus = _filterSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_filterSO == null) return;
            _filterSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _filterSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_filterSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bandLabel != null)
                _bandLabel.text = $"Bands: {_filterSO.Bands}/{_filterSO.BandsNeeded}";

            if (_filterLabel != null)
                _filterLabel.text = $"Filters: {_filterSO.FilterCount}";

            if (_bandBar != null)
                _bandBar.value = _filterSO.BandProgress;
        }

        public ZoneControlCaptureFilterSO FilterSO => _filterSO;
    }
}
