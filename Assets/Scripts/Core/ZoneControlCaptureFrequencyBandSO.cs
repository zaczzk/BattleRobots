using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Categorises a captures-per-minute rate into one of four frequency bands.</summary>
    public enum CaptureFrequencyBand
    {
        Low     = 0,
        Medium  = 1,
        High    = 2,
        Extreme = 3,
    }

    /// <summary>
    /// Runtime SO that maps a captures-per-minute rate to a
    /// <see cref="CaptureFrequencyBand"/> (Low / Medium / High / Extreme).
    ///
    /// Fires <c>_onBandChanged</c> only when the band value changes.
    /// <see cref="Reset"/> returns to <c>Low</c> silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureFrequencyBand.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFrequencyBand", order = 92)]
    public sealed class ZoneControlCaptureFrequencyBandSO : ScriptableObject
    {
        [Header("Band Thresholds (captures per minute)")]
        [Min(0.1f)]
        [SerializeField] private float _mediumThreshold  = 1f;

        [Min(0.1f)]
        [SerializeField] private float _highThreshold    = 2f;

        [Min(0.1f)]
        [SerializeField] private float _extremeThreshold = 4f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBandChanged;

        private CaptureFrequencyBand _currentBand;

        private void OnEnable() => Reset();

        public CaptureFrequencyBand CurrentBand       => _currentBand;
        public float                MediumThreshold   => _mediumThreshold;
        public float                HighThreshold     => _highThreshold;
        public float                ExtremeThreshold  => _extremeThreshold;

        /// <summary>
        /// Evaluates <paramref name="capturesPerMinute"/> against thresholds and
        /// updates <see cref="CurrentBand"/>; fires <c>_onBandChanged</c> on change.
        /// </summary>
        public CaptureFrequencyBand EvaluateBand(float capturesPerMinute)
        {
            CaptureFrequencyBand newBand;

            if      (capturesPerMinute >= _extremeThreshold) newBand = CaptureFrequencyBand.Extreme;
            else if (capturesPerMinute >= _highThreshold)    newBand = CaptureFrequencyBand.High;
            else if (capturesPerMinute >= _mediumThreshold)  newBand = CaptureFrequencyBand.Medium;
            else                                             newBand = CaptureFrequencyBand.Low;

            if (newBand != _currentBand)
            {
                _currentBand = newBand;
                _onBandChanged?.Raise();
            }

            return _currentBand;
        }

        /// <summary>Returns a display label for the current band.</summary>
        public string GetBandLabel()
        {
            switch (_currentBand)
            {
                case CaptureFrequencyBand.Extreme: return "Extreme";
                case CaptureFrequencyBand.High:    return "High";
                case CaptureFrequencyBand.Medium:  return "Medium";
                default:                           return "Low";
            }
        }

        /// <summary>Resets to <c>Low</c> silently (no event).</summary>
        public void Reset()
        {
            _currentBand = CaptureFrequencyBand.Low;
        }

        private void OnValidate()
        {
            _mediumThreshold  = Mathf.Max(0.1f, _mediumThreshold);
            _highThreshold    = Mathf.Max(_mediumThreshold, _highThreshold);
            _extremeThreshold = Mathf.Max(_highThreshold,   _extremeThreshold);
        }
    }
}
