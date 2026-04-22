using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMonad", order = 365)]
    public sealed class ZoneControlCaptureMonadSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _operationsNeeded = 5;
        [SerializeField, Min(1)] private int _abortPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerChain    = 2215;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMonadChained;

        private int _operations;
        private int _chainCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OperationsNeeded   => _operationsNeeded;
        public int   AbortPerBot        => _abortPerBot;
        public int   BonusPerChain      => _bonusPerChain;
        public int   Operations         => _operations;
        public int   ChainCount         => _chainCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float OperationProgress  => _operationsNeeded > 0
            ? Mathf.Clamp01(_operations / (float)_operationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _operations = Mathf.Min(_operations + 1, _operationsNeeded);
            if (_operations >= _operationsNeeded)
            {
                int bonus = _bonusPerChain;
                _chainCount++;
                _totalBonusAwarded += bonus;
                _operations         = 0;
                _onMonadChained?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _operations = Mathf.Max(0, _operations - _abortPerBot);
        }

        public void Reset()
        {
            _operations        = 0;
            _chainCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
