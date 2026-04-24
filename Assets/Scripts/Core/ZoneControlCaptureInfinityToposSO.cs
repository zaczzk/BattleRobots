using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInfinityTopos", order = 465)]
    public sealed class ZoneControlCaptureInfinityToposSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _descentConditionsNeeded = 5;
        [SerializeField, Min(1)] private int _breakPerBot             = 1;
        [SerializeField, Min(0)] private int _bonusPerDescend         = 3715;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInfinityToposDescended;

        private int _descentConditions;
        private int _descendCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DescentConditionsNeeded => _descentConditionsNeeded;
        public int   BreakPerBot             => _breakPerBot;
        public int   BonusPerDescend         => _bonusPerDescend;
        public int   DescentConditions       => _descentConditions;
        public int   DescendCount            => _descendCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float DescentProgress         => _descentConditionsNeeded > 0
            ? Mathf.Clamp01(_descentConditions / (float)_descentConditionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _descentConditions = Mathf.Min(_descentConditions + 1, _descentConditionsNeeded);
            if (_descentConditions >= _descentConditionsNeeded)
            {
                int bonus = _bonusPerDescend;
                _descendCount++;
                _totalBonusAwarded  += bonus;
                _descentConditions   = 0;
                _onInfinityToposDescended?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _descentConditions = Mathf.Max(0, _descentConditions - _breakPerBot);
        }

        public void Reset()
        {
            _descentConditions = 0;
            _descendCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
