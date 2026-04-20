using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePillar", order = 233)]
    public sealed class ZoneControlCapturePillarSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesPerPillar = 4;
        [SerializeField, Min(1)] private int _maxPillars        = 4;
        [SerializeField, Min(0)] private int _bonusPerCapture   = 80;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStructureComplete;

        private int _buildCount;
        private int _pillarCount;
        private int _bonusCaptureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesPerPillar  => _capturesPerPillar;
        public int   MaxPillars         => _maxPillars;
        public int   BonusPerCapture    => _bonusPerCapture;
        public int   BuildCount         => _buildCount;
        public int   PillarCount        => _pillarCount;
        public int   BonusCaptureCount  => _bonusCaptureCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public bool  IsComplete         => _pillarCount >= _maxPillars;
        public float BuildProgress      => IsComplete
            ? 1f
            : (_capturesPerPillar > 0 ? Mathf.Clamp01(_buildCount / (float)_capturesPerPillar) : 0f);

        public int RecordPlayerCapture()
        {
            if (_pillarCount >= _maxPillars)
            {
                _bonusCaptureCount++;
                _totalBonusAwarded += _bonusPerCapture;
                return _bonusPerCapture;
            }

            _buildCount++;
            if (_buildCount >= _capturesPerPillar)
            {
                _buildCount = 0;
                _pillarCount++;
                if (_pillarCount >= _maxPillars)
                    _onStructureComplete?.Raise();
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            if (_pillarCount > 0)
            {
                _pillarCount--;
                _buildCount = 0;
            }
            else
            {
                _buildCount = Mathf.Max(0, _buildCount - 1);
            }
        }

        public void Reset()
        {
            _buildCount        = 0;
            _pillarCount       = 0;
            _bonusCaptureCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}
