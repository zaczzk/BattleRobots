using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates the current capture rate band via
    /// <see cref="ZoneControlCaptureFrequencyBandSO"/> and displays it.
    ///
    /// <c>_onPaceUpdated</c>: reads <c>ZoneCapturePaceTrackerSO.GetCapturesPerMinute</c>,
    /// calls <c>EvaluateBand</c>, and refreshes.
    /// <c>_onMatchStarted</c>: resets the band SO + Refresh.
    /// <c>_onBandChanged</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFrequencyBandController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFrequencyBandSO _bandSO;
        [SerializeField] private ZoneCapturePaceTrackerSO          _trackerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPaceUpdated;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBandChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bandLabel;
        [SerializeField] private Text       _rateLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePaceUpdatedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePaceUpdatedDelegate  = HandlePaceUpdated;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onPaceUpdated?.RegisterCallback(_handlePaceUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBandChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPaceUpdated?.UnregisterCallback(_handlePaceUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBandChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePaceUpdated()
        {
            if (_bandSO == null || _trackerSO == null) return;
            float rate = _trackerSO.GetCapturesPerMinute(Time.time);
            _bandSO.EvaluateBand(rate);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bandSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bandSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bandLabel != null)
                _bandLabel.text = $"Band: {_bandSO.GetBandLabel()}";

            if (_rateLabel != null && _trackerSO != null)
                _rateLabel.text = $"Rate: {_trackerSO.GetCapturesPerMinute(Time.time):F1}/min";
        }

        public ZoneControlCaptureFrequencyBandSO BandSO    => _bandSO;
        public ZoneCapturePaceTrackerSO           TrackerSO => _trackerSO;
    }
}
