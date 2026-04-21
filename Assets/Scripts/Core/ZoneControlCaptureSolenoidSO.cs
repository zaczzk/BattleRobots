using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSolenoid", order = 311)]
    public sealed class ZoneControlCaptureSolenoidSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _plungersNeeded    = 5;
        [SerializeField, Min(1)] private int _retractPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerActuation = 1405;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSolenoidActuated;

        private int _plungers;
        private int _actuationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PlungersNeeded    => _plungersNeeded;
        public int   RetractPerBot     => _retractPerBot;
        public int   BonusPerActuation => _bonusPerActuation;
        public int   Plungers          => _plungers;
        public int   ActuationCount    => _actuationCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PlungerProgress   => _plungersNeeded > 0
            ? Mathf.Clamp01(_plungers / (float)_plungersNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _plungers = Mathf.Min(_plungers + 1, _plungersNeeded);
            if (_plungers >= _plungersNeeded)
            {
                int bonus = _bonusPerActuation;
                _actuationCount++;
                _totalBonusAwarded += bonus;
                _plungers           = 0;
                _onSolenoidActuated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _plungers = Mathf.Max(0, _plungers - _retractPerBot);
        }

        public void Reset()
        {
            _plungers          = 0;
            _actuationCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
