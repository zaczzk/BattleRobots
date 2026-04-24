using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOpcart", order = 456)]
    public sealed class ZoneControlCaptureOpcartSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _liftsNeeded     = 7;
        [SerializeField, Min(1)] private int _obstructPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerLift    = 3580;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOpcartLifted;

        private int _lifts;
        private int _liftCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LiftsNeeded       => _liftsNeeded;
        public int   ObstructPerBot    => _obstructPerBot;
        public int   BonusPerLift      => _bonusPerLift;
        public int   Lifts             => _lifts;
        public int   LiftCount         => _liftCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LiftProgress      => _liftsNeeded > 0
            ? Mathf.Clamp01(_lifts / (float)_liftsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _lifts = Mathf.Min(_lifts + 1, _liftsNeeded);
            if (_lifts >= _liftsNeeded)
            {
                int bonus = _bonusPerLift;
                _liftCount++;
                _totalBonusAwarded += bonus;
                _lifts              = 0;
                _onOpcartLifted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _lifts = Mathf.Max(0, _lifts - _obstructPerBot);
        }

        public void Reset()
        {
            _lifts             = 0;
            _liftCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
