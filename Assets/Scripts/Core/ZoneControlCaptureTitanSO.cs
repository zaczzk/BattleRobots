using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTitan", order = 222)]
    public sealed class ZoneControlCaptureTitanSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesForRise    = 4;
        [SerializeField, Min(1)] private int _maxTitans          = 3;
        [SerializeField, Min(0)] private int _bonusPerRise       = 150;
        [SerializeField, Min(0)] private int _completionBonus    = 600;
        [SerializeField, Min(1)] private int _drainPerBotCapture = 1;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTitanRisen;
        [SerializeField] private VoidGameEvent _onTitanComplete;

        private int _buildCount;
        private int _titanCount;
        private int _completionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  CapturesForRise    => _capturesForRise;
        public int  MaxTitans          => _maxTitans;
        public int  BonusPerRise       => _bonusPerRise;
        public int  CompletionBonus    => _completionBonus;
        public int  DrainPerBotCapture => _drainPerBotCapture;
        public int  BuildCount         => _buildCount;
        public int  TitanCount         => _titanCount;
        public int  CompletionCount    => _completionCount;
        public int  TotalBonusAwarded  => _totalBonusAwarded;
        public float BuildProgress     => _capturesForRise > 0
            ? Mathf.Clamp01(_buildCount / (float)_capturesForRise)
            : 0f;

        public int RecordPlayerCapture()
        {
            _buildCount++;
            if (_buildCount >= _capturesForRise && _titanCount < _maxTitans)
            {
                _buildCount = 0;
                _titanCount++;
                _totalBonusAwarded += _bonusPerRise;
                _onTitanRisen?.Raise();

                if (_titanCount >= _maxTitans)
                    return Complete();

                return _bonusPerRise;
            }
            return 0;
        }

        private int Complete()
        {
            _completionCount++;
            _totalBonusAwarded += _completionBonus;
            _titanCount         = 0;
            _onTitanComplete?.Raise();
            return _completionBonus;
        }

        public void RecordBotCapture()
        {
            _buildCount = Mathf.Max(0, _buildCount - _drainPerBotCapture);
        }

        public void Reset()
        {
            _buildCount        = 0;
            _titanCount        = 0;
            _completionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
