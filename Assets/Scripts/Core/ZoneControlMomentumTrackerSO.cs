using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks capture momentum within a sliding time window
    /// and detects "burst" moments when captures-per-window reach
    /// <c>_burstThreshold</c>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="RecordCapture"/> records a timestamped capture and evaluates burst state.
    ///   <see cref="Tick"/> prunes stale timestamps and re-evaluates burst state;
    ///   call each frame with <c>Time.time</c>.
    ///   Fires <c>_onBurstDetected</c> on false→true transition.
    ///   Fires <c>_onBurstEnded</c> on true→false transition.
    ///   <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMomentumTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMomentumTracker", order = 77)]
    public sealed class ZoneControlMomentumTrackerSO : ScriptableObject
    {
        [Header("Burst Settings")]
        [Min(2)]
        [SerializeField] private int _burstThreshold = 3;

        [Min(0.5f)]
        [SerializeField] private float _burstWindow = 10f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBurstDetected;
        [SerializeField] private VoidGameEvent _onBurstEnded;

        private readonly List<float> _timestamps = new List<float>();
        private bool _isBurst;

        private void OnEnable() => Reset();

        public bool  IsBurst         => _isBurst;
        public int   BurstThreshold  => _burstThreshold;
        public float BurstWindow     => _burstWindow;
        public int   CaptureCount    => _timestamps.Count;

        /// <summary>Records a capture at <paramref name="timestamp"/> and evaluates burst state.</summary>
        public void RecordCapture(float timestamp)
        {
            Prune(timestamp);
            _timestamps.Add(timestamp);
            EvaluateBurst();
        }

        /// <summary>Prunes stale timestamps and re-evaluates burst state.</summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
            EvaluateBurst();
        }

        /// <summary>Returns the number of captures within the window ending at <paramref name="currentTime"/>.</summary>
        public int GetCapturesInWindow(float currentTime)
        {
            Prune(currentTime);
            return _timestamps.Count;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _isBurst = false;
        }

        private void Prune(float referenceTime)
        {
            float cutoff = referenceTime - _burstWindow;
            _timestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateBurst()
        {
            bool wasBurst = _isBurst;
            _isBurst = _timestamps.Count >= _burstThreshold;
            if (!wasBurst && _isBurst)
                _onBurstDetected?.Raise();
            else if (wasBurst && !_isBurst)
                _onBurstEnded?.Raise();
        }
    }
}
