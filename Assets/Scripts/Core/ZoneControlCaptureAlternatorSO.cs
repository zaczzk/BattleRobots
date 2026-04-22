using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAlternator", order = 315)]
    public sealed class ZoneControlCaptureAlternatorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _rotationsNeeded = 5;
        [SerializeField, Min(1)] private int _dragPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerCycle   = 1465;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAlternatorCycled;

        private int _rotations;
        private int _cycleCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RotationsNeeded    => _rotationsNeeded;
        public int   DragPerBot         => _dragPerBot;
        public int   BonusPerCycle      => _bonusPerCycle;
        public int   Rotations          => _rotations;
        public int   CycleCount         => _cycleCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float RotationProgress   => _rotationsNeeded > 0
            ? Mathf.Clamp01(_rotations / (float)_rotationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _rotations = Mathf.Min(_rotations + 1, _rotationsNeeded);
            if (_rotations >= _rotationsNeeded)
            {
                int bonus = _bonusPerCycle;
                _cycleCount++;
                _totalBonusAwarded += bonus;
                _rotations          = 0;
                _onAlternatorCycled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _rotations = Mathf.Max(0, _rotations - _dragPerBot);
        }

        public void Reset()
        {
            _rotations         = 0;
            _cycleCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
