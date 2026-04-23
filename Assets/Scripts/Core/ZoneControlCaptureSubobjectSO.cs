using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSubobject", order = 412)]
    public sealed class ZoneControlCaptureSubobjectSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _inclusionsNeeded  = 6;
        [SerializeField, Min(1)] private int _excludePerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerSubobject = 2920;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSubobjectClassified;

        private int _inclusions;
        private int _subobjectCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   InclusionsNeeded  => _inclusionsNeeded;
        public int   ExcludePerBot     => _excludePerBot;
        public int   BonusPerSubobject => _bonusPerSubobject;
        public int   Inclusions        => _inclusions;
        public int   SubobjectCount    => _subobjectCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float InclusionProgress => _inclusionsNeeded > 0
            ? Mathf.Clamp01(_inclusions / (float)_inclusionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _inclusions = Mathf.Min(_inclusions + 1, _inclusionsNeeded);
            if (_inclusions >= _inclusionsNeeded)
            {
                int bonus = _bonusPerSubobject;
                _subobjectCount++;
                _totalBonusAwarded += bonus;
                _inclusions         = 0;
                _onSubobjectClassified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _inclusions = Mathf.Max(0, _inclusions - _excludePerBot);
        }

        public void Reset()
        {
            _inclusions        = 0;
            _subobjectCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
