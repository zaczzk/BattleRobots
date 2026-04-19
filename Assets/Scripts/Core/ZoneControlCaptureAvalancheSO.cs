using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAvalanche", order = 172)]
    public sealed class ZoneControlCaptureAvalancheSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)]    private int   _baseBonus      = 50;
        [SerializeField, Min(0.1f)] private float _multiplierStep = 0.5f;
        [SerializeField, Min(1f)]   private float _maxMultiplier  = 4f;
        [SerializeField, Min(0.1f)] private float _windowSeconds  = 8f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAvalanche;

        private float _currentMultiplier = 1f;
        private int   _avalancheCount;
        private int   _totalBonusAwarded;
        private float _lastCaptureTime  = -1f;

        private void OnEnable() => Reset();

        public float CurrentMultiplier => _currentMultiplier;
        public int   AvalancheCount    => _avalancheCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public int   BaseBonus         => _baseBonus;
        public float MultiplierStep    => _multiplierStep;
        public float MaxMultiplier     => _maxMultiplier;

        public int RecordCapture(float t)
        {
            bool inWindow = _lastCaptureTime >= 0f && (t - _lastCaptureTime) <= _windowSeconds;

            if (inWindow)
            {
                _currentMultiplier  = Mathf.Min(_currentMultiplier + _multiplierStep, _maxMultiplier);
                _avalancheCount++;
                _totalBonusAwarded += _baseBonus;
                _onAvalanche?.Raise();
            }
            else
            {
                _currentMultiplier = 1f;
            }

            _lastCaptureTime = t;
            return Mathf.RoundToInt(_baseBonus * _currentMultiplier);
        }

        public void Reset()
        {
            _currentMultiplier = 1f;
            _avalancheCount    = 0;
            _totalBonusAwarded = 0;
            _lastCaptureTime   = -1f;
        }
    }
}
