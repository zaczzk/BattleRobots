using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAcceleration", order = 151)]
    public sealed class ZoneControlCaptureAccelerationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)] private float _accelerationPerCapture = 15f;
        [SerializeField, Min(0f)] private float _decayRate               = 10f;
        [SerializeField, Min(1f)] private float _maxAcceleration         = 100f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMaxAcceleration;

        private float _currentAcceleration;
        private bool  _peakFired;

        private void OnEnable() => Reset();

        public float CurrentAcceleration => _currentAcceleration;
        public float MaxAcceleration     => _maxAcceleration;
        public float AccelerationPerCapture => _accelerationPerCapture;
        public float DecayRate           => _decayRate;
        public float AccelerationProgress => Mathf.Clamp01(_currentAcceleration / _maxAcceleration);
        public bool  IsAtMax             => _currentAcceleration >= _maxAcceleration;

        public void RecordCapture()
        {
            _currentAcceleration = Mathf.Min(_currentAcceleration + _accelerationPerCapture, _maxAcceleration);
            EvaluatePeak();
        }

        public void Tick(float dt)
        {
            if (_currentAcceleration <= 0f) return;
            _currentAcceleration = Mathf.Max(0f, _currentAcceleration - _decayRate * dt);
            if (_currentAcceleration < _maxAcceleration)
                _peakFired = false;
        }

        public void Reset()
        {
            _currentAcceleration = 0f;
            _peakFired           = false;
        }

        private void EvaluatePeak()
        {
            if (_peakFired || _currentAcceleration < _maxAcceleration) return;
            _peakFired = true;
            _onMaxAcceleration?.Raise();
        }
    }
}
