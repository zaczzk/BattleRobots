using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBraided", order = 425)]
    public sealed class ZoneControlCaptureBraidedSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _braidsNeeded   = 5;
        [SerializeField, Min(1)] private int _unbraidPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerBraid  = 3115;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBraided;

        private int _braids;
        private int _braidCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BraidsNeeded      => _braidsNeeded;
        public int   UnbraidPerBot     => _unbraidPerBot;
        public int   BonusPerBraid     => _bonusPerBraid;
        public int   Braids            => _braids;
        public int   BraidCount        => _braidCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BraidProgress     => _braidsNeeded > 0
            ? Mathf.Clamp01(_braids / (float)_braidsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _braids = Mathf.Min(_braids + 1, _braidsNeeded);
            if (_braids >= _braidsNeeded)
            {
                int bonus = _bonusPerBraid;
                _braidCount++;
                _totalBonusAwarded += bonus;
                _braids             = 0;
                _onBraided?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _braids = Mathf.Max(0, _braids - _unbraidPerBot);
        }

        public void Reset()
        {
            _braids            = 0;
            _braidCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
