using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTalisman", order = 266)]
    public sealed class ZoneControlCaptureTalismanSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargesNeeded       = 7;
        [SerializeField, Min(1)] private int _drainPerBot         = 2;
        [SerializeField, Min(0)] private int _bonusPerActivation  = 730;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTalismanActivated;

        private int _charges;
        private int _activationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargesNeeded      => _chargesNeeded;
        public int   DrainPerBot        => _drainPerBot;
        public int   BonusPerActivation => _bonusPerActivation;
        public int   Charges            => _charges;
        public int   ActivationCount    => _activationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ChargeProgress     => _chargesNeeded > 0
            ? Mathf.Clamp01(_charges / (float)_chargesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _charges = Mathf.Min(_charges + 1, _chargesNeeded);
            if (_charges >= _chargesNeeded)
            {
                int bonus = _bonusPerActivation;
                _activationCount++;
                _totalBonusAwarded += bonus;
                _charges            = 0;
                _onTalismanActivated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _charges = Mathf.Max(0, _charges - _drainPerBot);
        }

        public void Reset()
        {
            _charges           = 0;
            _activationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
