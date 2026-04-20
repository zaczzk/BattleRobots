using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRadar", order = 200)]
    public sealed class ZoneControlCaptureRadarSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int   _windowCaptures = 6;
        [SerializeField, Min(0)] private int   _bonusPerPing   = 260;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRadarPing;

        private int _windowPlayerCaptures;
        private int _windowBotCaptures;
        private int _windowTotal;
        private int _pingCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int WindowCaptures     => _windowCaptures;
        public int BonusPerPing       => _bonusPerPing;
        public int PingCount          => _pingCount;
        public int TotalBonusAwarded  => _totalBonusAwarded;
        public int WindowPlayerCaptures => _windowPlayerCaptures;
        public int WindowBotCaptures    => _windowBotCaptures;

        public void RecordPlayerCapture()
        {
            _windowPlayerCaptures++;
            _windowTotal++;
            EvaluateWindow();
        }

        public void RecordBotCapture()
        {
            _windowBotCaptures++;
            _windowTotal++;
            EvaluateWindow();
        }

        private void EvaluateWindow()
        {
            if (_windowTotal < _windowCaptures) return;
            if (_windowPlayerCaptures > _windowBotCaptures)
            {
                _pingCount++;
                _totalBonusAwarded += _bonusPerPing;
                _onRadarPing?.Raise();
            }
            _windowPlayerCaptures = 0;
            _windowBotCaptures    = 0;
            _windowTotal          = 0;
        }

        public void Reset()
        {
            _windowPlayerCaptures = 0;
            _windowBotCaptures    = 0;
            _windowTotal          = 0;
            _pingCount            = 0;
            _totalBonusAwarded    = 0;
        }
    }
}
