using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Accumulates the total and average value earned from zone captures.
    /// Callers supply the value amount per capture via <c>RecordCapture(int)</c>.
    /// <c>AverageValue</c> is the mean value per capture (0 when no captures).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureValueTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureValueTracker", order = 124)]
    public sealed class ZoneControlCaptureValueTrackerSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onValueUpdated;

        private int _captureCount;
        private int _totalValue;

        private void OnEnable() => Reset();

        public int   CaptureCount => _captureCount;
        public int   TotalValue   => _totalValue;

        /// <summary>Mean value per capture; returns 0 when no captures recorded.</summary>
        public float AverageValue =>
            _captureCount > 0 ? (float)_totalValue / _captureCount : 0f;

        /// <summary>Records a capture and adds <paramref name="value"/> to the total.</summary>
        public void RecordCapture(int value)
        {
            if (value < 0) value = 0;
            _captureCount++;
            _totalValue += value;
            _onValueUpdated?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _captureCount = 0;
            _totalValue   = 0;
        }
    }
}
