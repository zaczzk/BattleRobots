using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCapacitor", order = 317)]
    public sealed class ZoneControlCaptureCapacitorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargesNeeded      = 5;
        [SerializeField, Min(1)] private int _leakPerBot         = 1;
        [SerializeField, Min(0)] private int _bonusPerDischarge  = 1495;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCapacitorDischarged;

        private int _charge;
        private int _dischargeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargesNeeded      => _chargesNeeded;
        public int   LeakPerBot         => _leakPerBot;
        public int   BonusPerDischarge  => _bonusPerDischarge;
        public int   Charge             => _charge;
        public int   DischargeCount     => _dischargeCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ChargeProgress     => _chargesNeeded > 0
            ? Mathf.Clamp01(_charge / (float)_chargesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _charge = Mathf.Min(_charge + 1, _chargesNeeded);
            if (_charge >= _chargesNeeded)
            {
                int bonus = _bonusPerDischarge;
                _dischargeCount++;
                _totalBonusAwarded += bonus;
                _charge             = 0;
                _onCapacitorDischarged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _charge = Mathf.Max(0, _charge - _leakPerBot);
        }

        public void Reset()
        {
            _charge            = 0;
            _dischargeCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
