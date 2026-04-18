using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneActivityTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneActivityTrackerSO _activityTrackerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onActivityMilestone;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _activityLabel;
        [SerializeField] private Text       _nextLabel;
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
            _onActivityMilestone?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onActivityMilestone?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_activityTrackerSO == null) return;
            _activityTrackerSO.RecordActivity();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _activityTrackerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_activityTrackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_activityLabel != null)
                _activityLabel.text = $"Activity: {_activityTrackerSO.TotalActivity}";

            if (_nextLabel != null)
                _nextLabel.text = $"Next: {_activityTrackerSO.NextMilestone}";
        }

        public ZoneControlZoneActivityTrackerSO ActivityTrackerSO => _activityTrackerSO;
    }
}
