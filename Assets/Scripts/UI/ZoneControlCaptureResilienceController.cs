using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureResilienceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureResilienceSO _resilienceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneLost;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onResilienceUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _responseLabel;
        [SerializeField] private Text       _recapturesLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneLostDelegate;
        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneLostDelegate       = HandleZoneLost;
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onZoneLost?.RegisterCallback(_handleZoneLostDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onResilienceUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneLost?.UnregisterCallback(_handleZoneLostDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onResilienceUpdated?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneLost()
        {
            if (_resilienceSO == null) return;
            _resilienceSO.RecordZoneLost(Time.time);
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_resilienceSO == null) return;
            _resilienceSO.RecordRecapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _resilienceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_resilienceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_responseLabel != null)
                _responseLabel.text = $"Response: {_resilienceSO.AverageResponseTime:F1}s avg";

            if (_recapturesLabel != null)
                _recapturesLabel.text = $"Recaptures: {_resilienceSO.RecaptureCount}";
        }

        public ZoneControlCaptureResilienceSO ResilienceSO => _resilienceSO;
    }
}
