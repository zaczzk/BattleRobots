using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCantorDiagonal", order = 536)]
    public sealed class ZoneControlCaptureCantorDiagonalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _diagonalConstructionsNeeded = 6;
        [SerializeField, Min(1)] private int _enumerationAttemptsPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerDiagonal            = 4780;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCantorDiagonalConstructed;

        private int _diagonalConstructions;
        private int _diagonalCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DiagonalConstructionsNeeded => _diagonalConstructionsNeeded;
        public int   EnumerationAttemptsPerBot   => _enumerationAttemptsPerBot;
        public int   BonusPerDiagonal            => _bonusPerDiagonal;
        public int   DiagonalConstructions       => _diagonalConstructions;
        public int   DiagonalCount               => _diagonalCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float DiagonalConstructionProgress => _diagonalConstructionsNeeded > 0
            ? Mathf.Clamp01(_diagonalConstructions / (float)_diagonalConstructionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _diagonalConstructions = Mathf.Min(_diagonalConstructions + 1, _diagonalConstructionsNeeded);
            if (_diagonalConstructions >= _diagonalConstructionsNeeded)
            {
                int bonus = _bonusPerDiagonal;
                _diagonalCount++;
                _totalBonusAwarded     += bonus;
                _diagonalConstructions  = 0;
                _onCantorDiagonalConstructed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _diagonalConstructions = Mathf.Max(0, _diagonalConstructions - _enumerationAttemptsPerBot);
        }

        public void Reset()
        {
            _diagonalConstructions = 0;
            _diagonalCount         = 0;
            _totalBonusAwarded     = 0;
        }
    }
}
