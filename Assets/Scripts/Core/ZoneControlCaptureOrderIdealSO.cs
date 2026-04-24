using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOrderIdeal", order = 441)]
    public sealed class ZoneControlCaptureOrderIdealSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _idealsNeeded      = 7;
        [SerializeField, Min(1)] private int _shrinkPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerExtension = 3355;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOrderIdealExtended;

        private int _ideals;
        private int _extensionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   IdealsNeeded      => _idealsNeeded;
        public int   ShrinkPerBot      => _shrinkPerBot;
        public int   BonusPerExtension => _bonusPerExtension;
        public int   Ideals            => _ideals;
        public int   ExtensionCount    => _extensionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float IdealProgress     => _idealsNeeded > 0
            ? Mathf.Clamp01(_ideals / (float)_idealsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ideals = Mathf.Min(_ideals + 1, _idealsNeeded);
            if (_ideals >= _idealsNeeded)
            {
                int bonus = _bonusPerExtension;
                _extensionCount++;
                _totalBonusAwarded += bonus;
                _ideals             = 0;
                _onOrderIdealExtended?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ideals = Mathf.Max(0, _ideals - _shrinkPerBot);
        }

        public void Reset()
        {
            _ideals            = 0;
            _extensionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
