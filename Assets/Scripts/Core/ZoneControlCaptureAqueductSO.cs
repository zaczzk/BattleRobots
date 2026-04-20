using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAqueduct", order = 235)]
    public sealed class ZoneControlCaptureAqueductSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesForFlow = 5;
        [SerializeField, Min(1)] private int _drainPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerFlow    = 450;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFlow;

        private int _waterLevel;
        private int _flowCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesForFlow    => _capturesForFlow;
        public int   DrainPerBot        => _drainPerBot;
        public int   BonusPerFlow       => _bonusPerFlow;
        public int   WaterLevel         => _waterLevel;
        public int   FlowCount          => _flowCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float WaterProgress      => _capturesForFlow > 0
            ? Mathf.Clamp01(_waterLevel / (float)_capturesForFlow)
            : 0f;

        public int RecordPlayerCapture()
        {
            _waterLevel = Mathf.Min(_waterLevel + 1, _capturesForFlow);
            if (_waterLevel >= _capturesForFlow)
            {
                int bonus = _bonusPerFlow;
                _flowCount++;
                _totalBonusAwarded += bonus;
                _waterLevel         = 0;
                _onFlow?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _waterLevel = Mathf.Max(0, _waterLevel - _drainPerBot);
        }

        public void Reset()
        {
            _waterLevel        = 0;
            _flowCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
