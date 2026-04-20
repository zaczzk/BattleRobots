using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSurgeWindow", order = 204)]
    public sealed class ZoneControlCaptureSurgeWindowSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _botTriggerCount      = 3;
        [SerializeField, Min(1)] private int _surgePlayerCaptures  = 3;
        [SerializeField, Min(0)] private int _bonusPerSurgeCapture = 110;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSurgeOpened;
        [SerializeField] private VoidGameEvent _onSurgeClosed;

        private int  _botStreak;
        private bool _isSurgeActive;
        private int  _playerCapturesDuringSurge;
        private int  _surgeCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BotTriggerCount           => _botTriggerCount;
        public int   SurgePlayerCaptures       => _surgePlayerCaptures;
        public int   BonusPerSurgeCapture      => _bonusPerSurgeCapture;
        public int   BotStreak                 => _botStreak;
        public bool  IsSurgeActive             => _isSurgeActive;
        public int   PlayerCapturesDuringSurge => _playerCapturesDuringSurge;
        public int   SurgeCount                => _surgeCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float SurgeProgress             => _isSurgeActive
            ? Mathf.Clamp01(_playerCapturesDuringSurge / (float)Mathf.Max(1, _surgePlayerCaptures))
            : Mathf.Clamp01(_botStreak / (float)Mathf.Max(1, _botTriggerCount));

        public void RecordBotCapture()
        {
            if (_isSurgeActive) { CloseSurge(); return; }
            _botStreak++;
            if (_botStreak >= _botTriggerCount)
                OpenSurge();
        }

        public int RecordPlayerCapture()
        {
            _botStreak = 0;
            if (!_isSurgeActive) return 0;
            _playerCapturesDuringSurge++;
            _totalBonusAwarded += _bonusPerSurgeCapture;
            if (_playerCapturesDuringSurge >= _surgePlayerCaptures)
                CloseSurge();
            return _bonusPerSurgeCapture;
        }

        private void OpenSurge()
        {
            _isSurgeActive             = true;
            _playerCapturesDuringSurge = 0;
            _botStreak                 = 0;
            _onSurgeOpened?.Raise();
        }

        private void CloseSurge()
        {
            _isSurgeActive = false;
            _botStreak     = 0;
            _surgeCount++;
            _onSurgeClosed?.Raise();
        }

        public void Reset()
        {
            _botStreak                 = 0;
            _isSurgeActive             = false;
            _playerCapturesDuringSurge = 0;
            _surgeCount                = 0;
            _totalBonusAwarded         = 0;
        }
    }
}
