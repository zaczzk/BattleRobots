using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSocket", order = 344)]
    public sealed class ZoneControlCaptureSocketSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _connectionsNeeded = 5;
        [SerializeField, Min(1)] private int _closePerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerSession   = 1900;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSocketBound;

        private int _connections;
        private int _sessionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ConnectionsNeeded  => _connectionsNeeded;
        public int   ClosePerBot        => _closePerBot;
        public int   BonusPerSession    => _bonusPerSession;
        public int   Connections        => _connections;
        public int   SessionCount       => _sessionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ConnectionProgress => _connectionsNeeded > 0
            ? Mathf.Clamp01(_connections / (float)_connectionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _connections = Mathf.Min(_connections + 1, _connectionsNeeded);
            if (_connections >= _connectionsNeeded)
            {
                int bonus = _bonusPerSession;
                _sessionCount++;
                _totalBonusAwarded += bonus;
                _connections        = 0;
                _onSocketBound?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _connections = Mathf.Max(0, _connections - _closePerBot);
        }

        public void Reset()
        {
            _connections       = 0;
            _sessionCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
