using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNebula", order = 221)]
    public sealed class ZoneControlCaptureNebulaSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)]         private float _densityPerCapture  = 15f;
        [SerializeField, Min(1f)]            private float _maxDensity         = 100f;
        [SerializeField, Range(0f, 1f)]      private float _payoutFraction     = 0.8f;
        [SerializeField, Min(0)]             private int   _bonusPerDensityUnit = 4;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNebulaDispersed;

        private float _density;
        private int   _dispersalCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float DensityPerCapture   => _densityPerCapture;
        public float MaxDensity          => _maxDensity;
        public float PayoutFraction      => _payoutFraction;
        public int   BonusPerDensityUnit => _bonusPerDensityUnit;
        public float CurrentDensity      => _density;
        public int   DispersalCount      => _dispersalCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float NebulaProgress      => _maxDensity > 0f
            ? Mathf.Clamp01(_density / _maxDensity)
            : 0f;

        public void RecordPlayerCapture()
        {
            _density = Mathf.Min(_density + _densityPerCapture, _maxDensity);
        }

        public int RecordBotCapture()
        {
            if (_density <= 0f)
                return 0;

            int payout = Mathf.RoundToInt(_density * _payoutFraction * _bonusPerDensityUnit);
            _density            = 0f;
            _dispersalCount++;
            _totalBonusAwarded += payout;
            _onNebulaDispersed?.Raise();
            return payout;
        }

        public void Reset()
        {
            _density           = 0f;
            _dispersalCount    = 0;
            _totalBonusAwarded = 0;
        }

        private void OnValidate()
        {
            _payoutFraction = Mathf.Clamp01(_payoutFraction);
        }
    }
}
