using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSessionTypes", order = 561)]
    public sealed class ZoneControlCaptureSessionTypesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _protocolStepsNeeded      = 6;
        [SerializeField, Min(1)] private int _protocolViolationsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerProtocolStep     = 5155;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSessionTypesCompleted;

        private int _protocolSteps;
        private int _sessionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ProtocolStepsNeeded      => _protocolStepsNeeded;
        public int   ProtocolViolationsPerBot => _protocolViolationsPerBot;
        public int   BonusPerProtocolStep     => _bonusPerProtocolStep;
        public int   ProtocolSteps            => _protocolSteps;
        public int   SessionCount             => _sessionCount;
        public int   TotalBonusAwarded        => _totalBonusAwarded;
        public float ProtocolStepProgress => _protocolStepsNeeded > 0
            ? Mathf.Clamp01(_protocolSteps / (float)_protocolStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _protocolSteps = Mathf.Min(_protocolSteps + 1, _protocolStepsNeeded);
            if (_protocolSteps >= _protocolStepsNeeded)
            {
                int bonus = _bonusPerProtocolStep;
                _sessionCount++;
                _totalBonusAwarded += bonus;
                _protocolSteps      = 0;
                _onSessionTypesCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _protocolSteps = Mathf.Max(0, _protocolSteps - _protocolViolationsPerBot);
        }

        public void Reset()
        {
            _protocolSteps     = 0;
            _sessionCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
