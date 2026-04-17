using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that measures the player's zone-capture velocity
    /// (captures per second) over a rolling time window.
    ///
    /// Call <see cref="RecordCapture(float)"/> with <c>Time.time</c> each time the
    /// player captures a zone.  Call <see cref="Tick(float)"/> (also with
    /// <c>Time.time</c>) each frame to prune stale timestamps and re-evaluate
    /// velocity thresholds.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureVelocity.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureVelocity", order = 92)]
    public sealed class ZoneControlCaptureVelocitySO : ScriptableObject
    {
        [Header("Velocity Settings")]
        [Min(0.5f)]
        [SerializeField] private float _windowDuration = 5f;

        [Min(0.1f)]
        [SerializeField] private float _highVelocityThreshold = 1.0f;

        [Min(0f)]
        [SerializeField] private float _lowVelocityThreshold = 0.2f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHighVelocity;
        [SerializeField] private VoidGameEvent _onLowVelocity;
        [SerializeField] private VoidGameEvent _onVelocityNormal;

        private readonly List<float> _timestamps = new List<float>();
        private bool _isHighVelocity;
        private bool _isLowVelocity;

        private void OnEnable() => Reset();

        public float WindowDuration         => _windowDuration;
        public float HighVelocityThreshold  => _highVelocityThreshold;
        public float LowVelocityThreshold   => _lowVelocityThreshold;
        public bool  IsHighVelocity         => _isHighVelocity;
        public bool  IsLowVelocity          => _isLowVelocity;
        public int   CaptureCount           => _timestamps.Count;

        /// <summary>
        /// Returns the current capture velocity (captures/second) based on
        /// timestamps within the window ending at <paramref name="currentTime"/>.
        /// </summary>
        public float GetVelocity(float currentTime)
        {
            Prune(currentTime);
            return _windowDuration > 0f ? _timestamps.Count / _windowDuration : 0f;
        }

        /// <summary>
        /// Records a capture at <paramref name="timestamp"/> and re-evaluates
        /// velocity thresholds.
        /// </summary>
        public void RecordCapture(float timestamp)
        {
            _timestamps.Add(timestamp);
            Prune(timestamp);
            EvaluateVelocity(timestamp);
        }

        /// <summary>
        /// Prunes stale timestamps and re-evaluates velocity thresholds without
        /// adding a new capture.  Call once per frame with <c>Time.time</c>.
        /// </summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
            EvaluateVelocity(currentTime);
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _isHighVelocity = false;
            _isLowVelocity  = false;
        }

        // ── private helpers ───────────────────────────────────────────────────

        private void Prune(float currentTime)
        {
            float cutoff = currentTime - _windowDuration;
            _timestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateVelocity(float currentTime)
        {
            float velocity = _windowDuration > 0f
                ? _timestamps.Count / _windowDuration
                : 0f;

            bool nowHigh = velocity >= _highVelocityThreshold;
            bool nowLow  = velocity <= _lowVelocityThreshold && _timestamps.Count == 0;

            if (nowHigh && !_isHighVelocity)
            {
                _isHighVelocity = true;
                _isLowVelocity  = false;
                _onHighVelocity?.Raise();
            }
            else if (!nowHigh && _isHighVelocity)
            {
                _isHighVelocity = false;
                _onVelocityNormal?.Raise();
            }

            if (nowLow && !_isLowVelocity)
            {
                _isLowVelocity = true;
                _onLowVelocity?.Raise();
            }
            else if (!nowLow && _isLowVelocity)
            {
                _isLowVelocity = false;
            }
        }
    }
}
