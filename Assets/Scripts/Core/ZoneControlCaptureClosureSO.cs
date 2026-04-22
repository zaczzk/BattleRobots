using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureClosure", order = 364)]
    public sealed class ZoneControlCaptureClosureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bindingsNeeded  = 6;
        [SerializeField, Min(1)] private int _unbindPerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerClosure = 2200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onClosureSealed;

        private int _bindings;
        private int _closureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BindingsNeeded     => _bindingsNeeded;
        public int   UnbindPerBot       => _unbindPerBot;
        public int   BonusPerClosure    => _bonusPerClosure;
        public int   Bindings           => _bindings;
        public int   ClosureCount       => _closureCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float BindingProgress    => _bindingsNeeded > 0
            ? Mathf.Clamp01(_bindings / (float)_bindingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bindings = Mathf.Min(_bindings + 1, _bindingsNeeded);
            if (_bindings >= _bindingsNeeded)
            {
                int bonus = _bonusPerClosure;
                _closureCount++;
                _totalBonusAwarded += bonus;
                _bindings           = 0;
                _onClosureSealed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bindings = Mathf.Max(0, _bindings - _unbindPerBot);
        }

        public void Reset()
        {
            _bindings          = 0;
            _closureCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
