using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDeceleration", order = 155)]
    public sealed class ZoneControlCaptureDecelerationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)] private float _decelerationPerBotCapture   = 20f;
        [SerializeField, Min(0f)] private float _reductionPerPlayerCapture   = 10f;
        [SerializeField, Min(0f)] private float _decayRate                   = 5f;
        [SerializeField, Min(1f)] private float _maxDeceleration             = 100f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDecelerationPeak;

        private float _currentDeceleration;
        private bool  _peakFired;

        private void OnEnable() => Reset();

        public float CurrentDeceleration    => _currentDeceleration;
        public float MaxDeceleration        => _maxDeceleration;
        public float DecelerationProgress   => Mathf.Clamp01(_currentDeceleration / _maxDeceleration);

        public void RecordBotCapture()
        {
            _currentDeceleration = Mathf.Min(_currentDeceleration + _decelerationPerBotCapture, _maxDeceleration);
            EvaluatePeak();
        }

        public void RecordPlayerCapture()
        {
            _currentDeceleration = Mathf.Max(0f, _currentDeceleration - _reductionPerPlayerCapture);
            if (_currentDeceleration < _maxDeceleration)
                _peakFired = false;
        }

        public void Tick(float dt)
        {
            if (_currentDeceleration <= 0f) return;
            _currentDeceleration = Mathf.Max(0f, _currentDeceleration - _decayRate * dt);
            if (_currentDeceleration < _maxDeceleration)
                _peakFired = false;
        }

        public void Reset()
        {
            _currentDeceleration = 0f;
            _peakFired           = false;
        }

        private void EvaluatePeak()
        {
            if (_peakFired || _currentDeceleration < _maxDeceleration) return;
            _peakFired = true;
            _onDecelerationPeak?.Raise();
        }
    }
}
