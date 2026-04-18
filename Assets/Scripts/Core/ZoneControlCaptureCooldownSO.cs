using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCooldown", order = 101)]
    public sealed class ZoneControlCaptureCooldownSO : ScriptableObject
    {
        [Header("Cooldown Settings")]
        [Min(0.1f)]
        [SerializeField] private float _cooldownDuration = 5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCooldownStarted;
        [SerializeField] private VoidGameEvent _onCooldownExpired;

        private bool  _isOnCooldown;
        private float _remaining;
        private int   _totalCooldowns;

        private void OnEnable() => Reset();

        public bool  IsOnCooldown     => _isOnCooldown;
        public float RemainingTime    => _remaining;
        public float CooldownDuration => _cooldownDuration;
        public int   TotalCooldowns   => _totalCooldowns;

        public float CooldownProgress =>
            _isOnCooldown
                ? Mathf.Clamp01(1f - _remaining / Mathf.Max(0.001f, _cooldownDuration))
                : 0f;

        public bool StartCooldown()
        {
            if (_isOnCooldown) return false;
            _isOnCooldown = true;
            _remaining    = _cooldownDuration;
            _totalCooldowns++;
            _onCooldownStarted?.Raise();
            return true;
        }

        public void Tick(float dt)
        {
            if (!_isOnCooldown) return;
            _remaining = Mathf.Max(0f, _remaining - dt);
            if (_remaining <= 0f)
                Expire();
        }

        public void Reset()
        {
            _isOnCooldown   = false;
            _remaining      = 0f;
            _totalCooldowns = 0;
        }

        private void Expire()
        {
            _isOnCooldown = false;
            _onCooldownExpired?.Raise();
        }
    }
}
