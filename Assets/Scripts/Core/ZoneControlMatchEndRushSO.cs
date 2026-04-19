using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchEndRush", order = 154)]
    public sealed class ZoneControlMatchEndRushSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)] private float _rushWindowSeconds  = 30f;
        [SerializeField, Min(0)]  private int   _bonusPerRushCapture = 75;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRushStarted;
        [SerializeField] private VoidGameEvent _onRushEnded;

        private bool _isActive;
        private bool _hasEnded;
        private int  _rushCaptureCount;
        private int  _totalRushBonus;

        private void OnEnable() => Reset();

        public bool  IsActive          => _isActive;
        public bool  HasEnded          => _hasEnded;
        public float RushWindowSeconds => _rushWindowSeconds;
        public int   BonusPerRushCapture => _bonusPerRushCapture;
        public int   RushCaptureCount  => _rushCaptureCount;
        public int   TotalRushBonus    => _totalRushBonus;

        public void StartRush()
        {
            if (_isActive || _hasEnded) return;
            _isActive = true;
            _onRushStarted?.Raise();
        }

        public int RecordCapture()
        {
            if (!_isActive) return 0;
            _rushCaptureCount++;
            _totalRushBonus += _bonusPerRushCapture;
            return _bonusPerRushCapture;
        }

        public void EndRush()
        {
            if (!_isActive) return;
            _isActive = false;
            _hasEnded = true;
            _onRushEnded?.Raise();
        }

        public void Reset()
        {
            _isActive        = false;
            _hasEnded        = false;
            _rushCaptureCount = 0;
            _totalRushBonus   = 0;
        }
    }
}
