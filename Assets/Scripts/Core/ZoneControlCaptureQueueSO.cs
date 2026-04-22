using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureQueue", order = 349)]
    public sealed class ZoneControlCaptureQueueSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _messagesNeeded   = 5;
        [SerializeField, Min(1)] private int _dropPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerDispatch = 1975;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onQueueDispatched;

        private int _messages;
        private int _dispatchCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MessagesNeeded    => _messagesNeeded;
        public int   DropPerBot        => _dropPerBot;
        public int   BonusPerDispatch  => _bonusPerDispatch;
        public int   Messages          => _messages;
        public int   DispatchCount     => _dispatchCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float MessageProgress   => _messagesNeeded > 0
            ? Mathf.Clamp01(_messages / (float)_messagesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _messages = Mathf.Min(_messages + 1, _messagesNeeded);
            if (_messages >= _messagesNeeded)
            {
                int bonus = _bonusPerDispatch;
                _dispatchCount++;
                _totalBonusAwarded += bonus;
                _messages           = 0;
                _onQueueDispatched?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _messages = Mathf.Max(0, _messages - _dropPerBot);
        }

        public void Reset()
        {
            _messages          = 0;
            _dispatchCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
