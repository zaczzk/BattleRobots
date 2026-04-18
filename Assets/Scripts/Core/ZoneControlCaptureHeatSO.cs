using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks capture heat: rises per capture and decays over time.
    /// Fires <c>_onHeatHigh</c> when heat reaches <c>_highHeatThreshold</c>;
    /// fires <c>_onHeatCooled</c> when it drops back below the threshold.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureHeat.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHeat", order = 131)]
    public sealed class ZoneControlCaptureHeatSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)]       private float _heatPerCapture    = 20f;
        [SerializeField, Min(0f)]       private float _decayRate         = 5f;
        [SerializeField, Range(0f,100f)] private float _highHeatThreshold = 60f;
        [SerializeField, Min(0f)]       private float _maxHeat           = 100f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHeatHigh;
        [SerializeField] private VoidGameEvent _onHeatCooled;

        private float _currentHeat;
        private bool  _isHot;

        private void OnEnable() => Reset();

        public float HeatPerCapture    => _heatPerCapture;
        public float DecayRate         => _decayRate;
        public float HighHeatThreshold => _highHeatThreshold;
        public float MaxHeat           => _maxHeat;
        public float CurrentHeat       => _currentHeat;
        public float HeatProgress      => _maxHeat > 0f ? Mathf.Clamp01(_currentHeat / _maxHeat) : 0f;
        public bool  IsHot             => _isHot;

        public void RecordCapture()
        {
            _currentHeat = Mathf.Min(_currentHeat + _heatPerCapture, _maxHeat);
            EvaluateHeat();
        }

        public void Tick(float dt)
        {
            if (_currentHeat <= 0f) return;
            _currentHeat = Mathf.Max(0f, _currentHeat - _decayRate * dt);
            EvaluateHeat();
        }

        public void Reset()
        {
            _currentHeat = 0f;
            _isHot       = false;
        }

        private void EvaluateHeat()
        {
            bool nowHot = _currentHeat >= _highHeatThreshold;
            if (nowHot && !_isHot)
            {
                _isHot = true;
                _onHeatHigh?.Raise();
            }
            else if (!nowHot && _isHot)
            {
                _isHot = false;
                _onHeatCooled?.Raise();
            }
        }
    }
}
