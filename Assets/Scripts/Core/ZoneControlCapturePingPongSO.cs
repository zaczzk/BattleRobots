using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePingPong", order = 162)]
    public sealed class ZoneControlCapturePingPongSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerPingPong = 125;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPingPong;

        private int  _pingPongCount;
        private int  _totalBonusAwarded;
        private bool _lastWasPlayer;
        private bool _hasFirst;

        private void OnEnable() => Reset();

        public int  PingPongCount     => _pingPongCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public int  BonusPerPingPong  => _bonusPerPingPong;
        public bool HasFirst          => _hasFirst;

        public void RecordPlayerCapture()
        {
            if (!_hasFirst)
            {
                _hasFirst      = true;
                _lastWasPlayer = true;
                return;
            }

            if (_lastWasPlayer) return;

            _lastWasPlayer = true;
            _pingPongCount++;
            _totalBonusAwarded += _bonusPerPingPong;
            _onPingPong?.Raise();
        }

        public void RecordBotCapture()
        {
            if (!_hasFirst)
            {
                _hasFirst      = true;
                _lastWasPlayer = false;
                return;
            }

            if (!_lastWasPlayer) return;

            _lastWasPlayer = false;
            _pingPongCount++;
            _totalBonusAwarded += _bonusPerPingPong;
            _onPingPong?.Raise();
        }

        public void Reset()
        {
            _pingPongCount     = 0;
            _totalBonusAwarded = 0;
            _lastWasPlayer     = false;
            _hasFirst          = false;
        }
    }
}
