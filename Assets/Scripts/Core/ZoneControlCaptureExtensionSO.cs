using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureExtension", order = 396)]
    public sealed class ZoneControlCaptureExtensionSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded    = 6;
        [SerializeField, Min(1)] private int _splitPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerExtension = 2680;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onExtensionApplied;

        private int _elements;
        private int _extensionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded      => _elementsNeeded;
        public int   SplitPerBot         => _splitPerBot;
        public int   BonusPerExtension   => _bonusPerExtension;
        public int   Elements            => _elements;
        public int   ExtensionCount      => _extensionCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ElementProgress     => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerExtension;
                _extensionCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onExtensionApplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _splitPerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _extensionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
