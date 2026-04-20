using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCairn", order = 245)]
    public sealed class ZoneControlCaptureCairnSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stonesNeeded    = 6;
        [SerializeField, Min(1)] private int _knockdownPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerCairn   = 470;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCairnComplete;

        private int _stones;
        private int _cairnCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StonesNeeded      => _stonesNeeded;
        public int   KnockdownPerBot   => _knockdownPerBot;
        public int   BonusPerCairn     => _bonusPerCairn;
        public int   Stones            => _stones;
        public int   CairnCount        => _cairnCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StoneProgress     => _stonesNeeded > 0
            ? Mathf.Clamp01(_stones / (float)_stonesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stones = Mathf.Min(_stones + 1, _stonesNeeded);
            if (_stones >= _stonesNeeded)
            {
                int bonus = _bonusPerCairn;
                _cairnCount++;
                _totalBonusAwarded += bonus;
                _stones             = 0;
                _onCairnComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stones = Mathf.Max(0, _stones - _knockdownPerBot);
        }

        public void Reset()
        {
            _stones            = 0;
            _cairnCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
