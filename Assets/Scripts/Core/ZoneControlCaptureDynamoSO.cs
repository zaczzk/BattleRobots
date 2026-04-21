using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDynamo", order = 312)]
    public sealed class ZoneControlCaptureDynamoSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _fieldsNeeded      = 6;
        [SerializeField, Min(1)] private int _slipPerBot        = 2;
        [SerializeField, Min(0)] private int _bonusPerGeneration = 1420;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDynamoGenerated;

        private int _fields;
        private int _generationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FieldsNeeded       => _fieldsNeeded;
        public int   SlipPerBot         => _slipPerBot;
        public int   BonusPerGeneration => _bonusPerGeneration;
        public int   Fields             => _fields;
        public int   GenerationCount    => _generationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float FieldProgress      => _fieldsNeeded > 0
            ? Mathf.Clamp01(_fields / (float)_fieldsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _fields = Mathf.Min(_fields + 1, _fieldsNeeded);
            if (_fields >= _fieldsNeeded)
            {
                int bonus = _bonusPerGeneration;
                _generationCount++;
                _totalBonusAwarded += bonus;
                _fields             = 0;
                _onDynamoGenerated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _fields = Mathf.Max(0, _fields - _slipPerBot);
        }

        public void Reset()
        {
            _fields            = 0;
            _generationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
