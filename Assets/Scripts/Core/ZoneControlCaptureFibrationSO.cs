using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFibration", order = 387)]
    public sealed class ZoneControlCaptureFibrationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _fibersNeeded    = 6;
        [SerializeField, Min(1)] private int _unravelPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerLift    = 2545;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFibrationLifted;

        private int _fibers;
        private int _liftCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FibersNeeded      => _fibersNeeded;
        public int   UnravelPerBot     => _unravelPerBot;
        public int   BonusPerLift      => _bonusPerLift;
        public int   Fibers            => _fibers;
        public int   LiftCount         => _liftCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float FiberProgress     => _fibersNeeded > 0
            ? Mathf.Clamp01(_fibers / (float)_fibersNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _fibers = Mathf.Min(_fibers + 1, _fibersNeeded);
            if (_fibers >= _fibersNeeded)
            {
                int bonus = _bonusPerLift;
                _liftCount++;
                _totalBonusAwarded += bonus;
                _fibers             = 0;
                _onFibrationLifted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _fibers = Mathf.Max(0, _fibers - _unravelPerBot);
        }

        public void Reset()
        {
            _fibers            = 0;
            _liftCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
