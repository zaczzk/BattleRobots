using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureExponential", order = 411)]
    public sealed class ZoneControlCaptureExponentialSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _basesNeeded      = 5;
        [SerializeField, Min(1)] private int _reducePerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerExponent = 2905;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onExponentRaised;

        private int _bases;
        private int _exponentCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BasesNeeded       => _basesNeeded;
        public int   ReducePerBot      => _reducePerBot;
        public int   BonusPerExponent  => _bonusPerExponent;
        public int   Bases             => _bases;
        public int   ExponentCount     => _exponentCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BaseProgress      => _basesNeeded > 0
            ? Mathf.Clamp01(_bases / (float)_basesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bases = Mathf.Min(_bases + 1, _basesNeeded);
            if (_bases >= _basesNeeded)
            {
                int bonus = _bonusPerExponent;
                _exponentCount++;
                _totalBonusAwarded += bonus;
                _bases              = 0;
                _onExponentRaised?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bases = Mathf.Max(0, _bases - _reducePerBot);
        }

        public void Reset()
        {
            _bases             = 0;
            _exponentCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
