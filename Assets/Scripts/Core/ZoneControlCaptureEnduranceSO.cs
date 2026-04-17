using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks whether the player sustains a minimum capture
    /// frequency over a rolling time window.
    ///
    /// Fires <c>_onEnduranceAchieved</c> when capture count first reaches
    /// <c>_requiredCaptures</c> within the window (false→true transition).
    /// Fires <c>_onEnduranceLost</c> when count drops below threshold (true→false).
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureEndurance.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEndurance", order = 79)]
    public sealed class ZoneControlCaptureEnduranceSO : ScriptableObject
    {
        [Header("Endurance Settings")]
        [Min(1)]
        [SerializeField] private int _requiredCaptures = 5;

        [Min(1f)]
        [SerializeField] private float _enduranceWindow = 30f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEnduranceAchieved;
        [SerializeField] private VoidGameEvent _onEnduranceLost;

        private readonly List<float> _timestamps = new List<float>();
        private bool _isEnduring;

        private void OnEnable() => Reset();

        public bool  IsEnduring       => _isEnduring;
        public int   RequiredCaptures => _requiredCaptures;
        public float EnduranceWindow  => _enduranceWindow;
        public int   CaptureCount     => _timestamps.Count;

        /// <summary>Normalised progress toward the required capture count [0, 1].</summary>
        public float Progress =>
            Mathf.Clamp01((float)_timestamps.Count / Mathf.Max(1, _requiredCaptures));

        /// <summary>Records a capture at <paramref name="timestamp"/> and evaluates endurance state.</summary>
        public void RecordCapture(float timestamp)
        {
            Prune(timestamp);
            _timestamps.Add(timestamp);
            EvaluateEndurance();
        }

        /// <summary>Prunes stale timestamps and re-evaluates endurance state.</summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
            EvaluateEndurance();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _isEnduring = false;
        }

        private void Prune(float referenceTime)
        {
            float cutoff = referenceTime - _enduranceWindow;
            _timestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateEndurance()
        {
            bool wasEnduring = _isEnduring;
            _isEnduring = _timestamps.Count >= _requiredCaptures;
            if (!wasEnduring && _isEnduring)
                _onEnduranceAchieved?.Raise();
            else if (wasEnduring && !_isEnduring)
                _onEnduranceLost?.Raise();
        }
    }
}
