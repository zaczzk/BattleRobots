using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAura", order = 169)]
    public sealed class ZoneControlCaptureAuraSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)]        private float _auraPerCapture  = 20f;
        [SerializeField, Min(1f)]        private float _maxAura          = 100f;
        [SerializeField, Range(0f, 1f)]  private float _auraThreshold   = 0.5f;
        [SerializeField, Min(0f)]        private float _decayRate        = 8f;
        [SerializeField, Min(0)]         private int   _bonusPerCapture  = 75;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAuraActivated;
        [SerializeField] private VoidGameEvent _onAuraDepleted;

        private float _currentAura;
        private bool  _isAuraActive;
        private int   _totalAuraBonus;
        private int   _auraCaptures;

        private void OnEnable() => Reset();

        public float CurrentAura    => _currentAura;
        public float MaxAura        => _maxAura;
        public float AuraPerCapture => _auraPerCapture;
        public float DecayRate      => _decayRate;
        public int   BonusPerCapture => _bonusPerCapture;
        public float AuraThreshold  => _auraThreshold;
        public float AuraProgress   => _maxAura > 0f ? Mathf.Clamp01(_currentAura / _maxAura) : 0f;
        public bool  IsAuraActive   => _isAuraActive;
        public int   TotalAuraBonus => _totalAuraBonus;
        public int   AuraCaptures   => _auraCaptures;

        public int RecordCapture()
        {
            _currentAura = Mathf.Min(_currentAura + _auraPerCapture, _maxAura);
            EvaluateAura();
            if (_isAuraActive)
            {
                _auraCaptures++;
                _totalAuraBonus += _bonusPerCapture;
                return _bonusPerCapture;
            }
            return 0;
        }

        public void Tick(float dt)
        {
            if (_currentAura <= 0f) return;
            _currentAura = Mathf.Max(0f, _currentAura - _decayRate * dt);
            EvaluateAura();
        }

        private void EvaluateAura()
        {
            float threshold = _maxAura * _auraThreshold;
            bool active = _currentAura >= threshold;
            if (active && !_isAuraActive)
            {
                _isAuraActive = true;
                _onAuraActivated?.Raise();
            }
            else if (!active && _isAuraActive)
            {
                _isAuraActive = false;
                _onAuraDepleted?.Raise();
            }
        }

        public void Reset()
        {
            _currentAura   = 0f;
            _isAuraActive  = false;
            _totalAuraBonus = 0;
            _auraCaptures  = 0;
        }
    }
}
