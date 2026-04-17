using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks how many zones are still neutral (uncaptured).
    ///
    /// Call <see cref="RecordCapture"/> when any zone is captured and
    /// <see cref="ReleaseZone"/> when a zone becomes neutral again.
    /// Fires <c>_onAllZonesCaptured</c> on the first tick when
    /// <see cref="NeutralCount"/> reaches zero.
    /// Fires <c>_onNeutralCountChanged</c> on every capture or release.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlNeutralZoneTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlNeutralZoneTracker", order = 84)]
    public sealed class ZoneControlNeutralZoneTrackerSO : ScriptableObject
    {
        [Header("Zone Settings")]
        [Min(1)]
        [SerializeField] private int _totalZones = 4;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAllZonesCaptured;
        [SerializeField] private VoidGameEvent _onNeutralCountChanged;

        private int  _capturedCount;
        private bool _allCaptured;

        private void OnEnable() => Reset();

        public int  TotalZones    => _totalZones;
        public int  CapturedCount => _capturedCount;
        public bool AllCaptured   => _allCaptured;

        /// <summary>Number of zones that have not yet been captured.</summary>
        public int NeutralCount => Mathf.Max(0, _totalZones - _capturedCount);

        /// <summary>Records one zone capture and re-evaluates neutral state.</summary>
        public void RecordCapture()
        {
            _capturedCount = Mathf.Min(_capturedCount + 1, _totalZones);
            EvaluateNeutral();
        }

        /// <summary>Releases one previously-captured zone back to neutral.</summary>
        public void ReleaseZone()
        {
            _capturedCount = Mathf.Max(0, _capturedCount - 1);
            EvaluateNeutral();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _capturedCount = 0;
            _allCaptured   = false;
        }

        private void EvaluateNeutral()
        {
            _onNeutralCountChanged?.Raise();

            if (NeutralCount == 0 && !_allCaptured)
            {
                _allCaptured = true;
                _onAllZonesCaptured?.Raise();
            }
            else if (NeutralCount > 0 && _allCaptured)
            {
                _allCaptured = false;
            }
        }
    }
}
