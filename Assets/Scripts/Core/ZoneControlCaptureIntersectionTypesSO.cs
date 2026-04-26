using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureIntersectionTypes", order = 564)]
    public sealed class ZoneControlCaptureIntersectionTypesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _witnessesNeeded     = 6;
        [SerializeField, Min(1)] private int _typeConflictsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerIntersection = 5200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onIntersectionTypesCompleted;

        private int _witnesses;
        private int _intersectionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   WitnessesNeeded    => _witnessesNeeded;
        public int   TypeConflictsPerBot => _typeConflictsPerBot;
        public int   BonusPerIntersection => _bonusPerIntersection;
        public int   Witnesses           => _witnesses;
        public int   IntersectionCount   => _intersectionCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float WitnessProgress => _witnessesNeeded > 0
            ? Mathf.Clamp01(_witnesses / (float)_witnessesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _witnesses = Mathf.Min(_witnesses + 1, _witnessesNeeded);
            if (_witnesses >= _witnessesNeeded)
            {
                int bonus = _bonusPerIntersection;
                _intersectionCount++;
                _totalBonusAwarded += bonus;
                _witnesses          = 0;
                _onIntersectionTypesCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _witnesses = Mathf.Max(0, _witnesses - _typeConflictsPerBot);
        }

        public void Reset()
        {
            _witnesses         = 0;
            _intersectionCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}
