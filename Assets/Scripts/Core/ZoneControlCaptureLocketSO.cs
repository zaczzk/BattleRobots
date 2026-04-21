using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLocket", order = 264)]
    public sealed class ZoneControlCaptureLocketSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _charmsNeeded   = 6;
        [SerializeField, Min(1)] private int _removePerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerFill   = 700;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLocketFilled;

        private int _charms;
        private int _locketCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CharmsNeeded      => _charmsNeeded;
        public int   RemovePerBot      => _removePerBot;
        public int   BonusPerFill      => _bonusPerFill;
        public int   Charms            => _charms;
        public int   LocketCount       => _locketCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CharmProgress     => _charmsNeeded > 0
            ? Mathf.Clamp01(_charms / (float)_charmsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _charms = Mathf.Min(_charms + 1, _charmsNeeded);
            if (_charms >= _charmsNeeded)
            {
                int bonus = _bonusPerFill;
                _locketCount++;
                _totalBonusAwarded += bonus;
                _charms             = 0;
                _onLocketFilled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _charms = Mathf.Max(0, _charms - _removePerBot);
        }

        public void Reset()
        {
            _charms            = 0;
            _locketCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
