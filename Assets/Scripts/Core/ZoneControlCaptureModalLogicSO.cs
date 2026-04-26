using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureModalLogic", order = 547)]
    public sealed class ZoneControlCaptureModalLogicSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _possibleWorldsNeeded        = 6;
        [SerializeField, Min(1)] private int _accessibilityFailuresPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerCompletion          = 4945;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onModalLogicCompleted;

        private int _possibleWorlds;
        private int _completionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PossibleWorldsNeeded        => _possibleWorldsNeeded;
        public int   AccessibilityFailuresPerBot => _accessibilityFailuresPerBot;
        public int   BonusPerCompletion          => _bonusPerCompletion;
        public int   PossibleWorlds              => _possibleWorlds;
        public int   CompletionCount             => _completionCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float PossibleWorldProgress => _possibleWorldsNeeded > 0
            ? Mathf.Clamp01(_possibleWorlds / (float)_possibleWorldsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _possibleWorlds = Mathf.Min(_possibleWorlds + 1, _possibleWorldsNeeded);
            if (_possibleWorlds >= _possibleWorldsNeeded)
            {
                int bonus = _bonusPerCompletion;
                _completionCount++;
                _totalBonusAwarded += bonus;
                _possibleWorlds     = 0;
                _onModalLogicCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _possibleWorlds = Mathf.Max(0, _possibleWorlds - _accessibilityFailuresPerBot);
        }

        public void Reset()
        {
            _possibleWorlds    = 0;
            _completionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
