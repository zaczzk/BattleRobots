using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureKan", order = 382)]
    public sealed class ZoneControlCaptureKanSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _extensionsNeeded  = 7;
        [SerializeField, Min(1)] private int _collapsePerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerExtension = 2470;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onKanExtended;

        private int _extensions;
        private int _extensionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ExtensionsNeeded  => _extensionsNeeded;
        public int   CollapsePerBot    => _collapsePerBot;
        public int   BonusPerExtension => _bonusPerExtension;
        public int   Extensions        => _extensions;
        public int   ExtensionCount    => _extensionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ExtensionProgress => _extensionsNeeded > 0
            ? Mathf.Clamp01(_extensions / (float)_extensionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _extensions = Mathf.Min(_extensions + 1, _extensionsNeeded);
            if (_extensions >= _extensionsNeeded)
            {
                int bonus = _bonusPerExtension;
                _extensionCount++;
                _totalBonusAwarded += bonus;
                _extensions         = 0;
                _onKanExtended?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _extensions = Mathf.Max(0, _extensions - _collapsePerBot);
        }

        public void Reset()
        {
            _extensions        = 0;
            _extensionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
