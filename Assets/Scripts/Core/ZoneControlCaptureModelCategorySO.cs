using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureModelCategory", order = 464)]
    public sealed class ZoneControlCaptureModelCategorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _weakEquivNeeded  = 6;
        [SerializeField, Min(1)] private int _breakPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerLocalize = 3700;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onModelCategoryLocalized;

        private int _weakEquivs;
        private int _localizeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   WeakEquivNeeded    => _weakEquivNeeded;
        public int   BreakPerBot        => _breakPerBot;
        public int   BonusPerLocalize   => _bonusPerLocalize;
        public int   WeakEquivs         => _weakEquivs;
        public int   LocalizeCount      => _localizeCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float WeakEquivProgress  => _weakEquivNeeded > 0
            ? Mathf.Clamp01(_weakEquivs / (float)_weakEquivNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _weakEquivs = Mathf.Min(_weakEquivs + 1, _weakEquivNeeded);
            if (_weakEquivs >= _weakEquivNeeded)
            {
                int bonus = _bonusPerLocalize;
                _localizeCount++;
                _totalBonusAwarded += bonus;
                _weakEquivs         = 0;
                _onModelCategoryLocalized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _weakEquivs = Mathf.Max(0, _weakEquivs - _breakPerBot);
        }

        public void Reset()
        {
            _weakEquivs        = 0;
            _localizeCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
