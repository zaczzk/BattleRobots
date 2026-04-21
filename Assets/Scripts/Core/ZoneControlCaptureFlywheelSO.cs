using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFlywheel", order = 298)]
    public sealed class ZoneControlCaptureFlywheelSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _turnsNeeded       = 7;
        [SerializeField, Min(1)] private int _dragPerBot        = 2;
        [SerializeField, Min(0)] private int _bonusPerRevolution = 1210;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFlywheelRevolved;

        private int _turns;
        private int _revolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TurnsNeeded        => _turnsNeeded;
        public int   DragPerBot         => _dragPerBot;
        public int   BonusPerRevolution => _bonusPerRevolution;
        public int   Turns              => _turns;
        public int   RevolutionCount    => _revolutionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float TurnProgress       => _turnsNeeded > 0
            ? Mathf.Clamp01(_turns / (float)_turnsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _turns = Mathf.Min(_turns + 1, _turnsNeeded);
            if (_turns >= _turnsNeeded)
            {
                int bonus = _bonusPerRevolution;
                _revolutionCount++;
                _totalBonusAwarded += bonus;
                _turns              = 0;
                _onFlywheelRevolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _turns = Mathf.Max(0, _turns - _dragPerBot);
        }

        public void Reset()
        {
            _turns             = 0;
            _revolutionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
