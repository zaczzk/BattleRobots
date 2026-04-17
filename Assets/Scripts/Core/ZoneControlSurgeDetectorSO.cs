using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that detects "surge" moments when captures within a sliding
    /// time window reach <c>_surgeThreshold</c>.
    ///
    /// Fires <c>_onSurgeStarted</c> on false→true transition.
    /// Fires <c>_onSurgeEnded</c> on true→false transition.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlSurgeDetector.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlSurgeDetector", order = 78)]
    public sealed class ZoneControlSurgeDetectorSO : ScriptableObject
    {
        [Header("Surge Settings")]
        [Min(2)]
        [SerializeField] private int _surgeThreshold = 4;

        [Min(0.5f)]
        [SerializeField] private float _surgeWindow = 8f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSurgeStarted;
        [SerializeField] private VoidGameEvent _onSurgeEnded;

        private readonly List<float> _timestamps = new List<float>();
        private bool _isSurging;

        private void OnEnable() => Reset();

        public bool  IsSurging      => _isSurging;
        public int   SurgeThreshold => _surgeThreshold;
        public float SurgeWindow    => _surgeWindow;
        public int   CaptureCount   => _timestamps.Count;

        /// <summary>Records a capture at <paramref name="timestamp"/> and evaluates surge state.</summary>
        public void RecordCapture(float timestamp)
        {
            Prune(timestamp);
            _timestamps.Add(timestamp);
            EvaluateSurge();
        }

        /// <summary>Prunes stale timestamps and re-evaluates surge state.</summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
            EvaluateSurge();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _isSurging = false;
        }

        private void Prune(float referenceTime)
        {
            float cutoff = referenceTime - _surgeWindow;
            _timestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateSurge()
        {
            bool wasSurging = _isSurging;
            _isSurging = _timestamps.Count >= _surgeThreshold;
            if (!wasSurging && _isSurging)
                _onSurgeStarted?.Raise();
            else if (wasSurging && !_isSurging)
                _onSurgeEnded?.Raise();
        }
    }
}
