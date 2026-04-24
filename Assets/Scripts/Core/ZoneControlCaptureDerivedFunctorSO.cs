using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDerivedFunctor", order = 463)]
    public sealed class ZoneControlCaptureDerivedFunctorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _resolutionsNeeded = 5;
        [SerializeField, Min(1)] private int _destroyPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerDerive    = 3685;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDerivedFunctorDerived;

        private int _resolutions;
        private int _deriveCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ResolutionsNeeded  => _resolutionsNeeded;
        public int   DestroyPerBot      => _destroyPerBot;
        public int   BonusPerDerive     => _bonusPerDerive;
        public int   Resolutions        => _resolutions;
        public int   DeriveCount        => _deriveCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ResolutionProgress => _resolutionsNeeded > 0
            ? Mathf.Clamp01(_resolutions / (float)_resolutionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _resolutions = Mathf.Min(_resolutions + 1, _resolutionsNeeded);
            if (_resolutions >= _resolutionsNeeded)
            {
                int bonus = _bonusPerDerive;
                _deriveCount++;
                _totalBonusAwarded += bonus;
                _resolutions        = 0;
                _onDerivedFunctorDerived?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _resolutions = Mathf.Max(0, _resolutions - _destroyPerBot);
        }

        public void Reset()
        {
            _resolutions       = 0;
            _deriveCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
