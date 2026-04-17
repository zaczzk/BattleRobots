using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneValue", order = 99)]
    public sealed class ZoneControlZoneValueSO : ScriptableObject
    {
        [Header("Zone Value Settings")]
        [Min(0)]
        [SerializeField] private int _baseValue = 100;

        [Min(0f)]
        [SerializeField] private float _valueAccrualRate = 10f;

        [Min(0)]
        [SerializeField] private int _maxValue = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onValueChanged;

        private float _accruedBonus;
        private bool  _isActive;

        private void OnEnable() => Reset();

        public int   BaseValue         => _baseValue;
        public float ValueAccrualRate  => _valueAccrualRate;
        public int   MaxValue          => _maxValue;
        public int   CurrentValue      => Mathf.Min(_baseValue + Mathf.FloorToInt(_accruedBonus), _maxValue);
        public float AccrualProgress   => Mathf.Clamp01((CurrentValue - _baseValue) / (float)Mathf.Max(1, _maxValue - _baseValue));
        public bool  IsActive          => _isActive;

        public void StartAccruing()
        {
            if (_isActive) return;
            _isActive = true;
        }

        public void StopAccruing()
        {
            _isActive = false;
        }

        public void Tick(float dt)
        {
            if (!_isActive) return;
            int before = CurrentValue;
            _accruedBonus += _valueAccrualRate * dt;
            if (CurrentValue != before)
                _onValueChanged?.Raise();
        }

        public int Harvest()
        {
            int value = CurrentValue;
            _accruedBonus = 0f;
            _isActive     = false;
            _onValueChanged?.Raise();
            return value;
        }

        public void Reset()
        {
            _accruedBonus = 0f;
            _isActive     = false;
        }
    }
}
