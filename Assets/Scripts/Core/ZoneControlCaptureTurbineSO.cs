using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTurbine", order = 304)]
    public sealed class ZoneControlCaptureTurbineSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bladesNeeded = 6;
        [SerializeField, Min(1)] private int _stallPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerSpin = 1300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTurbineSpun;

        private int _blades;
        private int _spinCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BladesNeeded      => _bladesNeeded;
        public int   StallPerBot       => _stallPerBot;
        public int   BonusPerSpin      => _bonusPerSpin;
        public int   Blades            => _blades;
        public int   SpinCount         => _spinCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BladeProgress     => _bladesNeeded > 0
            ? Mathf.Clamp01(_blades / (float)_bladesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _blades = Mathf.Min(_blades + 1, _bladesNeeded);
            if (_blades >= _bladesNeeded)
            {
                int bonus = _bonusPerSpin;
                _spinCount++;
                _totalBonusAwarded += bonus;
                _blades             = 0;
                _onTurbineSpun?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _blades = Mathf.Max(0, _blades - _stallPerBot);
        }

        public void Reset()
        {
            _blades            = 0;
            _spinCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
