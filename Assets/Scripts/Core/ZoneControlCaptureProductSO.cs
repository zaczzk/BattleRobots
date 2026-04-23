using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureProduct", order = 410)]
    public sealed class ZoneControlCaptureProductSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _factorsNeeded  = 5;
        [SerializeField, Min(1)] private int _splitPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerProduct = 2890;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onProductFormed;

        private int _factors;
        private int _productCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FactorsNeeded     => _factorsNeeded;
        public int   SplitPerBot       => _splitPerBot;
        public int   BonusPerProduct   => _bonusPerProduct;
        public int   Factors           => _factors;
        public int   ProductCount      => _productCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float FactorProgress    => _factorsNeeded > 0
            ? Mathf.Clamp01(_factors / (float)_factorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _factors = Mathf.Min(_factors + 1, _factorsNeeded);
            if (_factors >= _factorsNeeded)
            {
                int bonus = _bonusPerProduct;
                _productCount++;
                _totalBonusAwarded += bonus;
                _factors            = 0;
                _onProductFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _factors = Mathf.Max(0, _factors - _splitPerBot);
        }

        public void Reset()
        {
            _factors           = 0;
            _productCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
