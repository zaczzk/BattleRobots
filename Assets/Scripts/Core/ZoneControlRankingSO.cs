using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Rank levels awarded by <see cref="ZoneControlRankingSO"/>.
    /// </summary>
    public enum ZoneControlRankLevel
    {
        Unranked  = 0,
        Bronze    = 1,
        Silver    = 2,
        Gold      = 3,
        Platinum  = 4,
        Diamond   = 5,
    }

    /// <summary>
    /// Runtime ScriptableObject that assigns a ranked title to the player based on
    /// their cumulative <see cref="ZoneControlSessionSummarySO.TotalZonesCaptured"/>
    /// and the number of unlocked progression tiers.
    ///
    /// ── Rank logic ─────────────────────────────────────────────────────────────
    ///   <see cref="EvaluateRank"/> receives total zones and unlocked tiers.
    ///   The rank is the highest <see cref="ZoneControlRankLevel"/> whose combined
    ///   threshold is met.  Rank advances when either <paramref name="totalZones"/>
    ///   reaches the zone threshold OR <paramref name="unlockedTiers"/> reaches the
    ///   tier requirement — whichever is easier.  When the rank improves
    ///   <see cref="_onRankChanged"/> fires (idempotent thereafter).
    ///
    /// ── Rank thresholds (defaults) ──────────────────────────────────────────────
    ///   Unranked → Bronze   : 10 zones  OR 1 tier
    ///   Bronze   → Silver   : 25 zones  OR 2 tiers
    ///   Silver   → Gold     : 50 zones  OR 3 tiers
    ///   Gold     → Platinum : 100 zones OR 4 tiers
    ///   Platinum → Diamond  : 200 zones OR 5 tiers
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — use LoadSnapshot for persistence.
    ///   - Zero heap allocation on EvaluateRank hot path.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlRanking.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRanking", order = 29)]
    public sealed class ZoneControlRankingSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zone Thresholds (Unranked→Bronze, Bronze→Silver, …)")]
        [Tooltip("Minimum cumulative zones to reach each rank. " +
                 "5 entries for Bronze/Silver/Gold/Platinum/Diamond.")]
        [SerializeField] private int[] _zoneThresholds = { 10, 25, 50, 100, 200 };

        [Header("Tier Thresholds (Unranked→Bronze, Bronze→Silver, …)")]
        [Tooltip("Alternative minimum unlocked tiers to reach each rank.")]
        [SerializeField] private int[] _tierThresholds = { 1, 2, 3, 4, 5 };

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the player's rank improves.")]
        [SerializeField] private VoidGameEvent _onRankChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private ZoneControlRankLevel _currentRank = ZoneControlRankLevel.Unranked;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The player's current rank level.</summary>
        public ZoneControlRankLevel CurrentRank => _currentRank;

        /// <summary>Human-readable label for the current rank.</summary>
        public string GetRankLabel() => _currentRank.ToString();

        /// <summary>
        /// The cumulative zone count required to reach the next rank.
        /// Returns <c>-1</c> when the player is already at Diamond.
        /// </summary>
        public int GetNextZoneThreshold()
        {
            int nextIndex = (int)_currentRank; // next rank index = current + 1, base 0
            if (_zoneThresholds == null || nextIndex >= _zoneThresholds.Length)
                return -1;
            return _zoneThresholds[nextIndex];
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the player's rank from cumulative zone captures and unlocked tiers.
        /// Fires <see cref="_onRankChanged"/> if the rank improves.
        /// Idempotent when called with the same or lower values.
        /// Zero heap allocation.
        /// </summary>
        /// <param name="totalZones">Cumulative zones captured.</param>
        /// <param name="unlockedTiers">Number of progression tiers unlocked.</param>
        public void EvaluateRank(int totalZones, int unlockedTiers)
        {
            int maxRankIndex = 0; // Unranked = index 0

            int zoneLen = _zoneThresholds != null ? _zoneThresholds.Length : 0;
            int tierLen = _tierThresholds != null ? _tierThresholds.Length : 0;
            int maxLen  = Mathf.Max(zoneLen, tierLen);

            for (int i = 0; i < maxLen; i++)
            {
                bool meetsZone = i < zoneLen && totalZones >= _zoneThresholds[i];
                bool meetsTier = i < tierLen && unlockedTiers >= _tierThresholds[i];
                if (meetsZone || meetsTier)
                    maxRankIndex = i + 1; // +1 because Unranked=0, Bronze=1, …
                else
                    break;
            }

            // Clamp to valid enum range [Unranked, Diamond]
            var newRank = (ZoneControlRankLevel)Mathf.Clamp(maxRankIndex, 0, 5);

            if (newRank > _currentRank)
            {
                _currentRank = newRank;
                _onRankChanged?.Raise();
            }
        }

        /// <summary>
        /// Restores the player's rank from persisted data.
        /// Bootstrapper-safe; does not fire any events.
        /// </summary>
        public void LoadSnapshot(int rankLevel)
        {
            _currentRank = (ZoneControlRankLevel)Mathf.Clamp(rankLevel, 0, 5);
        }

        /// <summary>Returns the current rank level as an int for persistence.</summary>
        public int TakeSnapshot() => (int)_currentRank;

        /// <summary>
        /// Resets rank to Unranked.
        /// Does not fire any events.
        /// </summary>
        public void Reset()
        {
            _currentRank = ZoneControlRankLevel.Unranked;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_zoneThresholds != null)
                for (int i = 0; i < _zoneThresholds.Length; i++)
                    _zoneThresholds[i] = Mathf.Max(0, _zoneThresholds[i]);

            if (_tierThresholds != null)
                for (int i = 0; i < _tierThresholds.Length; i++)
                    _tierThresholds[i] = Mathf.Max(0, _tierThresholds[i]);
        }
    }
}
