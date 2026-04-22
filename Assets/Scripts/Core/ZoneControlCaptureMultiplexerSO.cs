using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMultiplexer", order = 326)]
    public sealed class ZoneControlCaptureMultiplexerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _inputsNeeded  = 5;
        [SerializeField, Min(1)] private int _dropPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerRoute = 1630;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMultiplexerRouted;

        private int _inputs;
        private int _routeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   InputsNeeded      => _inputsNeeded;
        public int   DropPerBot        => _dropPerBot;
        public int   BonusPerRoute     => _bonusPerRoute;
        public int   Inputs            => _inputs;
        public int   RouteCount        => _routeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float InputProgress     => _inputsNeeded > 0
            ? Mathf.Clamp01(_inputs / (float)_inputsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _inputs = Mathf.Min(_inputs + 1, _inputsNeeded);
            if (_inputs >= _inputsNeeded)
            {
                int bonus = _bonusPerRoute;
                _routeCount++;
                _totalBonusAwarded += bonus;
                _inputs             = 0;
                _onMultiplexerRouted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _inputs = Mathf.Max(0, _inputs - _dropPerBot);
        }

        public void Reset()
        {
            _inputs            = 0;
            _routeCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
