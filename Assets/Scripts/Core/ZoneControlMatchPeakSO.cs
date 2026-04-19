using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchPeak", order = 144)]
    public sealed class ZoneControlMatchPeakSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNewPeak;

        private int _currentStreak;
        private int _peakStreak;

        private void OnEnable() => Reset();

        public int CurrentStreak => _currentStreak;
        public int PeakStreak    => _peakStreak;

        public void RecordPlayerCapture()
        {
            _currentStreak++;
            if (_currentStreak > _peakStreak)
            {
                _peakStreak = _currentStreak;
                _onNewPeak?.Raise();
            }
        }

        public void RecordBotCapture()
        {
            _currentStreak = 0;
        }

        public void Reset()
        {
            _currentStreak = 0;
            _peakStreak    = 0;
        }
    }
}
