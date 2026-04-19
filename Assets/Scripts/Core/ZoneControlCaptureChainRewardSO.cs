using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureChainReward", order = 149)]
    public sealed class ZoneControlCaptureChainRewardSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _chainTarget    = 5;
        [SerializeField, Min(0)] private int _bonusPerChain  = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChainCompleted;

        private int _currentChain;
        private int _chainCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChainTarget       => _chainTarget;
        public int   BonusPerChain     => _bonusPerChain;
        public int   CurrentChain      => _currentChain;
        public int   ChainCount        => _chainCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ChainProgress     => _chainTarget > 0
            ? Mathf.Clamp01((float)_currentChain / _chainTarget)
            : 1f;

        public void RecordPlayerCapture()
        {
            _currentChain++;
            if (_currentChain < _chainTarget)
                return;

            _chainCount++;
            _totalBonusAwarded += _bonusPerChain;
            _currentChain       = 0;
            _onChainCompleted?.Raise();
        }

        public void BreakChain()
        {
            _currentChain = 0;
        }

        public void Reset()
        {
            _currentChain      = 0;
            _chainCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
