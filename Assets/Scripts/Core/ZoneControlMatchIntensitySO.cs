using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that computes a composite match intensity score [0–10] from
    /// four inputs: surge active (bool), frenzy active (bool), endurance active
    /// (bool), and a normalised capture velocity [0,1].
    ///
    /// Each active boolean contributes <see cref="BoolWeight"/> (default 2.5) and
    /// the velocity contributes <c>VelocityWeight × normalisedVelocity</c>
    /// (default 2.5).  The sum is clamped to [0,10].
    ///
    /// Call <see cref="ComputeIntensity"/> to evaluate and cache the score.
    /// Fires <c>_onIntensityChanged</c> on each computation.
    /// <see cref="Reset"/> clears the cached score silently.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchIntensity.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchIntensity", order = 98)]
    public sealed class ZoneControlMatchIntensitySO : ScriptableObject
    {
        [Header("Intensity Weights")]
        [Min(0f)]
        [SerializeField] private float _boolWeight     = 2.5f;

        [Min(0f)]
        [SerializeField] private float _velocityWeight = 2.5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onIntensityChanged;

        private float _lastIntensity;

        private void OnEnable() => Reset();

        public float LastIntensity   => _lastIntensity;
        public float BoolWeight      => _boolWeight;
        public float VelocityWeight  => _velocityWeight;

        /// <summary>
        /// Computes the composite intensity score from the four inputs, caches it,
        /// and fires <c>_onIntensityChanged</c>.
        /// </summary>
        /// <param name="isSurging">Whether a surge is currently active.</param>
        /// <param name="isFrenzy">Whether a frenzy is currently active.</param>
        /// <param name="isEnduring">Whether endurance is currently achieved.</param>
        /// <param name="normalisedVelocity">Capture velocity normalised to [0,1].</param>
        /// <returns>Intensity score in [0,10].</returns>
        public float ComputeIntensity(bool isSurging, bool isFrenzy, bool isEnduring, float normalisedVelocity)
        {
            float score = 0f;

            if (isSurging)  score += _boolWeight;
            if (isFrenzy)   score += _boolWeight;
            if (isEnduring) score += _boolWeight;

            score += _velocityWeight * Mathf.Clamp01(normalisedVelocity);

            _lastIntensity = Mathf.Clamp(score, 0f, 10f);
            _onIntensityChanged?.Raise();
            return _lastIntensity;
        }

        /// <summary>Clears the cached intensity score silently.</summary>
        public void Reset()
        {
            _lastIntensity = 0f;
        }

        private void OnValidate()
        {
            _boolWeight     = Mathf.Max(0f, _boolWeight);
            _velocityWeight = Mathf.Max(0f, _velocityWeight);
        }
    }
}
