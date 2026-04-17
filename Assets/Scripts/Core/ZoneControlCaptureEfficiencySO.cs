using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEfficiency", order = 97)]
    public sealed class ZoneControlCaptureEfficiencySO : ScriptableObject
    {
        [Header("Efficiency Thresholds")]
        [Range(0f, 1f)]
        [SerializeField] private float _highEfficiencyThreshold = 0.7f;

        [Range(0f, 1f)]
        [SerializeField] private float _lowEfficiencyThreshold = 0.3f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHighEfficiency;
        [SerializeField] private VoidGameEvent _onLowEfficiency;
        [SerializeField] private VoidGameEvent _onEfficiencyNormal;

        private int  _playerCaptures;
        private int  _totalCaptures;
        private bool _isHighEfficiency;
        private bool _isLowEfficiency;

        private void OnEnable() => Reset();

        private void OnValidate()
        {
            if (_lowEfficiencyThreshold > _highEfficiencyThreshold)
                _lowEfficiencyThreshold = _highEfficiencyThreshold;
        }

        public int   PlayerCaptures          => _playerCaptures;
        public int   TotalCaptures           => _totalCaptures;
        public float HighEfficiencyThreshold => _highEfficiencyThreshold;
        public float LowEfficiencyThreshold  => _lowEfficiencyThreshold;
        public bool  IsHighEfficiency        => _isHighEfficiency;
        public bool  IsLowEfficiency         => _isLowEfficiency;

        public float Efficiency => _totalCaptures > 0
            ? Mathf.Clamp01((float)_playerCaptures / _totalCaptures)
            : 0f;

        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            _totalCaptures++;
            EvaluateEfficiency();
        }

        public void RecordBotCapture()
        {
            _totalCaptures++;
            EvaluateEfficiency();
        }

        public void Reset()
        {
            _playerCaptures   = 0;
            _totalCaptures    = 0;
            _isHighEfficiency = false;
            _isLowEfficiency  = false;
        }

        private void EvaluateEfficiency()
        {
            float ratio   = Efficiency;
            bool  nowHigh = _totalCaptures > 0 && ratio >= _highEfficiencyThreshold;
            bool  nowLow  = _totalCaptures > 0 && ratio <= _lowEfficiencyThreshold && !nowHigh;

            if (nowHigh && !_isHighEfficiency)
            {
                _isHighEfficiency = true;
                _isLowEfficiency  = false;
                _onHighEfficiency?.Raise();
            }
            else if (nowLow && !_isLowEfficiency)
            {
                _isLowEfficiency  = true;
                _isHighEfficiency = false;
                _onLowEfficiency?.Raise();
            }
            else if (!nowHigh && _isHighEfficiency)
            {
                _isHighEfficiency = false;
                _onEfficiencyNormal?.Raise();
            }
            else if (!nowLow && _isLowEfficiency)
            {
                _isLowEfficiency = false;
                _onEfficiencyNormal?.Raise();
            }
        }
    }
}
