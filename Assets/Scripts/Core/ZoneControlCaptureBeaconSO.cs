using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBeacon", order = 210)]
    public sealed class ZoneControlCaptureBeaconSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesForBeacon     = 4;
        [SerializeField, Min(0)] private int _bonusPerBeaconCapture = 90;
        [SerializeField, Min(1)] private int _durabilityMax         = 3;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBeaconLit;
        [SerializeField] private VoidGameEvent _onBeaconExtinguished;

        private int  _chargeCount;
        private bool _isLit;
        private int  _currentDurability;
        private int  _beaconLitCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesForBeacon     => _capturesForBeacon;
        public int   BonusPerBeaconCapture => _bonusPerBeaconCapture;
        public int   DurabilityMax         => _durabilityMax;
        public bool  IsLit                 => _isLit;
        public int   ChargeCount           => _chargeCount;
        public int   CurrentDurability     => _currentDurability;
        public int   BeaconLitCount        => _beaconLitCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float ChargeProgress        => !_isLit && _capturesForBeacon > 0
            ? Mathf.Clamp01(_chargeCount / (float)_capturesForBeacon)
            : 0f;
        public float DurabilityProgress    => _isLit && _durabilityMax > 0
            ? Mathf.Clamp01(_currentDurability / (float)_durabilityMax)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_isLit)
            {
                int bonus = _bonusPerBeaconCapture;
                _totalBonusAwarded += bonus;
                return bonus;
            }

            _chargeCount++;
            if (_chargeCount >= _capturesForBeacon)
                Light();
            return 0;
        }

        private void Light()
        {
            _isLit             = true;
            _chargeCount       = 0;
            _currentDurability = _durabilityMax;
            _beaconLitCount++;
            _onBeaconLit?.Raise();
        }

        public void RecordBotCapture()
        {
            if (!_isLit) return;
            _currentDurability--;
            if (_currentDurability <= 0)
                Extinguish();
        }

        private void Extinguish()
        {
            _isLit             = false;
            _currentDurability = 0;
            _onBeaconExtinguished?.Raise();
        }

        public void Reset()
        {
            _chargeCount       = 0;
            _isLit             = false;
            _currentDurability = 0;
            _beaconLitCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
