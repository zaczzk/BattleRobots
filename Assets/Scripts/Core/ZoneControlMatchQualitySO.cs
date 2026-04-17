using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that computes a composite 0–100 match-quality score from
    /// three normalised inputs: total zones captured, capture pace, and match rating.
    ///
    /// ── Formula ────────────────────────────────────────────────────────────────
    ///   Each input is normalised to [0,1] using its configured divisor/max,
    ///   weighted, and averaged → multiplied by 100 → rounded to int.
    ///   Zero total weight → returns 0.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchQuality.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchQuality", order = 77)]
    public sealed class ZoneControlMatchQualitySO : ScriptableObject
    {
        [Header("Normalisation Divisors")]
        [Min(1)]
        [SerializeField] private int _zoneScaleDivisor = 20;

        [Min(0.1f)]
        [SerializeField] private float _paceScaleDivisor = 5f;

        [Min(1)]
        [SerializeField] private int _maxRating = 5;

        [Header("Weights")]
        [Min(0f)]
        [SerializeField] private float _zoneWeight = 1f;

        [Min(0f)]
        [SerializeField] private float _paceWeight = 1f;

        [Min(0f)]
        [SerializeField] private float _ratingWeight = 1f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onQualityComputed;

        private int _lastQuality;

        private void OnEnable() => Reset();

        public int   LastQuality       => _lastQuality;
        public int   ZoneScaleDivisor  => _zoneScaleDivisor;
        public float PaceScaleDivisor  => _paceScaleDivisor;
        public int   MaxRating         => _maxRating;
        public float ZoneWeight        => _zoneWeight;
        public float PaceWeight        => _paceWeight;
        public float RatingWeight      => _ratingWeight;

        /// <summary>
        /// Computes and caches a quality score in [0, 100].
        /// Fires <c>_onQualityComputed</c> and returns the score.
        /// </summary>
        public int ComputeQuality(int totalZones, float pace, int rating)
        {
            float totalWeight = _zoneWeight + _paceWeight + _ratingWeight;
            if (totalWeight <= 0f)
            {
                _lastQuality = 0;
                _onQualityComputed?.Raise();
                return 0;
            }

            float zoneNorm   = Mathf.Clamp01(Mathf.Max(0, totalZones) / (float)Mathf.Max(1, _zoneScaleDivisor));
            float paceNorm   = Mathf.Clamp01(Mathf.Max(0f, pace) / Mathf.Max(0.001f, _paceScaleDivisor));
            float ratingNorm = Mathf.Clamp01(Mathf.Max(0, rating) / (float)Mathf.Max(1, _maxRating));

            float weighted = (zoneNorm * _zoneWeight + paceNorm * _paceWeight + ratingNorm * _ratingWeight)
                             / totalWeight;

            _lastQuality = Mathf.RoundToInt(weighted * 100f);
            _onQualityComputed?.Raise();
            return _lastQuality;
        }

        /// <summary>Clears <c>_lastQuality</c> silently.</summary>
        public void Reset() => _lastQuality = 0;
    }
}
