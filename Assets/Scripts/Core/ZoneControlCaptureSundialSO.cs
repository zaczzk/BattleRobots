using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSundial", order = 252)]
    public sealed class ZoneControlCaptureSundialSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _hoursNeeded   = 6;
        [SerializeField, Min(1)] private int _shadowPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerNoon  = 475;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNoonReached;

        private int _hours;
        private int _noonCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HoursNeeded       => _hoursNeeded;
        public int   ShadowPerBot      => _shadowPerBot;
        public int   BonusPerNoon      => _bonusPerNoon;
        public int   Hours             => _hours;
        public int   NoonCount         => _noonCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float HourProgress      => _hoursNeeded > 0
            ? Mathf.Clamp01(_hours / (float)_hoursNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _hours = Mathf.Min(_hours + 1, _hoursNeeded);
            if (_hours >= _hoursNeeded)
            {
                int bonus = _bonusPerNoon;
                _noonCount++;
                _totalBonusAwarded += bonus;
                _hours              = 0;
                _onNoonReached?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _hours = Mathf.Max(0, _hours - _shadowPerBot);
        }

        public void Reset()
        {
            _hours             = 0;
            _noonCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
