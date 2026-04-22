using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInterrupt", order = 342)]
    public sealed class ZoneControlCaptureInterruptSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _irqsNeeded    = 6;
        [SerializeField, Min(1)] private int _maskPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerISR   = 1870;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInterruptHandled;

        private int _irqs;
        private int _isrCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   IrqsNeeded        => _irqsNeeded;
        public int   MaskPerBot        => _maskPerBot;
        public int   BonusPerISR       => _bonusPerISR;
        public int   Irqs              => _irqs;
        public int   IsrCount          => _isrCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float IrqProgress       => _irqsNeeded > 0
            ? Mathf.Clamp01(_irqs / (float)_irqsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _irqs = Mathf.Min(_irqs + 1, _irqsNeeded);
            if (_irqs >= _irqsNeeded)
            {
                int bonus = _bonusPerISR;
                _isrCount++;
                _totalBonusAwarded += bonus;
                _irqs               = 0;
                _onInterruptHandled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _irqs = Mathf.Max(0, _irqs - _maskPerBot);
        }

        public void Reset()
        {
            _irqs              = 0;
            _isrCount          = 0;
            _totalBonusAwarded = 0;
        }
    }
}
