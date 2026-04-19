using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEcho", order = 163)]
    public sealed class ZoneControlCaptureEchoSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _echoWindowSeconds = 5f;
        [SerializeField, Min(0)]    private int   _bonusPerEcho = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEcho;

        private float _lastCaptureTime = -1f;
        private int   _echoCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   EchoCount         => _echoCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float EchoWindowSeconds => _echoWindowSeconds;
        public int   BonusPerEcho      => _bonusPerEcho;
        public bool  HasPriorCapture   => _lastCaptureTime >= 0f;

        public void RecordCapture(float t)
        {
            if (HasPriorCapture && t - _lastCaptureTime <= _echoWindowSeconds)
            {
                _echoCount++;
                _totalBonusAwarded += _bonusPerEcho;
                _onEcho?.Raise();
            }
            _lastCaptureTime = t;
        }

        public void Reset()
        {
            _lastCaptureTime   = -1f;
            _echoCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
