using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRift", order = 187)]
    public sealed class ZoneControlCaptureRiftSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)] private float _idleThreshold     = 15f;
        [SerializeField, Min(0)]  private int   _riftBonus         = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRiftOpened;
        [SerializeField] private VoidGameEvent _onRiftClosed;

        private float _idleTimer;
        private bool  _isRiftOpen;
        private int   _riftCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float IdleThreshold     => _idleThreshold;
        public int   RiftBonus         => _riftBonus;
        public bool  IsRiftOpen        => _isRiftOpen;
        public float IdleTimer         => _idleTimer;
        public int   RiftCount         => _riftCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float IdleProgress      => Mathf.Clamp01(_idleTimer / Mathf.Max(0.001f, _idleThreshold));

        public void Tick(float dt)
        {
            if (_isRiftOpen) return;
            _idleTimer += dt;
            if (_idleTimer >= _idleThreshold)
                OpenRift();
        }

        public int RecordCapture()
        {
            _idleTimer = 0f;
            if (!_isRiftOpen)
                return 0;

            CloseRift();
            return _riftBonus;
        }

        private void OpenRift()
        {
            _isRiftOpen = true;
            _idleTimer  = _idleThreshold;
            _onRiftOpened?.Raise();
        }

        private void CloseRift()
        {
            _isRiftOpen         = false;
            _riftCount++;
            _totalBonusAwarded += _riftBonus;
            _onRiftClosed?.Raise();
        }

        public void Reset()
        {
            _idleTimer         = 0f;
            _isRiftOpen        = false;
            _riftCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
