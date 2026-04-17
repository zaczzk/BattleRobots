using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges zone-capture events into
    /// <see cref="ZoneControlNeutralZoneTrackerSO"/> and displays the current
    /// neutral zone count and an "All Captured" badge.
    ///
    /// <c>_onZoneCaptured</c>: records a capture + Refresh.
    /// <c>_onZoneReleased</c>: releases a zone + Refresh.
    /// <c>_onMatchStarted</c>: resets the tracker + Refresh.
    /// <c>_onAllZonesCaptured/_onNeutralCountChanged</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlNeutralZoneTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlNeutralZoneTrackerSO _trackerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onZoneReleased;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAllZonesCaptured;
        [SerializeField] private VoidGameEvent _onNeutralCountChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _neutralCountLabel;
        [SerializeField] private GameObject _allCapturedBadge;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleZoneReleasedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleZoneReleasedDelegate = HandleZoneReleased;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onZoneReleased?.RegisterCallback(_handleZoneReleasedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAllZonesCaptured?.RegisterCallback(_refreshDelegate);
            _onNeutralCountChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onZoneReleased?.UnregisterCallback(_handleZoneReleasedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAllZonesCaptured?.UnregisterCallback(_refreshDelegate);
            _onNeutralCountChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            _trackerSO?.RecordCapture();
            Refresh();
        }

        private void HandleZoneReleased()
        {
            _trackerSO?.ReleaseZone();
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

            if (_neutralCountLabel != null)
                _neutralCountLabel.text = $"Neutral: {_trackerSO.NeutralCount}";

            _allCapturedBadge?.SetActive(_trackerSO.AllCaptured);
        }

        public ZoneControlNeutralZoneTrackerSO TrackerSO => _trackerSO;
    }
}
