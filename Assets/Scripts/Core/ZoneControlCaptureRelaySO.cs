using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRelay", order = 324)]
    public sealed class ZoneControlCaptureRelaySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _closuresNeeded = 5;
        [SerializeField, Min(1)] private int _openPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerTrip   = 1600;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRelayTripped;

        private int _closures;
        private int _tripCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ClosuresNeeded    => _closuresNeeded;
        public int   OpenPerBot        => _openPerBot;
        public int   BonusPerTrip      => _bonusPerTrip;
        public int   Closures          => _closures;
        public int   TripCount         => _tripCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ClosureProgress   => _closuresNeeded > 0
            ? Mathf.Clamp01(_closures / (float)_closuresNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _closures = Mathf.Min(_closures + 1, _closuresNeeded);
            if (_closures >= _closuresNeeded)
            {
                int bonus = _bonusPerTrip;
                _tripCount++;
                _totalBonusAwarded += bonus;
                _closures           = 0;
                _onRelayTripped?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _closures = Mathf.Max(0, _closures - _openPerBot);
        }

        public void Reset()
        {
            _closures          = 0;
            _tripCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
