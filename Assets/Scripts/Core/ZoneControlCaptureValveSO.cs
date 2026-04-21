using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureValve", order = 303)]
    public sealed class ZoneControlCaptureValveSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stemsNeeded  = 5;
        [SerializeField, Min(1)] private int _leakPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerOpen = 1285;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onValveOpened;

        private int _stems;
        private int _openCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StemsNeeded       => _stemsNeeded;
        public int   LeakPerBot        => _leakPerBot;
        public int   BonusPerOpen      => _bonusPerOpen;
        public int   Stems             => _stems;
        public int   OpenCount         => _openCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StemProgress      => _stemsNeeded > 0
            ? Mathf.Clamp01(_stems / (float)_stemsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stems = Mathf.Min(_stems + 1, _stemsNeeded);
            if (_stems >= _stemsNeeded)
            {
                int bonus = _bonusPerOpen;
                _openCount++;
                _totalBonusAwarded += bonus;
                _stems              = 0;
                _onValveOpened?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stems = Mathf.Max(0, _stems - _leakPerBot);
        }

        public void Reset()
        {
            _stems             = 0;
            _openCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
