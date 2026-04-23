using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHomology", order = 392)]
    public sealed class ZoneControlCaptureHomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cyclesNeeded    = 6;
        [SerializeField, Min(1)] private int _tearPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerHomology = 2620;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHomologyFormed;

        private int _cycles;
        private int _homologyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CyclesNeeded      => _cyclesNeeded;
        public int   TearPerBot        => _tearPerBot;
        public int   BonusPerHomology  => _bonusPerHomology;
        public int   Cycles            => _cycles;
        public int   HomologyCount     => _homologyCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CycleProgress     => _cyclesNeeded > 0
            ? Mathf.Clamp01(_cycles / (float)_cyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cycles = Mathf.Min(_cycles + 1, _cyclesNeeded);
            if (_cycles >= _cyclesNeeded)
            {
                int bonus = _bonusPerHomology;
                _homologyCount++;
                _totalBonusAwarded += bonus;
                _cycles             = 0;
                _onHomologyFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cycles = Mathf.Max(0, _cycles - _tearPerBot);
        }

        public void Reset()
        {
            _cycles            = 0;
            _homologyCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
