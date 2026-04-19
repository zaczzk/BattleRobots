using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInverter", order = 192)]
    public sealed class ZoneControlCaptureInverterSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargeThreshold   = 3;
        [SerializeField, Min(0)] private int _bonusPerInversion = 220;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInversion;

        private int _botChargeCount;
        private int _inversionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargeThreshold   => _chargeThreshold;
        public int   BonusPerInversion => _bonusPerInversion;
        public int   BotChargeCount    => _botChargeCount;
        public int   InversionCount    => _inversionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float InversionProgress => _chargeThreshold > 0
            ? Mathf.Clamp01(_botChargeCount / (float)_chargeThreshold)
            : 0f;

        public void RecordBotCapture() => _botChargeCount++;

        public int RecordPlayerCapture()
        {
            if (_botChargeCount < _chargeThreshold) return 0;
            _botChargeCount    -= _chargeThreshold;
            _inversionCount++;
            _totalBonusAwarded += _bonusPerInversion;
            _onInversion?.Raise();
            return _bonusPerInversion;
        }

        public void Reset()
        {
            _botChargeCount    = 0;
            _inversionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
