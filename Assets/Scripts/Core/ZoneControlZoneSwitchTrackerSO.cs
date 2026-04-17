using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks how often the player switches to a different zone
    /// (i.e. captures a zone whose index differs from the last captured zone).
    ///
    /// Call <see cref="RecordCapture(int)"/> each time the player captures a zone.
    /// A "switch" is counted when the captured zone index differs from the
    /// previously captured zone index.  The first capture is never a switch.
    ///
    /// Fires <c>_onSwitchRecorded</c> on every switch and
    /// <c>_onFrequentSwitcher</c> the first time <see cref="SwitchCount"/>
    /// reaches <see cref="FrequentSwitchThreshold"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneSwitchTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneSwitchTracker", order = 95)]
    public sealed class ZoneControlZoneSwitchTrackerSO : ScriptableObject
    {
        [Header("Switch Tracker Settings")]
        [Min(1)]
        [SerializeField] private int _frequentSwitchThreshold = 5;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSwitchRecorded;
        [SerializeField] private VoidGameEvent _onFrequentSwitcher;

        private int  _switchCount;
        private int  _lastZoneIndex = -1;
        private bool _frequentFired;

        private void OnEnable() => Reset();

        public int  SwitchCount              => _switchCount;
        public int  FrequentSwitchThreshold  => _frequentSwitchThreshold;
        public bool IsFrequentSwitcher       => _switchCount >= _frequentSwitchThreshold;
        public int  LastZoneIndex            => _lastZoneIndex;

        /// <summary>
        /// Records a zone capture.  A switch is counted when <paramref name="zoneIndex"/>
        /// differs from the previously captured zone.  The first capture initialises
        /// the last-zone index without counting as a switch.
        /// </summary>
        public void RecordCapture(int zoneIndex)
        {
            if (_lastZoneIndex == -1)
            {
                _lastZoneIndex = zoneIndex;
                return;
            }

            if (zoneIndex == _lastZoneIndex)
                return;

            _lastZoneIndex = zoneIndex;
            _switchCount++;
            _onSwitchRecorded?.Raise();

            if (!_frequentFired && _switchCount >= _frequentSwitchThreshold)
            {
                _frequentFired = true;
                _onFrequentSwitcher?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _switchCount   = 0;
            _lastZoneIndex = -1;
            _frequentFired = false;
        }
    }
}
