using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCutElimination", order = 546)]
    public sealed class ZoneControlCaptureCutEliminationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cutFreeDerivationsNeeded = 6;
        [SerializeField, Min(1)] private int _cutRulesPerBot           = 1;
        [SerializeField, Min(0)] private int _bonusPerElimination      = 4930;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCutEliminationAchieved;

        private int _cutFreeDerivations;
        private int _eliminationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CutFreeDerivationsNeeded => _cutFreeDerivationsNeeded;
        public int   CutRulesPerBot           => _cutRulesPerBot;
        public int   BonusPerElimination      => _bonusPerElimination;
        public int   CutFreeDerivations       => _cutFreeDerivations;
        public int   EliminationCount         => _eliminationCount;
        public int   TotalBonusAwarded        => _totalBonusAwarded;
        public float CutFreeDerivationProgress => _cutFreeDerivationsNeeded > 0
            ? Mathf.Clamp01(_cutFreeDerivations / (float)_cutFreeDerivationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cutFreeDerivations = Mathf.Min(_cutFreeDerivations + 1, _cutFreeDerivationsNeeded);
            if (_cutFreeDerivations >= _cutFreeDerivationsNeeded)
            {
                int bonus = _bonusPerElimination;
                _eliminationCount++;
                _totalBonusAwarded  += bonus;
                _cutFreeDerivations  = 0;
                _onCutEliminationAchieved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cutFreeDerivations = Mathf.Max(0, _cutFreeDerivations - _cutRulesPerBot);
        }

        public void Reset()
        {
            _cutFreeDerivations = 0;
            _eliminationCount   = 0;
            _totalBonusAwarded  = 0;
        }
    }
}
