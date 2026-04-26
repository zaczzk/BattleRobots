using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDependentTypes", order = 554)]
    public sealed class ZoneControlCaptureDependentTypesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _typeWitnessesNeeded       = 6;
        [SerializeField, Min(1)] private int _typeCheckingFailuresPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerWitness           = 5050;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDependentTypesCompleted;

        private int _typeWitnesses;
        private int _witnessCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TypeWitnessesNeeded       => _typeWitnessesNeeded;
        public int   TypeCheckingFailuresPerBot => _typeCheckingFailuresPerBot;
        public int   BonusPerWitness           => _bonusPerWitness;
        public int   TypeWitnesses             => _typeWitnesses;
        public int   WitnessCount              => _witnessCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float TypeWitnessProgress => _typeWitnessesNeeded > 0
            ? Mathf.Clamp01(_typeWitnesses / (float)_typeWitnessesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _typeWitnesses = Mathf.Min(_typeWitnesses + 1, _typeWitnessesNeeded);
            if (_typeWitnesses >= _typeWitnessesNeeded)
            {
                int bonus = _bonusPerWitness;
                _witnessCount++;
                _totalBonusAwarded += bonus;
                _typeWitnesses      = 0;
                _onDependentTypesCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _typeWitnesses = Mathf.Max(0, _typeWitnesses - _typeCheckingFailuresPerBot);
        }

        public void Reset()
        {
            _typeWitnesses     = 0;
            _witnessCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
