using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCondenser", order = 308)]
    public sealed class ZoneControlCaptureCondenserSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _platesNeeded       = 6;
        [SerializeField, Min(1)] private int _dischargePerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerCondensation = 1360;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCondenserCharged;

        private int _plates;
        private int _chargeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PlatesNeeded        => _platesNeeded;
        public int   DischargePerBot     => _dischargePerBot;
        public int   BonusPerCondensation => _bonusPerCondensation;
        public int   Plates              => _plates;
        public int   ChargeCount         => _chargeCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float PlateProgress       => _platesNeeded > 0
            ? Mathf.Clamp01(_plates / (float)_platesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _plates = Mathf.Min(_plates + 1, _platesNeeded);
            if (_plates >= _platesNeeded)
            {
                int bonus = _bonusPerCondensation;
                _chargeCount++;
                _totalBonusAwarded += bonus;
                _plates             = 0;
                _onCondenserCharged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _plates = Mathf.Max(0, _plates - _dischargePerBot);
        }

        public void Reset()
        {
            _plates            = 0;
            _chargeCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
