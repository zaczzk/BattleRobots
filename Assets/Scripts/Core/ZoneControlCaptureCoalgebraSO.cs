using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCoalgebra", order = 383)]
    public sealed class ZoneControlCaptureCoalgebraSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _statesNeeded   = 5;
        [SerializeField, Min(1)] private int _dissolvePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerUnfold = 2485;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCoalgebraUnfolded;

        private int _states;
        private int _unfoldCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StatesNeeded     => _statesNeeded;
        public int   DissolvePerBot   => _dissolvePerBot;
        public int   BonusPerUnfold   => _bonusPerUnfold;
        public int   States           => _states;
        public int   UnfoldCount      => _unfoldCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StateProgress    => _statesNeeded > 0
            ? Mathf.Clamp01(_states / (float)_statesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _states = Mathf.Min(_states + 1, _statesNeeded);
            if (_states >= _statesNeeded)
            {
                int bonus = _bonusPerUnfold;
                _unfoldCount++;
                _totalBonusAwarded += bonus;
                _states             = 0;
                _onCoalgebraUnfolded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _states = Mathf.Max(0, _states - _dissolvePerBot);
        }

        public void Reset()
        {
            _states            = 0;
            _unfoldCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
