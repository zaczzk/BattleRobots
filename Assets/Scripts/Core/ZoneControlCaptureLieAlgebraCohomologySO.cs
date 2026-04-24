using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLieAlgebraCohomology", order = 476)]
    public sealed class ZoneControlCaptureLieAlgebraCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chainsNeeded       = 6;
        [SerializeField, Min(1)] private int _boundaryPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerReduction  = 3880;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLieAlgebraCohomologyReduced;

        private int _chains;
        private int _reduceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChainsNeeded      => _chainsNeeded;
        public int   BoundaryPerBot    => _boundaryPerBot;
        public int   BonusPerReduction => _bonusPerReduction;
        public int   Chains            => _chains;
        public int   ReduceCount       => _reduceCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ChainProgress     => _chainsNeeded > 0
            ? Mathf.Clamp01(_chains / (float)_chainsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _chains = Mathf.Min(_chains + 1, _chainsNeeded);
            if (_chains >= _chainsNeeded)
            {
                int bonus = _bonusPerReduction;
                _reduceCount++;
                _totalBonusAwarded += bonus;
                _chains             = 0;
                _onLieAlgebraCohomologyReduced?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _chains = Mathf.Max(0, _chains - _boundaryPerBot);
        }

        public void Reset()
        {
            _chains            = 0;
            _reduceCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
