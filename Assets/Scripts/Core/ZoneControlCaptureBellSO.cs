using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBell", order = 251)]
    public sealed class ZoneControlCaptureBellSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ringsNeeded  = 5;
        [SerializeField, Min(1)] private int _mutePerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerToll = 505;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBellTolled;

        private int _rings;
        private int _tollCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RingsNeeded       => _ringsNeeded;
        public int   MutePerBot        => _mutePerBot;
        public int   BonusPerToll      => _bonusPerToll;
        public int   Rings             => _rings;
        public int   TollCount         => _tollCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float RingProgress      => _ringsNeeded > 0
            ? Mathf.Clamp01(_rings / (float)_ringsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _rings = Mathf.Min(_rings + 1, _ringsNeeded);
            if (_rings >= _ringsNeeded)
            {
                int bonus = _bonusPerToll;
                _tollCount++;
                _totalBonusAwarded += bonus;
                _rings              = 0;
                _onBellTolled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _rings = Mathf.Max(0, _rings - _mutePerBot);
        }

        public void Reset()
        {
            _rings             = 0;
            _tollCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
