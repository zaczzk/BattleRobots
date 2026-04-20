using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFlux", order = 201)]
    public sealed class ZoneControlCaptureFluxSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _minGapSeconds  = 3f;
        [SerializeField, Min(1f)]   private float _maxGapSeconds  = 20f;
        [SerializeField, Min(0)]    private int   _bonusPerSecond = 15;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFlux;

        private float _lastPlayerCaptureTime = -1f;
        private int   _fluxCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float MinGapSeconds      => _minGapSeconds;
        public float MaxGapSeconds      => _maxGapSeconds;
        public int   BonusPerSecond     => _bonusPerSecond;
        public int   FluxCount          => _fluxCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public bool  HasPriorCapture    => _lastPlayerCaptureTime >= 0f;

        public int RecordPlayerCapture(float t)
        {
            if (_lastPlayerCaptureTime < 0f)
            {
                _lastPlayerCaptureTime = t;
                return 0;
            }

            float gap   = Mathf.Clamp(t - _lastPlayerCaptureTime, _minGapSeconds, _maxGapSeconds);
            int   bonus = Mathf.RoundToInt(gap * _bonusPerSecond);
            _lastPlayerCaptureTime = t;
            _fluxCount++;
            _totalBonusAwarded += bonus;
            _onFlux?.Raise();
            return bonus;
        }

        public void RecordBotCapture()
        {
            _lastPlayerCaptureTime = -1f;
        }

        public void Reset()
        {
            _lastPlayerCaptureTime = -1f;
            _fluxCount             = 0;
            _totalBonusAwarded     = 0;
        }
    }
}
