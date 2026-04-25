using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCompactnessTheorem", order = 541)]
    public sealed class ZoneControlCaptureCompactnessTheoremSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _finiteWitnessesNeeded      = 7;
        [SerializeField, Min(1)] private int _unsatisfiableSubsetsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerSatisfaction        = 4855;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCompactnessTheoremSatisfied;

        private int _finiteWitnesses;
        private int _satisfactionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FiniteWitnessesNeeded      => _finiteWitnessesNeeded;
        public int   UnsatisfiableSubsetsPerBot => _unsatisfiableSubsetsPerBot;
        public int   BonusPerSatisfaction        => _bonusPerSatisfaction;
        public int   FiniteWitnesses             => _finiteWitnesses;
        public int   SatisfactionCount           => _satisfactionCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float FiniteWitnessProgress       => _finiteWitnessesNeeded > 0
            ? Mathf.Clamp01(_finiteWitnesses / (float)_finiteWitnessesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _finiteWitnesses = Mathf.Min(_finiteWitnesses + 1, _finiteWitnessesNeeded);
            if (_finiteWitnesses >= _finiteWitnessesNeeded)
            {
                int bonus = _bonusPerSatisfaction;
                _satisfactionCount++;
                _totalBonusAwarded += bonus;
                _finiteWitnesses    = 0;
                _onCompactnessTheoremSatisfied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _finiteWitnesses = Mathf.Max(0, _finiteWitnesses - _unsatisfiableSubsetsPerBot);
        }

        public void Reset()
        {
            _finiteWitnesses   = 0;
            _satisfactionCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}
