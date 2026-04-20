using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSanctuary", order = 236)]
    public sealed class ZoneControlCaptureSanctuarySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargesNeeded       = 4;
        [SerializeField, Min(0)] private int _bonusPerSanctuary   = 380;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSanctuarySealed;

        private int _charges;
        private int _sanctuaryCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargesNeeded      => _chargesNeeded;
        public int   BonusPerSanctuary  => _bonusPerSanctuary;
        public int   Charges            => _charges;
        public int   SanctuaryCount     => _sanctuaryCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ChargeProgress     => _chargesNeeded > 0
            ? Mathf.Clamp01(_charges / (float)_chargesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _charges++;
            if (_charges >= _chargesNeeded)
            {
                int bonus = _bonusPerSanctuary;
                _sanctuaryCount++;
                _totalBonusAwarded += bonus;
                _charges            = 0;
                _onSanctuarySealed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _charges = Mathf.Max(0, _charges - 1);
        }

        public void Reset()
        {
            _charges           = 0;
            _sanctuaryCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
