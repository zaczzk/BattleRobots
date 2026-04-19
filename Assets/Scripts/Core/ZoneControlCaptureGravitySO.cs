using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGravity", order = 184)]
    public sealed class ZoneControlCaptureGravitySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)] private float _gravityRisePerBotCapture    = 20f;
        [SerializeField, Min(0f)] private float _gravityFallPerPlayerCapture = 15f;
        [SerializeField, Min(1f)] private float _maxGravity                  = 100f;
        [SerializeField, Min(0)]  private int   _bonusAtMaxGravity           = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGravityPeak;

        private float _currentGravity;
        private bool  _peakFired;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float MaxGravity                  => _maxGravity;
        public float GravityRisePerBotCapture    => _gravityRisePerBotCapture;
        public float GravityFallPerPlayerCapture => _gravityFallPerPlayerCapture;
        public int   BonusAtMaxGravity           => _bonusAtMaxGravity;
        public float CurrentGravity              => _currentGravity;
        public bool  IsAtPeak                    => _currentGravity >= _maxGravity;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float GravityProgress             => _maxGravity > 0f ? Mathf.Clamp01(_currentGravity / _maxGravity) : 0f;

        public void RecordBotCapture()
        {
            _currentGravity = Mathf.Min(_currentGravity + _gravityRisePerBotCapture, _maxGravity);
            EvaluatePeak();
        }

        public int RecordPlayerCapture()
        {
            int bonus = 0;
            if (_currentGravity >= _maxGravity)
            {
                bonus               = _bonusAtMaxGravity;
                _totalBonusAwarded += bonus;
            }
            _currentGravity = Mathf.Max(0f, _currentGravity - _gravityFallPerPlayerCapture);
            EvaluatePeak();
            return bonus;
        }

        private void EvaluatePeak()
        {
            if (_currentGravity >= _maxGravity && !_peakFired)
            {
                _peakFired = true;
                _onGravityPeak?.Raise();
            }
            if (_currentGravity < _maxGravity && _peakFired)
                _peakFired = false;
        }

        public void Reset()
        {
            _currentGravity    = 0f;
            _peakFired         = false;
            _totalBonusAwarded = 0;
        }
    }
}
