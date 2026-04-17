using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges player zone-capture events into
    /// <see cref="ZoneControlZonePatrolSO"/> and displays how many unique zones
    /// the player has captured this match.
    ///
    /// <c>_onPlayerZoneCaptured</c> (IntGameEvent): records a visit for the given
    /// zone index + Refresh.
    /// <c>_onMatchStarted</c>: resets the patrol SO + Refresh.
    /// <c>_onNewZoneVisited/_onAllZonesVisited</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZonePatrolController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZonePatrolSO _patrolSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private IntGameEvent  _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNewZoneVisited;
        [SerializeField] private VoidGameEvent _onAllZonesVisited;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _visitedLabel;
        [SerializeField] private GameObject _allVisitedBadge;
        [SerializeField] private GameObject _panel;

        private Action<int> _handlePlayerZoneCapturedDelegate;
        private Action      _handleMatchStartedDelegate;
        private Action      _refreshDelegate;

        private void Awake()
        {
            _handlePlayerZoneCapturedDelegate = HandlePlayerZoneCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _refreshDelegate                  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNewZoneVisited?.RegisterCallback(_refreshDelegate);
            _onAllZonesVisited?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNewZoneVisited?.UnregisterCallback(_refreshDelegate);
            _onAllZonesVisited?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerZoneCaptured(int zoneIndex)
        {
            _patrolSO?.RecordVisit(zoneIndex);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _patrolSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_patrolSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_visitedLabel != null)
                _visitedLabel.text = $"Visited: {_patrolSO.VisitedCount}/{_patrolSO.TotalZones}";

            _allVisitedBadge?.SetActive(_patrolSO.AllVisited);
        }

        public ZoneControlZonePatrolSO PatrolSO => _patrolSO;
    }
}
