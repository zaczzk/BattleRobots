using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDual", order = 414)]
    public sealed class ZoneControlCaptureDualSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _reversalsNeeded = 5;
        [SerializeField, Min(1)] private int _flipPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerDual    = 2950;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDualFormed;

        private int _reversals;
        private int _dualCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ReversalsNeeded   => _reversalsNeeded;
        public int   FlipPerBot        => _flipPerBot;
        public int   BonusPerDual      => _bonusPerDual;
        public int   Reversals         => _reversals;
        public int   DualCount         => _dualCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ReversalProgress  => _reversalsNeeded > 0
            ? Mathf.Clamp01(_reversals / (float)_reversalsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _reversals = Mathf.Min(_reversals + 1, _reversalsNeeded);
            if (_reversals >= _reversalsNeeded)
            {
                int bonus = _bonusPerDual;
                _dualCount++;
                _totalBonusAwarded += bonus;
                _reversals          = 0;
                _onDualFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _reversals = Mathf.Max(0, _reversals - _flipPerBot);
        }

        public void Reset()
        {
            _reversals         = 0;
            _dualCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
