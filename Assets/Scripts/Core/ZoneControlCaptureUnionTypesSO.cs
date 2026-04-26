using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureUnionTypes", order = 565)]
    public sealed class ZoneControlCaptureUnionTypesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _variantsNeeded              = 6;
        [SerializeField, Min(1)] private int _discriminantErasuresPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerUnionElimination    = 5215;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onUnionTypesCompleted;

        private int _variants;
        private int _unionEliminationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VariantsNeeded             => _variantsNeeded;
        public int   DiscriminantErasuresPerBot  => _discriminantErasuresPerBot;
        public int   BonusPerUnionElimination   => _bonusPerUnionElimination;
        public int   Variants                   => _variants;
        public int   UnionEliminationCount      => _unionEliminationCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float VariantProgress => _variantsNeeded > 0
            ? Mathf.Clamp01(_variants / (float)_variantsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _variants = Mathf.Min(_variants + 1, _variantsNeeded);
            if (_variants >= _variantsNeeded)
            {
                int bonus = _bonusPerUnionElimination;
                _unionEliminationCount++;
                _totalBonusAwarded     += bonus;
                _variants               = 0;
                _onUnionTypesCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _variants = Mathf.Max(0, _variants - _discriminantErasuresPerBot);
        }

        public void Reset()
        {
            _variants              = 0;
            _unionEliminationCount = 0;
            _totalBonusAwarded     = 0;
        }
    }
}
