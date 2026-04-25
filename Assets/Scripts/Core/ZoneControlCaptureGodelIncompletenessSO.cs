using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGodelIncompleteness", order = 526)]
    public sealed class ZoneControlCaptureGodelIncompletenessSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _consistentExtensionsNeeded = 6;
        [SerializeField, Min(1)] private int _godelSentencesPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerExtension          = 4630;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onExtensionCompleted;

        private int _consistentExtensions;
        private int _extensionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ConsistentExtensionsNeeded => _consistentExtensionsNeeded;
        public int   GodelSentencesPerBot       => _godelSentencesPerBot;
        public int   BonusPerExtension          => _bonusPerExtension;
        public int   ConsistentExtensions       => _consistentExtensions;
        public int   ExtensionCount             => _extensionCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float ExtensionProgress          => _consistentExtensionsNeeded > 0
            ? Mathf.Clamp01(_consistentExtensions / (float)_consistentExtensionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _consistentExtensions = Mathf.Min(_consistentExtensions + 1, _consistentExtensionsNeeded);
            if (_consistentExtensions >= _consistentExtensionsNeeded)
            {
                int bonus = _bonusPerExtension;
                _extensionCount++;
                _totalBonusAwarded    += bonus;
                _consistentExtensions  = 0;
                _onExtensionCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _consistentExtensions = Mathf.Max(0, _consistentExtensions - _godelSentencesPerBot);
        }

        public void Reset()
        {
            _consistentExtensions = 0;
            _extensionCount       = 0;
            _totalBonusAwarded    = 0;
        }
    }
}
