using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks which unique zone indices a player has captured during
    /// a match, rewarding exploration of the entire zone set.
    ///
    /// Call <see cref="RecordVisit(int)"/> when the player captures a zone.
    /// Fires <c>_onNewZoneVisited</c> the first time each distinct index is seen.
    /// Fires <c>_onAllZonesVisited</c> once all <see cref="TotalZones"/> indices have
    /// been visited (idempotent thereafter).
    /// <see cref="Reset"/> clears state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZonePatrol.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZonePatrol", order = 87)]
    public sealed class ZoneControlZonePatrolSO : ScriptableObject
    {
        [Header("Settings")]
        [Min(1)]
        [SerializeField] private int _totalZones = 4;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNewZoneVisited;
        [SerializeField] private VoidGameEvent _onAllZonesVisited;

        private readonly HashSet<int> _visitedZones = new HashSet<int>();
        private bool _allVisited;

        private void OnEnable() => Reset();

        public int  TotalZones   => _totalZones;
        public int  VisitedCount => _visitedZones.Count;
        public bool AllVisited   => _allVisited;

        /// <summary>
        /// Records a visit to <paramref name="zoneIndex"/>.  Out-of-range indices
        /// (negative or ≥ <see cref="TotalZones"/>) are silently ignored.
        /// Fires <c>_onNewZoneVisited</c> on a first-time visit and
        /// <c>_onAllZonesVisited</c> when all zones have been visited.
        /// </summary>
        public void RecordVisit(int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= _totalZones) return;
            if (_visitedZones.Contains(zoneIndex)) return;

            _visitedZones.Add(zoneIndex);
            _onNewZoneVisited?.Raise();
            EvaluateAllVisited();
        }

        /// <summary>Returns true if the given zone has already been visited.</summary>
        public bool HasVisited(int zoneIndex) => _visitedZones.Contains(zoneIndex);

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _visitedZones.Clear();
            _allVisited = false;
        }

        private void EvaluateAllVisited()
        {
            if (_allVisited) return;
            if (_visitedZones.Count >= _totalZones)
            {
                _allVisited = true;
                _onAllZonesVisited?.Raise();
            }
        }
    }
}
