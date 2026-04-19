using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureChrono", order = 186)]
    public sealed class ZoneControlCaptureChronoSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)] private float _minGapForBonus      = 2f;
        [SerializeField, Min(0f)] private float _maxGapForBonus      = 30f;
        [SerializeField, Min(0)]  private int   _bonusPerSecondOfGap = 20;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChronoRecord;

        private float _lastCaptureTime  = -1f;
        private float _bestGap;
        private int   _qualifyingCaptures;
        private int   _totalChronoBonus;

        private void OnEnable() => Reset();

        public float MinGapForBonus      => _minGapForBonus;
        public float MaxGapForBonus      => _maxGapForBonus;
        public int   BonusPerSecondOfGap => _bonusPerSecondOfGap;
        public float BestGap             => _bestGap;
        public int   QualifyingCaptures  => _qualifyingCaptures;
        public int   TotalChronoBonus    => _totalChronoBonus;
        public bool  HasFirstCapture     => _lastCaptureTime >= 0f;

        public int RecordCapture(float gameTime)
        {
            if (!HasFirstCapture)
            {
                _lastCaptureTime = gameTime;
                return 0;
            }

            float gap = gameTime - _lastCaptureTime;
            _lastCaptureTime = gameTime;

            if (gap < _minGapForBonus)
                return 0;

            float clampedGap = Mathf.Min(gap, _maxGapForBonus);
            int   bonus      = Mathf.RoundToInt(clampedGap * _bonusPerSecondOfGap);
            _totalChronoBonus  += bonus;
            _qualifyingCaptures++;

            if (gap > _bestGap)
            {
                _bestGap = gap;
                _onChronoRecord?.Raise();
            }

            return bonus;
        }

        public void Reset()
        {
            _lastCaptureTime    = -1f;
            _bestGap            = 0f;
            _qualifyingCaptures = 0;
            _totalChronoBonus   = 0;
        }
    }
}
