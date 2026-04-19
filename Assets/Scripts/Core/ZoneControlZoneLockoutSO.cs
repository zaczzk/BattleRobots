using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneLockout", order = 148)]
    public sealed class ZoneControlZoneLockoutSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _lockoutDuration = 5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLockoutStarted;
        [SerializeField] private VoidGameEvent _onLockoutExpired;

        private bool  _isLockedOut;
        private float _remaining;
        private int   _totalLockouts;

        private void OnEnable() => Reset();

        public bool  IsLockedOut     => _isLockedOut;
        public float RemainingTime   => _remaining;
        public float LockoutDuration => _lockoutDuration;
        public int   TotalLockouts   => _totalLockouts;
        public float LockoutProgress => _isLockedOut
            ? Mathf.Clamp01(1f - _remaining / _lockoutDuration)
            : 0f;

        public bool StartLockout()
        {
            if (_isLockedOut) return false;
            _isLockedOut = true;
            _remaining   = _lockoutDuration;
            _totalLockouts++;
            _onLockoutStarted?.Raise();
            return true;
        }

        public void Tick(float dt)
        {
            if (!_isLockedOut) return;
            _remaining = Mathf.Max(0f, _remaining - dt);
            if (_remaining <= 0f)
                Expire();
        }

        public void Reset()
        {
            _isLockedOut   = false;
            _remaining     = 0f;
            _totalLockouts = 0;
        }

        private void Expire()
        {
            _isLockedOut = false;
            _remaining   = 0f;
            _onLockoutExpired?.Raise();
        }
    }
}
