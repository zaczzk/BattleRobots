using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOvertime", order = 166)]
    public sealed class ZoneControlCaptureOvertimeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerOvertimeLead = 75;
        [SerializeField, Min(0)] private int _maxOvertimeBonus     = 600;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOvertimeResolved;

        private bool _isActive;
        private int  _playerOTCaptures;
        private int  _botOTCaptures;
        private int  _overtimeBonus;

        private void OnEnable() => Reset();

        public bool IsActive          => _isActive;
        public int  PlayerOTCaptures  => _playerOTCaptures;
        public int  BotOTCaptures     => _botOTCaptures;
        public int  OvertimeLead      => Mathf.Max(0, _playerOTCaptures - _botOTCaptures);
        public int  OvertimeBonus     => _overtimeBonus;
        public int  BonusPerOvertimeLead => _bonusPerOvertimeLead;
        public int  MaxOvertimeBonus  => _maxOvertimeBonus;

        public void StartOvertime()
        {
            if (_isActive) return;
            _isActive = true;
        }

        public void RecordPlayerCapture()
        {
            if (!_isActive) return;
            _playerOTCaptures++;
        }

        public void RecordBotCapture()
        {
            if (!_isActive) return;
            _botOTCaptures++;
        }

        public int ResolveOvertime()
        {
            _isActive = false;
            int lead  = OvertimeLead;
            _overtimeBonus = lead > 0
                ? Mathf.Min(lead * _bonusPerOvertimeLead, _maxOvertimeBonus)
                : 0;
            _onOvertimeResolved?.Raise();
            return _overtimeBonus;
        }

        public void Reset()
        {
            _isActive         = false;
            _playerOTCaptures = 0;
            _botOTCaptures    = 0;
            _overtimeBonus    = 0;
        }
    }
}
