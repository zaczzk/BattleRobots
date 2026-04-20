using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureVolcano", order = 228)]
    public sealed class ZoneControlCaptureVolcanoSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _pressurePerCapture  = 20f;
        [SerializeField, Min(1f)]   private float _eruptionThreshold   = 100f;
        [SerializeField, Min(0.1f)] private float _coolingPerBot       = 15f;
        [SerializeField, Min(0)]    private int   _bonusPerEruption    = 425;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEruption;

        private float _pressure;
        private int   _eruptionCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float PressurePerCapture  => _pressurePerCapture;
        public float EruptionThreshold   => _eruptionThreshold;
        public float CoolingPerBot       => _coolingPerBot;
        public int   BonusPerEruption    => _bonusPerEruption;
        public float Pressure            => _pressure;
        public int   EruptionCount       => _eruptionCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float PressureProgress    => _eruptionThreshold > 0f
            ? Mathf.Clamp01(_pressure / _eruptionThreshold)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pressure = Mathf.Min(_pressure + _pressurePerCapture, _eruptionThreshold);
            if (_pressure >= _eruptionThreshold)
                return Erupt();
            return 0;
        }

        public void RecordBotCapture()
        {
            _pressure = Mathf.Max(0f, _pressure - _coolingPerBot);
        }

        private int Erupt()
        {
            _eruptionCount++;
            _totalBonusAwarded += _bonusPerEruption;
            _pressure           = 0f;
            _onEruption?.Raise();
            return _bonusPerEruption;
        }

        public void Reset()
        {
            _pressure          = 0f;
            _eruptionCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
