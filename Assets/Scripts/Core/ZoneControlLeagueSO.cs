using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Division tiers for the zone-control league system.</summary>
    public enum ZoneControlLeagueDivision
    {
        Bronze   = 0,
        Silver   = 1,
        Gold     = 2,
        Platinum = 3
    }

    /// <summary>
    /// Runtime ScriptableObject managing a seasonal zone-control league.
    /// Rating points are earned per star of match rating and accumulate toward
    /// promotion thresholds.  Falling below the relegation threshold drops the
    /// player one division.
    ///
    /// ── Promotion/Relegation flow ────────────────────────────────────────────
    ///   Call <see cref="AddRatingPoints"/> after each rated match.
    ///   The SO evaluates promotion (≥ <see cref="PromotionThreshold"/>) and
    ///   relegation (&lt; <see cref="RelegationThreshold"/>) automatically.
    ///   Promotion resets points to 0 and advances the division up to Platinum.
    ///   Relegation drops one division (clamped to Bronze) and adjusts points.
    ///
    /// ── Persistence ────────────────────────────────────────────────────────────
    ///   Use <see cref="LoadSnapshot"/> / <see cref="TakeSnapshot"/> with a
    ///   bootstrapper.  <see cref="Reset"/> clears all state silently.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use LoadSnapshot for persistence.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlLeague.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlLeague", order = 35)]
    public sealed class ZoneControlLeagueSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("League Settings")]
        [Tooltip("Points earned per star of match rating (e.g. 3-star = 3 × pointsPerRating).")]
        [Min(1)]
        [SerializeField] private int _pointsPerRating = 10;

        [Tooltip("Points required to promote to the next division.  Resets to 0 on promotion.")]
        [Min(1)]
        [SerializeField] private int _promotionThreshold = 100;

        [Tooltip("Point total below which the player is relegated one division.")]
        [Min(0)]
        [SerializeField] private int _relegationThreshold = 0;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLeagueUpdated;
        [SerializeField] private VoidGameEvent _onPromotion;
        [SerializeField] private VoidGameEvent _onRelegation;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private ZoneControlLeagueDivision _currentDivision;
        private int _currentPoints;
        private int _promotionCount;
        private int _relegationCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current league division.</summary>
        public ZoneControlLeagueDivision CurrentDivision => _currentDivision;

        /// <summary>Accumulated points toward the next promotion threshold.</summary>
        public int CurrentPoints     => _currentPoints;

        /// <summary>Total number of promotions earned.</summary>
        public int PromotionCount    => _promotionCount;

        /// <summary>Total number of relegations incurred.</summary>
        public int RelegationCount   => _relegationCount;

        /// <summary>Points earned per star of match rating.</summary>
        public int PointsPerRating   => _pointsPerRating;

        /// <summary>Points required to promote.</summary>
        public int PromotionThreshold  => _promotionThreshold;

        /// <summary>Points floor below which relegation triggers.</summary>
        public int RelegationThreshold => _relegationThreshold;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <c>rating × PointsPerRating</c> to the current point total,
        /// then evaluates promotion or relegation.
        /// Fires <see cref="_onLeagueUpdated"/> after adding points.
        /// </summary>
        /// <param name="rating">Match rating (typically 1–5 stars).</param>
        public void AddRatingPoints(int rating)
        {
            _currentPoints += rating * _pointsPerRating;
            _onLeagueUpdated?.Raise();
            EvaluatePromotion();
        }

        /// <summary>
        /// Restores persisted league state.  Bootstrapper-safe; no events fired.
        /// Division value is clamped to valid enum range.
        /// </summary>
        public void LoadSnapshot(int division, int points, int promotions, int relegations)
        {
            _currentDivision = (ZoneControlLeagueDivision)Mathf.Clamp(division, 0, 3);
            _currentPoints   = Mathf.Max(0, points);
            _promotionCount  = Mathf.Max(0, promotions);
            _relegationCount = Mathf.Max(0, relegations);
        }

        /// <summary>Returns all runtime fields as a value tuple for persistence.</summary>
        public (int division, int points, int promotions, int relegations) TakeSnapshot() =>
            ((int)_currentDivision, _currentPoints, _promotionCount, _relegationCount);

        /// <summary>
        /// Resets all runtime state silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _currentDivision = ZoneControlLeagueDivision.Bronze;
            _currentPoints   = 0;
            _promotionCount  = 0;
            _relegationCount = 0;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void EvaluatePromotion()
        {
            if (_currentPoints >= _promotionThreshold
                && _currentDivision < ZoneControlLeagueDivision.Platinum)
            {
                _currentDivision = (ZoneControlLeagueDivision)((int)_currentDivision + 1);
                _currentPoints   = 0;
                _promotionCount++;
                _onPromotion?.Raise();
            }
            else if (_currentPoints < _relegationThreshold
                     && _currentDivision > ZoneControlLeagueDivision.Bronze)
            {
                _currentDivision = (ZoneControlLeagueDivision)((int)_currentDivision - 1);
                _currentPoints   = Mathf.Max(0, _currentPoints);
                _relegationCount++;
                _onRelegation?.Raise();
            }
        }
    }
}
