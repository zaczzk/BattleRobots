using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFlash", order = 195)]
    public sealed class ZoneControlCaptureFlashSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _flashWindowSeconds = 4f;
        [SerializeField, Min(0)]   private int   _bonusPerFlash       = 175;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFlash;

        private float _lastBotCaptureTime = -1f;
        private int   _flashCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float FlashWindowSeconds  => _flashWindowSeconds;
        public int   BonusPerFlash       => _bonusPerFlash;
        public int   FlashCount          => _flashCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public bool  FlashWindowActive   => _lastBotCaptureTime >= 0f;

        public void RecordBotCapture(float t)
        {
            _lastBotCaptureTime = t;
        }

        public int RecordPlayerCapture(float t)
        {
            bool isFlash = _lastBotCaptureTime >= 0f && (t - _lastBotCaptureTime) <= _flashWindowSeconds;
            _lastBotCaptureTime = -1f;
            if (!isFlash) return 0;
            _flashCount++;
            _totalBonusAwarded += _bonusPerFlash;
            _onFlash?.Raise();
            return _bonusPerFlash;
        }

        public void Reset()
        {
            _lastBotCaptureTime = -1f;
            _flashCount         = 0;
            _totalBonusAwarded  = 0;
        }
    }
}
