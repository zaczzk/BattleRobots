using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDerivedCategory", order = 462)]
    public sealed class ZoneControlCaptureDerivedCategorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _quasiIsosNeeded  = 5;
        [SerializeField, Min(1)] private int _introducePerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerDerive   = 3670;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDerivedCategoryDerived;

        private int _quasiIsos;
        private int _deriveCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   QuasiIsosNeeded   => _quasiIsosNeeded;
        public int   IntroducePerBot   => _introducePerBot;
        public int   BonusPerDerive    => _bonusPerDerive;
        public int   QuasiIsos         => _quasiIsos;
        public int   DeriveCount       => _deriveCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float QuasiIsoProgress  => _quasiIsosNeeded > 0
            ? Mathf.Clamp01(_quasiIsos / (float)_quasiIsosNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _quasiIsos = Mathf.Min(_quasiIsos + 1, _quasiIsosNeeded);
            if (_quasiIsos >= _quasiIsosNeeded)
            {
                int bonus = _bonusPerDerive;
                _deriveCount++;
                _totalBonusAwarded += bonus;
                _quasiIsos          = 0;
                _onDerivedCategoryDerived?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _quasiIsos = Mathf.Max(0, _quasiIsos - _introducePerBot);
        }

        public void Reset()
        {
            _quasiIsos         = 0;
            _deriveCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
