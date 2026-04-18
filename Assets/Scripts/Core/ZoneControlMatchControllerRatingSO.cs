using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Computes a composite 0–100 match-controller rating from three normalised
    /// inputs: hold ratio, capture efficiency, and lead delta.
    /// Fires <c>_onRatingComputed</c> after each <see cref="ComputeRating"/> call.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchControllerRating.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchControllerRating", order = 134)]
    public sealed class ZoneControlMatchControllerRatingSO : ScriptableObject
    {
        [Header("Weights")]
        [SerializeField, Min(0f)] private float _holdRatioWeight   = 1f;
        [SerializeField, Min(0f)] private float _efficiencyWeight  = 1f;
        [SerializeField, Min(0f)] private float _leadDeltaWeight   = 1f;

        [Header("Lead Normalisation")]
        [SerializeField, Min(1)] private int _maxLeadDelta = 10;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRatingComputed;

        private int _lastRating;

        private void OnEnable() => Reset();

        public float HoldRatioWeight  => _holdRatioWeight;
        public float EfficiencyWeight => _efficiencyWeight;
        public float LeadDeltaWeight  => _leadDeltaWeight;
        public int   MaxLeadDelta     => _maxLeadDelta;
        public int   LastRating       => _lastRating;

        /// <summary>
        /// Computes a 0–100 rating.
        /// <paramref name="holdRatio"/> and <paramref name="captureEfficiency"/>
        /// are expected in [0,1]; <paramref name="leadDelta"/> is player-minus-bot
        /// capture count, clamped internally to [0, MaxLeadDelta].
        /// </summary>
        public int ComputeRating(float holdRatio, float captureEfficiency, int leadDelta)
        {
            float totalWeight = _holdRatioWeight + _efficiencyWeight + _leadDeltaWeight;
            if (totalWeight <= 0f)
            {
                _lastRating = 0;
                _onRatingComputed?.Raise();
                return 0;
            }

            float normHold       = Mathf.Clamp01(holdRatio);
            float normEfficiency = Mathf.Clamp01(captureEfficiency);
            float normLead       = _maxLeadDelta > 0
                ? Mathf.Clamp01((float)Mathf.Max(0, leadDelta) / _maxLeadDelta)
                : 0f;

            float weighted = (normHold * _holdRatioWeight
                            + normEfficiency * _efficiencyWeight
                            + normLead * _leadDeltaWeight) / totalWeight;

            _lastRating = Mathf.RoundToInt(weighted * 100f);
            _onRatingComputed?.Raise();
            return _lastRating;
        }

        public static string GetGradeLabel(int rating)
        {
            if (rating >= 90) return "A";
            if (rating >= 75) return "B";
            if (rating >= 55) return "C";
            if (rating >= 35) return "D";
            return "F";
        }

        public void Reset() => _lastRating = 0;
    }
}
