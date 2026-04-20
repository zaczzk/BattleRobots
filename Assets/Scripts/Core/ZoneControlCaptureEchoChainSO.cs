using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEchoChain", order = 203)]
    public sealed class ZoneControlCaptureEchoChainSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _echoWindowSeconds = 6f;
        [SerializeField, Min(0)]    private int   _bonusPerEcho      = 60;
        [SerializeField, Min(1)]    private int   _maxEchoMultiplier = 5;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEchoHit;

        private float _lastPlayerCapTime = -1f;
        private int   _currentMultiplier;
        private int   _echoCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float EchoWindowSeconds  => _echoWindowSeconds;
        public int   BonusPerEcho       => _bonusPerEcho;
        public int   MaxEchoMultiplier  => _maxEchoMultiplier;
        public int   CurrentMultiplier  => _currentMultiplier;
        public int   EchoCount          => _echoCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float MultiplierProgress => _maxEchoMultiplier > 0
            ? Mathf.Clamp01(_currentMultiplier / (float)_maxEchoMultiplier)
            : 0f;

        public int RecordPlayerCapture(float t)
        {
            bool withinWindow = _lastPlayerCapTime >= 0f &&
                                (t - _lastPlayerCapTime) <= _echoWindowSeconds;
            _currentMultiplier = withinWindow
                ? Mathf.Min(_currentMultiplier + 1, _maxEchoMultiplier)
                : 1;

            int bonus = _bonusPerEcho * _currentMultiplier;
            _echoCount++;
            _totalBonusAwarded += bonus;
            _lastPlayerCapTime  = t;
            _onEchoHit?.Raise();
            return bonus;
        }

        public void RecordBotCapture()
        {
            _currentMultiplier = 0;
            _lastPlayerCapTime = -1f;
        }

        public void Reset()
        {
            _lastPlayerCapTime = -1f;
            _currentMultiplier = 0;
            _echoCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
