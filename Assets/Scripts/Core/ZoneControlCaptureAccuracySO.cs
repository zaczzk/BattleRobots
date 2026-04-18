using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks player zone-capture accuracy: the ratio of successful
    /// captures to total attempts.  <see cref="RecordAttempt"/> increments attempts;
    /// <see cref="RecordSuccess"/> increments successes and fires <c>_onAccuracyChanged</c>.
    /// <see cref="Accuracy"/> is clamped to [0,1] and is 0 when no attempts have been made.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureAccuracy.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAccuracy", order = 108)]
    public sealed class ZoneControlCaptureAccuracySO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAccuracyChanged;

        private int _totalAttempts;
        private int _totalSuccesses;

        private void OnEnable() => Reset();

        public int   TotalAttempts  => _totalAttempts;
        public int   TotalSuccesses => _totalSuccesses;

        /// <summary>Accuracy ratio [0,1]; 0 when no attempts recorded.</summary>
        public float Accuracy =>
            _totalAttempts > 0
                ? Mathf.Clamp01((float)_totalSuccesses / _totalAttempts)
                : 0f;

        /// <summary>Records one capture attempt.</summary>
        public void RecordAttempt() => _totalAttempts++;

        /// <summary>Records a successful capture and fires <c>_onAccuracyChanged</c>.</summary>
        public void RecordSuccess()
        {
            _totalSuccesses++;
            _onAccuracyChanged?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _totalAttempts  = 0;
            _totalSuccesses = 0;
        }
    }
}
