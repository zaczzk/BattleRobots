using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLucky", order = 161)]
    public sealed class ZoneControlCaptureLuckySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)] private float _intervalSeconds = 10f;
        [SerializeField, Min(0f)] private float _tolerance       = 0.5f;
        [SerializeField, Min(0)]  private int   _bonusPerLucky   = 250;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLuckyCapture;

        private int _luckyCount;
        private int _totalLuckyBonus;

        private void OnEnable() => Reset();

        public int   LuckyCount      => _luckyCount;
        public int   TotalLuckyBonus => _totalLuckyBonus;
        public float IntervalSeconds => _intervalSeconds;
        public float Tolerance       => _tolerance;
        public int   BonusPerLucky   => _bonusPerLucky;

        public void RecordCapture(float matchTime)
        {
            float remainder = matchTime % _intervalSeconds;
            bool atStart = remainder <= _tolerance;
            bool atEnd   = remainder >= _intervalSeconds - _tolerance;

            if (atStart || atEnd)
            {
                _luckyCount++;
                _totalLuckyBonus += _bonusPerLucky;
                _onLuckyCapture?.Raise();
            }
        }

        public void Reset()
        {
            _luckyCount      = 0;
            _totalLuckyBonus = 0;
        }
    }
}
