using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGeyser", order = 224)]
    public sealed class ZoneControlCaptureGeyserSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _capturesForEruption = 6;
        [SerializeField, Min(0)] private int _bonusPerEruption    = 220;
        [SerializeField, Min(1)] private int _maxEruptions        = 4;
        [SerializeField, Min(0)] private int _completionBonus     = 700;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEruption;
        [SerializeField] private VoidGameEvent _onGeyserComplete;

        private int  _buildCount;
        private int  _eruptionCount;
        private bool _isComplete;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  CapturesForEruption => _capturesForEruption;
        public int  BonusPerEruption    => _bonusPerEruption;
        public int  MaxEruptions        => _maxEruptions;
        public int  CompletionBonus     => _completionBonus;
        public int  BuildCount          => _buildCount;
        public int  EruptionCount       => _eruptionCount;
        public bool IsComplete          => _isComplete;
        public int  TotalBonusAwarded   => _totalBonusAwarded;
        public float BuildProgress      => _capturesForEruption > 0
            ? Mathf.Clamp01(_buildCount / (float)_capturesForEruption)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_isComplete) return 0;

            _buildCount++;
            if (_buildCount >= _capturesForEruption)
                return Erupt();

            return 0;
        }

        private int Erupt()
        {
            _eruptionCount++;
            _buildCount = 0;

            if (_eruptionCount >= _maxEruptions)
            {
                Complete();
                return _completionBonus;
            }

            _totalBonusAwarded += _bonusPerEruption;
            _onEruption?.Raise();
            return _bonusPerEruption;
        }

        private void Complete()
        {
            _isComplete         = true;
            _totalBonusAwarded += _completionBonus;
            _onGeyserComplete?.Raise();
        }

        public void RecordBotCapture()
        {
            _buildCount = Mathf.Max(0, _buildCount - 1);
        }

        public void Reset()
        {
            _buildCount        = 0;
            _eruptionCount     = 0;
            _isComplete        = false;
            _totalBonusAwarded = 0;
        }
    }
}
