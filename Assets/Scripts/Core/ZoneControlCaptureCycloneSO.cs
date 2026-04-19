using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCyclone", order = 191)]
    public sealed class ZoneControlCaptureCycloneSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)]   private int   _activatesEveryN        = 5;
        [SerializeField, Min(0)]   private int   _cycloneBonus           = 200;
        [SerializeField, Min(0.1f)] private float _cycloneDurationSeconds = 8f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCycloneOpened;
        [SerializeField] private VoidGameEvent _onCycloneClosed;

        private int   _totalCaptures;
        private bool  _isActive;
        private float _timer;
        private int   _cycloneCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ActivatesEveryN       => _activatesEveryN;
        public int   CycloneBonus          => _cycloneBonus;
        public float CycloneDurationSeconds => _cycloneDurationSeconds;
        public int   TotalCaptures         => _totalCaptures;
        public bool  IsActive              => _isActive;
        public float CycloneTimer          => _timer;
        public int   CycloneCount          => _cycloneCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;

        public int GetCaptureBonus() => _isActive ? _cycloneBonus : 0;

        public void RecordCapture()
        {
            _totalCaptures++;
            if (!_isActive && _totalCaptures % _activatesEveryN == 0)
                Activate();
        }

        public void Tick(float dt)
        {
            if (!_isActive) return;
            _timer -= dt;
            if (_timer <= 0f)
                Deactivate();
        }

        private void Activate()
        {
            _isActive = true;
            _timer    = _cycloneDurationSeconds;
            _cycloneCount++;
            _onCycloneOpened?.Raise();
        }

        private void Deactivate()
        {
            _isActive = false;
            _timer    = 0f;
            _onCycloneClosed?.Raise();
        }

        public void Reset()
        {
            _totalCaptures    = 0;
            _isActive         = false;
            _timer            = 0f;
            _cycloneCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
