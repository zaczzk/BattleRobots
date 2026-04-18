using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRateController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRateSO _rateSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onRateUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _rateLabel;
        [SerializeField] private Text       _captureLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleZoneCapturedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onRateUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onRateUpdated?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchStarted()
        {
            _rateSO?.StartMatch(Time.time);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            _rateSO?.RecordCapture(Time.time);
            Refresh();
        }

        public void Refresh()
        {
            if (_rateSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rateLabel != null)
                _rateLabel.text = $"Rate: {_rateSO.GetAverageRate(Time.time):F1}/min";

            if (_captureLabel != null)
                _captureLabel.text = $"Caps: {_rateSO.CaptureCount}";
        }

        public ZoneControlCaptureRateSO RateSO => _rateSO;
    }
}
