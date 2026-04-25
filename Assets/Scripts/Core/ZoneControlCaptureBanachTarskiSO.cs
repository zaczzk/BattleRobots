using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBanachTarski", order = 535)]
    public sealed class ZoneControlCaptureBanachTarskiSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _decompositionPiecesNeeded      = 5;
        [SerializeField, Min(1)] private int _unmeasurableObstructionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerParadox                = 4765;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBanachTarskiParadoxCompleted;

        private int _decompositionPieces;
        private int _paradoxCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DecompositionPiecesNeeded      => _decompositionPiecesNeeded;
        public int   UnmeasurableObstructionsPerBot => _unmeasurableObstructionsPerBot;
        public int   BonusPerParadox                => _bonusPerParadox;
        public int   DecompositionPieces            => _decompositionPieces;
        public int   ParadoxCount                   => _paradoxCount;
        public int   TotalBonusAwarded              => _totalBonusAwarded;
        public float DecompositionPieceProgress     => _decompositionPiecesNeeded > 0
            ? Mathf.Clamp01(_decompositionPieces / (float)_decompositionPiecesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _decompositionPieces = Mathf.Min(_decompositionPieces + 1, _decompositionPiecesNeeded);
            if (_decompositionPieces >= _decompositionPiecesNeeded)
            {
                int bonus = _bonusPerParadox;
                _paradoxCount++;
                _totalBonusAwarded  += bonus;
                _decompositionPieces = 0;
                _onBanachTarskiParadoxCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _decompositionPieces = Mathf.Max(0, _decompositionPieces - _unmeasurableObstructionsPerBot);
        }

        public void Reset()
        {
            _decompositionPieces = 0;
            _paradoxCount        = 0;
            _totalBonusAwarded   = 0;
        }
    }
}
