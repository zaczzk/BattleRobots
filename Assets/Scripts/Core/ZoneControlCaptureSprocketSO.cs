using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSprocket", order = 301)]
    public sealed class ZoneControlCaptureSprocketSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _teethNeeded    = 5;
        [SerializeField, Min(1)] private int _wearPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerEngage = 1255;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSprocketEngaged;

        private int _teeth;
        private int _engageCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TeethNeeded       => _teethNeeded;
        public int   WearPerBot        => _wearPerBot;
        public int   BonusPerEngage    => _bonusPerEngage;
        public int   Teeth             => _teeth;
        public int   EngageCount       => _engageCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ToothProgress     => _teethNeeded > 0
            ? Mathf.Clamp01(_teeth / (float)_teethNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _teeth = Mathf.Min(_teeth + 1, _teethNeeded);
            if (_teeth >= _teethNeeded)
            {
                int bonus = _bonusPerEngage;
                _engageCount++;
                _totalBonusAwarded += bonus;
                _teeth              = 0;
                _onSprocketEngaged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _teeth = Mathf.Max(0, _teeth - _wearPerBot);
        }

        public void Reset()
        {
            _teeth             = 0;
            _engageCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
