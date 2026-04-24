using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCharacteristicClass", order = 488)]
    public sealed class ZoneControlCaptureCharacteristicClassSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _obstructionsNeeded   = 5;
        [SerializeField, Min(1)] private int _trivializationsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerEvaluation   = 4060;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCharacteristicClassEvaluated;

        private int _obstructions;
        private int _evaluationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ObstructionsNeeded    => _obstructionsNeeded;
        public int   TrivializationsPerBot => _trivializationsPerBot;
        public int   BonusPerEvaluation    => _bonusPerEvaluation;
        public int   Obstructions          => _obstructions;
        public int   EvaluationCount       => _evaluationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float ObstructionProgress   => _obstructionsNeeded > 0
            ? Mathf.Clamp01(_obstructions / (float)_obstructionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _obstructions = Mathf.Min(_obstructions + 1, _obstructionsNeeded);
            if (_obstructions >= _obstructionsNeeded)
            {
                int bonus = _bonusPerEvaluation;
                _evaluationCount++;
                _totalBonusAwarded += bonus;
                _obstructions       = 0;
                _onCharacteristicClassEvaluated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _obstructions = Mathf.Max(0, _obstructions - _trivializationsPerBot);
        }

        public void Reset()
        {
            _obstructions      = 0;
            _evaluationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
