using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePulley", order = 291)]
    public sealed class ZoneControlCapturePulleySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _hoistsNeeded  = 5;
        [SerializeField, Min(1)] private int _slipPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerLift  = 1105;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPulleyLifted;

        private int _hoists;
        private int _liftCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HoistsNeeded      => _hoistsNeeded;
        public int   SlipPerBot        => _slipPerBot;
        public int   BonusPerLift      => _bonusPerLift;
        public int   Hoists            => _hoists;
        public int   LiftCount         => _liftCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float HoistProgress     => _hoistsNeeded > 0
            ? Mathf.Clamp01(_hoists / (float)_hoistsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _hoists = Mathf.Min(_hoists + 1, _hoistsNeeded);
            if (_hoists >= _hoistsNeeded)
            {
                int bonus = _bonusPerLift;
                _liftCount++;
                _totalBonusAwarded += bonus;
                _hoists             = 0;
                _onPulleyLifted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _hoists = Mathf.Max(0, _hoists - _slipPerBot);
        }

        public void Reset()
        {
            _hoists            = 0;
            _liftCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
