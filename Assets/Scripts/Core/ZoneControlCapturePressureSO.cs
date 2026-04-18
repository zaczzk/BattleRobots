using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks capture pressure as the ratio of bot captures to total captures
    /// within a sliding time window.  High pressure fires <c>_onHighPressure</c>; returning
    /// to normal fires <c>_onPressureNormal</c>.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCapturePressure.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePressure", order = 110)]
    public sealed class ZoneControlCapturePressureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)]  private float _windowDuration        = 30f;
        [SerializeField, Range(0f, 1f)] private float _highPressureThreshold = 0.6f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHighPressure;
        [SerializeField] private VoidGameEvent _onPressureNormal;

        private readonly List<float> _playerTimestamps = new List<float>();
        private readonly List<float> _botTimestamps    = new List<float>();
        private bool _isHighPressure;

        private void OnEnable() => Reset();

        public float WindowDuration        => _windowDuration;
        public float HighPressureThreshold => _highPressureThreshold;
        public bool  IsHighPressure        => _isHighPressure;
        public int   PlayerCaptureCount    => _playerTimestamps.Count;
        public int   BotCaptureCount       => _botTimestamps.Count;

        public float PressureRatio
        {
            get
            {
                int total = _playerTimestamps.Count + _botTimestamps.Count;
                if (total <= 0) return 0f;
                return Mathf.Clamp01((float)_botTimestamps.Count / total);
            }
        }

        /// <summary>Records a player zone capture at time <paramref name="t"/>.</summary>
        public void RecordPlayerCapture(float t)
        {
            Prune(t);
            _playerTimestamps.Add(t);
            EvaluatePressure();
        }

        /// <summary>Records a bot zone capture at time <paramref name="t"/>.</summary>
        public void RecordBotCapture(float t)
        {
            Prune(t);
            _botTimestamps.Add(t);
            EvaluatePressure();
        }

        /// <summary>Prunes stale entries and re-evaluates pressure state.</summary>
        public void Tick(float t)
        {
            Prune(t);
            EvaluatePressure();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _playerTimestamps.Clear();
            _botTimestamps.Clear();
            _isHighPressure = false;
        }

        private void Prune(float t)
        {
            float cutoff = t - _windowDuration;
            _playerTimestamps.RemoveAll(ts => ts < cutoff);
            _botTimestamps.RemoveAll(ts => ts < cutoff);
        }

        private void EvaluatePressure()
        {
            bool high = PressureRatio >= _highPressureThreshold;
            if (high && !_isHighPressure)
            {
                _isHighPressure = true;
                _onHighPressure?.Raise();
            }
            else if (!high && _isHighPressure)
            {
                _isHighPressure = false;
                _onPressureNormal?.Raise();
            }
        }
    }
}
