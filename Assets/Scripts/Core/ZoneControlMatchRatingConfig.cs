using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data-only ScriptableObject that defines the thresholds used to compute a
    /// 1–5 star match rating from zone-control career statistics.
    ///
    /// ── Rating formula ─────────────────────────────────────────────────────────
    ///   Base rating is determined by <c>totalZonesCaptured</c>:
    ///     ★     &lt; MinZonesForRating2
    ///     ★★    ≥ MinZonesForRating2
    ///     ★★★   ≥ MinZonesForRating3
    ///     ★★★★  ≥ MinZonesForRating4
    ///     ★★★★★ ≥ MinZonesForRating5
    ///   +1 bonus (capped at 5) when either:
    ///     - <c>bestStreak</c> ≥ <see cref="MinStreakForBonus"/>, or
    ///     - <c>dominanceMatches</c> ≥ <see cref="MinDominanceMatchesForBonus"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime — all fields are serialised, no runtime state.
    ///   - Zero heap allocation on <see cref="ComputeRating"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchRatingConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchRatingConfig", order = 24)]
    public sealed class ZoneControlMatchRatingConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zone Capture Thresholds (total zones across session)")]
        [Tooltip("Minimum total zones captured to achieve a 2-star base rating.")]
        [SerializeField, Min(0)] private int _minZonesForRating2 = 3;

        [Tooltip("Minimum total zones captured to achieve a 3-star base rating.")]
        [SerializeField, Min(0)] private int _minZonesForRating3 = 6;

        [Tooltip("Minimum total zones captured to achieve a 4-star base rating.")]
        [SerializeField, Min(0)] private int _minZonesForRating4 = 10;

        [Tooltip("Minimum total zones captured to achieve a 5-star base rating.")]
        [SerializeField, Min(0)] private int _minZonesForRating5 = 15;

        [Header("Bonus Thresholds")]
        [Tooltip("Best consecutive capture streak required to earn the +1 star bonus.")]
        [SerializeField, Min(0)] private int _minStreakForBonus = 3;

        [Tooltip("Number of matches where the player held zone majority " +
                 "required to earn the +1 star bonus.")]
        [SerializeField, Min(0)] private int _minDominanceMatchesForBonus = 2;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Zones required for 2-star base rating.</summary>
        public int MinZonesForRating2 => _minZonesForRating2;

        /// <summary>Zones required for 3-star base rating.</summary>
        public int MinZonesForRating3 => _minZonesForRating3;

        /// <summary>Zones required for 4-star base rating.</summary>
        public int MinZonesForRating4 => _minZonesForRating4;

        /// <summary>Zones required for 5-star base rating.</summary>
        public int MinZonesForRating5 => _minZonesForRating5;

        /// <summary>Best streak required to earn the +1 bonus star.</summary>
        public int MinStreakForBonus => _minStreakForBonus;

        /// <summary>Dominance matches required to earn the +1 bonus star.</summary>
        public int MinDominanceMatchesForBonus => _minDominanceMatchesForBonus;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes a 1–5 star rating from the supplied session statistics.
        /// Zero allocation.
        /// </summary>
        /// <param name="totalZonesCaptured">Cumulative zones captured this session.</param>
        /// <param name="bestStreak">Highest consecutive capture streak achieved.</param>
        /// <param name="dominanceMatches">Matches where the player held zone majority.</param>
        /// <returns>An integer rating in [1, 5].</returns>
        public int ComputeRating(int totalZonesCaptured, int bestStreak, int dominanceMatches)
        {
            int rating;

            if      (totalZonesCaptured >= _minZonesForRating5) rating = 5;
            else if (totalZonesCaptured >= _minZonesForRating4) rating = 4;
            else if (totalZonesCaptured >= _minZonesForRating3) rating = 3;
            else if (totalZonesCaptured >= _minZonesForRating2) rating = 2;
            else                                                rating = 1;

            bool hasBonus = bestStreak >= _minStreakForBonus ||
                            dominanceMatches >= _minDominanceMatchesForBonus;

            if (hasBonus) rating = Mathf.Min(5, rating + 1);

            return rating;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            _minZonesForRating2 = Mathf.Max(0, _minZonesForRating2);
            _minZonesForRating3 = Mathf.Max(0, _minZonesForRating3);
            _minZonesForRating4 = Mathf.Max(0, _minZonesForRating4);
            _minZonesForRating5 = Mathf.Max(0, _minZonesForRating5);
            _minStreakForBonus  = Mathf.Max(0, _minStreakForBonus);
            _minDominanceMatchesForBonus = Mathf.Max(0, _minDominanceMatchesForBonus);
        }
    }
}
