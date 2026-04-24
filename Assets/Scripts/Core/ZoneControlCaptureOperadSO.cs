using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOperad", order = 458)]
    public sealed class ZoneControlCaptureOperadSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _operationsNeeded = 6;
        [SerializeField, Min(1)] private int _collapsePerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerCompose  = 3610;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOperadComposed;

        private int _operations;
        private int _composeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OperationsNeeded  => _operationsNeeded;
        public int   CollapsePerBot    => _collapsePerBot;
        public int   BonusPerCompose   => _bonusPerCompose;
        public int   Operations        => _operations;
        public int   ComposeCount      => _composeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float OperationProgress => _operationsNeeded > 0
            ? Mathf.Clamp01(_operations / (float)_operationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _operations = Mathf.Min(_operations + 1, _operationsNeeded);
            if (_operations >= _operationsNeeded)
            {
                int bonus = _bonusPerCompose;
                _composeCount++;
                _totalBonusAwarded += bonus;
                _operations         = 0;
                _onOperadComposed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _operations = Mathf.Max(0, _operations - _collapsePerBot);
        }

        public void Reset()
        {
            _operations        = 0;
            _composeCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
