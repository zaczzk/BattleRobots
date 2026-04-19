using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureVoltage", order = 177)]
    public sealed class ZoneControlCaptureVoltageSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)] private float _chargePerCapture = 25f;
        [SerializeField, Min(1f)] private float _maxVoltage       = 100f;
        [SerializeField, Min(0f)] private float _decayRate        = 8f;
        [SerializeField, Min(0)]  private int   _bonusOnDischarge = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDischarge;

        private float _currentVoltage;
        private int   _dischargeCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float ChargePerCapture  => _chargePerCapture;
        public float MaxVoltage        => _maxVoltage;
        public float DecayRate         => _decayRate;
        public int   BonusOnDischarge  => _bonusOnDischarge;
        public float CurrentVoltage    => _currentVoltage;
        public int   DischargeCount    => _dischargeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float VoltageProgress   => Mathf.Clamp01(_currentVoltage / Mathf.Max(1f, _maxVoltage));

        public void RecordCapture()
        {
            _currentVoltage = Mathf.Min(_currentVoltage + _chargePerCapture, _maxVoltage);
            if (_currentVoltage >= _maxVoltage)
                Discharge();
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            _currentVoltage = Mathf.Max(0f, _currentVoltage - _decayRate * dt);
        }

        private void Discharge()
        {
            _dischargeCount++;
            _totalBonusAwarded += _bonusOnDischarge;
            _currentVoltage     = 0f;
            _onDischarge?.Raise();
        }

        public void Reset()
        {
            _currentVoltage    = 0f;
            _dischargeCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
