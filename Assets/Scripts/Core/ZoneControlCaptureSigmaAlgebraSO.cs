using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSigmaAlgebra", order = 439)]
    public sealed class ZoneControlCaptureSigmaAlgebraSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _setsNeeded    = 5;
        [SerializeField, Min(1)] private int _scatterPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerMeasure = 3325;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSigmaAlgebraMeasured;

        private int _sets;
        private int _measureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SetsNeeded        => _setsNeeded;
        public int   ScatterPerBot     => _scatterPerBot;
        public int   BonusPerMeasure   => _bonusPerMeasure;
        public int   Sets              => _sets;
        public int   MeasureCount      => _measureCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SetProgress       => _setsNeeded > 0
            ? Mathf.Clamp01(_sets / (float)_setsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sets = Mathf.Min(_sets + 1, _setsNeeded);
            if (_sets >= _setsNeeded)
            {
                int bonus = _bonusPerMeasure;
                _measureCount++;
                _totalBonusAwarded += bonus;
                _sets               = 0;
                _onSigmaAlgebraMeasured?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sets = Mathf.Max(0, _sets - _scatterPerBot);
        }

        public void Reset()
        {
            _sets              = 0;
            _measureCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
