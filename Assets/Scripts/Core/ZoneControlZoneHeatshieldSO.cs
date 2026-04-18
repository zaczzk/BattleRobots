using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneHeatshield", order = 101)]
    public sealed class ZoneControlZoneHeatshieldSO : ScriptableObject
    {
        [Header("Heatshield Settings")]
        [Min(0f)]
        [SerializeField] private float _maxShield = 100f;

        [Min(0f)]
        [SerializeField] private float _decayRate = 5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onShieldActivated;
        [SerializeField] private VoidGameEvent _onShieldDepleted;

        private bool  _isActive;
        private float _currentShield;

        private void OnEnable() => Reset();

        public bool  IsActive       => _isActive;
        public float CurrentShield  => _currentShield;
        public float MaxShield      => _maxShield;
        public float DecayRate      => _decayRate;
        public float ShieldProgress => _isActive ? Mathf.Clamp01(_currentShield / Mathf.Max(0.001f, _maxShield)) : 0f;

        public void Activate()
        {
            if (_isActive) return;
            _isActive      = true;
            _currentShield = _maxShield;
            _onShieldActivated?.Raise();
        }

        public void Tick(float dt)
        {
            if (!_isActive) return;
            _currentShield = Mathf.Max(0f, _currentShield - _decayRate * dt);
            if (_currentShield <= 0f)
                Deplete();
        }

        public void Reset()
        {
            _isActive      = false;
            _currentShield = 0f;
        }

        private void Deplete()
        {
            _isActive = false;
            _onShieldDepleted?.Raise();
        }
    }
}
