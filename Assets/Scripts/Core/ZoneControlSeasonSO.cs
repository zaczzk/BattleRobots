using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject tracking multiple zone-control league seasons.
    /// Each call to <see cref="EndSeason"/> records the division reached that season
    /// and updates the all-time highest division.  A reward tier (1–4) is derived
    /// directly from the highest division reached in the concluded season:
    /// Bronze = 1, Silver = 2, Gold = 3, Platinum = 4.
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
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlSeason.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlSeason", order = 36)]
    public sealed class ZoneControlSeasonSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once a season has been concluded via EndSeason.")]
        [SerializeField] private VoidGameEvent _onSeasonEnded;

        [Tooltip("Raised after any state mutation (EndSeason, LoadSnapshot reset path).")]
        [SerializeField] private VoidGameEvent _onSeasonUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int                        _seasonCount;
        private ZoneControlLeagueDivision  _highestDivision;
        private readonly List<int>         _divisionHistory = new List<int>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of seasons concluded so far.</summary>
        public int SeasonCount => _seasonCount;

        /// <summary>Highest division reached across all seasons.</summary>
        public ZoneControlLeagueDivision HighestDivision => _highestDivision;

        /// <summary>Number of seasons recorded in the division history list.</summary>
        public int HistoryCount => _divisionHistory.Count;

        /// <summary>
        /// Reward tier (1–4) based on the division reached in the most recent season.
        /// Returns 0 when no season has been concluded.
        /// </summary>
        public int LatestRewardTier =>
            _seasonCount > 0
                ? GetRewardTier((ZoneControlLeagueDivision)_divisionHistory[_divisionHistory.Count - 1])
                : 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Maps a <see cref="ZoneControlLeagueDivision"/> to a reward tier (1–4).
        /// Bronze = 1, Silver = 2, Gold = 3, Platinum = 4.
        /// </summary>
        public static int GetRewardTier(ZoneControlLeagueDivision division) =>
            (int)division + 1;

        /// <summary>
        /// Concludes a season with the given division reached.
        /// Updates <see cref="HighestDivision"/> when <paramref name="divisionReached"/>
        /// exceeds the previous best.
        /// Fires <see cref="_onSeasonUpdated"/> then <see cref="_onSeasonEnded"/>.
        /// </summary>
        public void EndSeason(ZoneControlLeagueDivision divisionReached)
        {
            _seasonCount++;
            if ((int)divisionReached > (int)_highestDivision)
                _highestDivision = divisionReached;
            _divisionHistory.Add((int)divisionReached);
            _onSeasonUpdated?.Raise();
            _onSeasonEnded?.Raise();
        }

        /// <summary>
        /// Returns the division recorded for the given season index (0 = oldest).
        /// Returns <see cref="ZoneControlLeagueDivision.Bronze"/> when the index
        /// is out of range.
        /// </summary>
        public ZoneControlLeagueDivision GetSeasonDivision(int seasonIndex)
        {
            if (seasonIndex < 0 || seasonIndex >= _divisionHistory.Count)
                return ZoneControlLeagueDivision.Bronze;
            return (ZoneControlLeagueDivision)_divisionHistory[seasonIndex];
        }

        /// <summary>
        /// Restores persisted season state.  Bootstrapper-safe; no events fired.
        /// All values are clamped to valid ranges.
        /// </summary>
        public void LoadSnapshot(int seasonCount, int highestDivision, IReadOnlyList<int> history)
        {
            _seasonCount     = Mathf.Max(0, seasonCount);
            _highestDivision = (ZoneControlLeagueDivision)Mathf.Clamp(highestDivision, 0, 3);
            _divisionHistory.Clear();
            if (history != null)
                foreach (int d in history)
                    _divisionHistory.Add(Mathf.Clamp(d, 0, 3));
        }

        /// <summary>Returns all runtime fields as a value tuple for persistence.</summary>
        public (int seasonCount, int highestDivision, IReadOnlyList<int> history) TakeSnapshot() =>
            (_seasonCount, (int)_highestDivision, _divisionHistory);

        /// <summary>
        /// Resets all runtime state silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _seasonCount     = 0;
            _highestDivision = ZoneControlLeagueDivision.Bronze;
            _divisionHistory.Clear();
        }
    }
}
