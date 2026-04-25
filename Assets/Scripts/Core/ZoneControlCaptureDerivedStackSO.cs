using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDerivedStack", order = 496)]
    public sealed class ZoneControlCaptureDerivedStackSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _derivedThickeningsNeeded    = 6;
        [SerializeField, Min(1)] private int _obstructionTheoriesPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerResolution          = 4180;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDerivedStackResolved;

        private int _derivedThickenings;
        private int _resolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DerivedThickeningsNeeded  => _derivedThickeningsNeeded;
        public int   ObstructionTheoriesPerBot => _obstructionTheoriesPerBot;
        public int   BonusPerResolution        => _bonusPerResolution;
        public int   DerivedThickenings        => _derivedThickenings;
        public int   ResolutionCount           => _resolutionCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float DerivedThickeningProgress => _derivedThickeningsNeeded > 0
            ? Mathf.Clamp01(_derivedThickenings / (float)_derivedThickeningsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _derivedThickenings = Mathf.Min(_derivedThickenings + 1, _derivedThickeningsNeeded);
            if (_derivedThickenings >= _derivedThickeningsNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolutionCount++;
                _totalBonusAwarded  += bonus;
                _derivedThickenings  = 0;
                _onDerivedStackResolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _derivedThickenings = Mathf.Max(0, _derivedThickenings - _obstructionTheoriesPerBot);
        }

        public void Reset()
        {
            _derivedThickenings = 0;
            _resolutionCount    = 0;
            _totalBonusAwarded  = 0;
        }
    }
}
