using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBifunctor", order = 377)]
    public sealed class ZoneControlCaptureBifunctorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pairsNeeded   = 5;
        [SerializeField, Min(1)] private int _splitPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerBimap = 2395;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBifunctorBimapped;

        private int _pairs;
        private int _bimapCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PairsNeeded        => _pairsNeeded;
        public int   SplitPerBot        => _splitPerBot;
        public int   BonusPerBimap      => _bonusPerBimap;
        public int   Pairs              => _pairs;
        public int   BimapCount         => _bimapCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float PairProgress       => _pairsNeeded > 0
            ? Mathf.Clamp01(_pairs / (float)_pairsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pairs = Mathf.Min(_pairs + 1, _pairsNeeded);
            if (_pairs >= _pairsNeeded)
            {
                int bonus = _bonusPerBimap;
                _bimapCount++;
                _totalBonusAwarded += bonus;
                _pairs              = 0;
                _onBifunctorBimapped?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pairs = Mathf.Max(0, _pairs - _splitPerBot);
        }

        public void Reset()
        {
            _pairs             = 0;
            _bimapCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
