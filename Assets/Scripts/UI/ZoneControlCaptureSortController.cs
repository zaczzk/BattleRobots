using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSortController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSortSO _sortSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSortComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _comparisonLabel;
        [SerializeField] private Text       _sortLabel;
        [SerializeField] private Slider     _comparisonBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSortCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSortCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSortComplete?.RegisterCallback(_handleSortCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSortComplete?.UnregisterCallback(_handleSortCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sortSO == null) return;
            int bonus = _sortSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sortSO == null) return;
            _sortSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sortSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sortSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_comparisonLabel != null)
                _comparisonLabel.text = $"Comparisons: {_sortSO.Comparisons}/{_sortSO.ComparisonsNeeded}";

            if (_sortLabel != null)
                _sortLabel.text = $"Sorts: {_sortSO.SortCount}";

            if (_comparisonBar != null)
                _comparisonBar.value = _sortSO.ComparisonProgress;
        }

        public ZoneControlCaptureSortSO SortSO => _sortSO;
    }
}
