using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLink", order = 355)]
    public sealed class ZoneControlCaptureLinkSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _connectionsNeeded = 5;
        [SerializeField, Min(1)] private int _breakPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerList      = 2065;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onListFormed;

        private int _connections;
        private int _listCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ConnectionsNeeded  => _connectionsNeeded;
        public int   BreakPerBot        => _breakPerBot;
        public int   BonusPerList       => _bonusPerList;
        public int   Connections        => _connections;
        public int   ListCount          => _listCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ConnectionProgress => _connectionsNeeded > 0
            ? Mathf.Clamp01(_connections / (float)_connectionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _connections = Mathf.Min(_connections + 1, _connectionsNeeded);
            if (_connections >= _connectionsNeeded)
            {
                int bonus = _bonusPerList;
                _listCount++;
                _totalBonusAwarded += bonus;
                _connections        = 0;
                _onListFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _connections = Mathf.Max(0, _connections - _breakPerBot);
        }

        public void Reset()
        {
            _connections       = 0;
            _listCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
