using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLinearType", order = 555)]
    public sealed class ZoneControlCaptureLinearTypeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _linearUsesNeeded          = 6;
        [SerializeField, Min(1)] private int _resourceDuplicationsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerLinearUse         = 5065;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLinearTypeCompleted;

        private int _linearUses;
        private int _linearUseCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LinearUsesNeeded           => _linearUsesNeeded;
        public int   ResourceDuplicationsPerBot => _resourceDuplicationsPerBot;
        public int   BonusPerLinearUse          => _bonusPerLinearUse;
        public int   LinearUses                 => _linearUses;
        public int   LinearUseCount             => _linearUseCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float LinearUseProgress => _linearUsesNeeded > 0
            ? Mathf.Clamp01(_linearUses / (float)_linearUsesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _linearUses = Mathf.Min(_linearUses + 1, _linearUsesNeeded);
            if (_linearUses >= _linearUsesNeeded)
            {
                int bonus = _bonusPerLinearUse;
                _linearUseCount++;
                _totalBonusAwarded += bonus;
                _linearUses         = 0;
                _onLinearTypeCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _linearUses = Mathf.Max(0, _linearUses - _resourceDuplicationsPerBot);
        }

        public void Reset()
        {
            _linearUses        = 0;
            _linearUseCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
