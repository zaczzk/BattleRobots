using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureValueTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureValueTrackerSO _trackerSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Config")]
        [SerializeField, Min(0)] private int _baseValuePerCapture = 100;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onValueUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _totalValueLabel;
        [SerializeField] private Text       _avgValueLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onValueUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onValueUpdated?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_trackerSO == null) return;
            _trackerSO.RecordCapture(_baseValuePerCapture);
            _wallet?.AddFunds(_baseValuePerCapture);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _trackerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_trackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_totalValueLabel != null)
                _totalValueLabel.text = $"Total Value: {_trackerSO.TotalValue}";

            if (_avgValueLabel != null)
                _avgValueLabel.text = $"Avg Value: {Mathf.RoundToInt(_trackerSO.AverageValue)}";
        }

        public ZoneControlCaptureValueTrackerSO TrackerSO => _trackerSO;
    }
}
