using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBellows", order = 284)]
    public sealed class ZoneControlCaptureBellowsSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pumpsNeeded    = 6;
        [SerializeField, Min(1)] private int _releasePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerBlast  = 1000;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBellowsBlasted;

        private int _pumps;
        private int _blastCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PumpsNeeded       => _pumpsNeeded;
        public int   ReleasePerBot     => _releasePerBot;
        public int   BonusPerBlast     => _bonusPerBlast;
        public int   Pumps             => _pumps;
        public int   BlastCount        => _blastCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PumpProgress      => _pumpsNeeded > 0
            ? Mathf.Clamp01(_pumps / (float)_pumpsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pumps = Mathf.Min(_pumps + 1, _pumpsNeeded);
            if (_pumps >= _pumpsNeeded)
            {
                int bonus = _bonusPerBlast;
                _blastCount++;
                _totalBonusAwarded += bonus;
                _pumps              = 0;
                _onBellowsBlasted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pumps = Mathf.Max(0, _pumps - _releasePerBot);
        }

        public void Reset()
        {
            _pumps             = 0;
            _blastCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
