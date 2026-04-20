using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLightning", order = 256)]
    public sealed class ZoneControlCaptureLightningSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargesNeeded      = 6;
        [SerializeField, Min(1)] private int _dischargePerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerStrike     = 580;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLightningStruck;

        private int _charges;
        private int _strikeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargesNeeded      => _chargesNeeded;
        public int   DischargePerBot    => _dischargePerBot;
        public int   BonusPerStrike     => _bonusPerStrike;
        public int   Charges            => _charges;
        public int   StrikeCount        => _strikeCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ChargeProgress     => _chargesNeeded > 0
            ? Mathf.Clamp01(_charges / (float)_chargesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _charges = Mathf.Min(_charges + 1, _chargesNeeded);
            if (_charges >= _chargesNeeded)
            {
                int bonus = _bonusPerStrike;
                _strikeCount++;
                _totalBonusAwarded += bonus;
                _charges            = 0;
                _onLightningStruck?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _charges = Mathf.Max(0, _charges - _dischargePerBot);
        }

        public void Reset()
        {
            _charges           = 0;
            _strikeCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
