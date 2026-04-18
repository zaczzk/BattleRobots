using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks match momentum based on the ratio of player vs bot captures
    /// within a rolling time window. Momentum [0,1]: 1 = all player, 0 = all bot, 0.5 = tied.
    /// Fires <c>_onMomentumHigh</c> when player ratio ≥ <c>_highThreshold</c>,
    /// <c>_onMomentumLow</c> when ≤ <c>_lowThreshold</c>, and <c>_onMomentumNeutral</c>
    /// on transitions to the neutral band.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchMomentum.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchMomentum", order = 128)]
    public sealed class ZoneControlMatchMomentumSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)]       private float _windowSeconds  = 20f;
        [SerializeField, Range(0f, 1f)] private float _highThreshold  = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _lowThreshold   = 0.35f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMomentumHigh;
        [SerializeField] private VoidGameEvent _onMomentumLow;
        [SerializeField] private VoidGameEvent _onMomentumNeutral;

        private readonly List<float> _playerTimestamps = new List<float>();
        private readonly List<float> _botTimestamps    = new List<float>();

        private bool _isHigh;
        private bool _isLow;

        private void OnEnable() => Reset();

        public float WindowSeconds => _windowSeconds;
        public float HighThreshold => _highThreshold;
        public float LowThreshold  => _lowThreshold;

        /// <summary>Player capture ratio in window [0,1]; 0 when no captures.</summary>
        public float Momentum
        {
            get
            {
                int total = _playerTimestamps.Count + _botTimestamps.Count;
                return total == 0 ? 0.5f : Mathf.Clamp01((float)_playerTimestamps.Count / total);
            }
        }

        public bool IsHigh    => _isHigh;
        public bool IsLow     => _isLow;
        public bool IsNeutral => !_isHigh && !_isLow;

        public void RecordPlayerCapture(float time)
        {
            Prune(time);
            _playerTimestamps.Add(time);
            EvaluateMomentum();
        }

        public void RecordBotCapture(float time)
        {
            Prune(time);
            _botTimestamps.Add(time);
            EvaluateMomentum();
        }

        public void Tick(float time)
        {
            Prune(time);
            EvaluateMomentum();
        }

        private void Prune(float now)
        {
            float cutoff = now - _windowSeconds;
            _playerTimestamps.RemoveAll(t => t < cutoff);
            _botTimestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateMomentum()
        {
            float m = Momentum;

            if (!_isHigh && m >= _highThreshold)
            {
                _isHigh = true;
                _isLow  = false;
                _onMomentumHigh?.Raise();
            }
            else if (!_isLow && m <= _lowThreshold)
            {
                _isLow  = true;
                _isHigh = false;
                _onMomentumLow?.Raise();
            }
            else if ((_isHigh || _isLow) && m > _lowThreshold && m < _highThreshold)
            {
                _isHigh = false;
                _isLow  = false;
                _onMomentumNeutral?.Raise();
            }
        }

        public void Reset()
        {
            _playerTimestamps.Clear();
            _botTimestamps.Clear();
            _isHigh = false;
            _isLow  = false;
        }
    }
}
