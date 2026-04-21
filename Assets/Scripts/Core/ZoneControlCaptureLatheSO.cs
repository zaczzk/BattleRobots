using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLathe", order = 287)]
    public sealed class ZoneControlCaptureLatheSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _turningsNeeded  = 5;
        [SerializeField, Min(1)] private int _shavingsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerSpin    = 1045;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLatheSpun;

        private int _turnings;
        private int _spinCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TurningsNeeded    => _turningsNeeded;
        public int   ShavingsPerBot    => _shavingsPerBot;
        public int   BonusPerSpin      => _bonusPerSpin;
        public int   Turnings          => _turnings;
        public int   SpinCount         => _spinCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TurningProgress   => _turningsNeeded > 0
            ? Mathf.Clamp01(_turnings / (float)_turningsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _turnings = Mathf.Min(_turnings + 1, _turningsNeeded);
            if (_turnings >= _turningsNeeded)
            {
                int bonus = _bonusPerSpin;
                _spinCount++;
                _totalBonusAwarded += bonus;
                _turnings           = 0;
                _onLatheSpun?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _turnings = Mathf.Max(0, _turnings - _shavingsPerBot);
        }

        public void Reset()
        {
            _turnings          = 0;
            _spinCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
