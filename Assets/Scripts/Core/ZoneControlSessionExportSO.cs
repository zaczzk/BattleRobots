using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// JSON payload produced by <see cref="ZoneControlSessionExportSO.ExportSession"/>.
    /// All fields are value types or primitive collections serialisable by JsonUtility.
    /// </summary>
    [Serializable]
    public sealed class ZoneControlSessionPayload
    {
        public int         TotalZonesCaptured;
        public int         MatchesPlayed;
        public int         BestCaptureStreak;
        public List<int>   RatingHistory   = new List<int>();
        public List<bool>  RoundResultWins = new List<bool>();
    }

    /// <summary>
    /// Runtime ScriptableObject that serialises the current zone-control session
    /// (summary, round results, and rating history) to a JSON string.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="ExportSession"/> with the three data SOs.
    ///   The method builds a <see cref="ZoneControlSessionPayload"/>, serialises it
    ///   via <c>JsonUtility.ToJson</c>, caches the result in <see cref="LastExportJson"/>,
    ///   and fires <see cref="_onExportCompleted"/>.
    ///   Call <see cref="Reset"/> to clear the cache.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at session start.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlSessionExport.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlSessionExport", order = 56)]
    public sealed class ZoneControlSessionExportSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after a successful ExportSession call.")]
        [SerializeField] private VoidGameEvent _onExportCompleted;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private string _lastExportJson = string.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// The JSON string produced by the most recent <see cref="ExportSession"/> call.
        /// Empty string when no export has been performed or after <see cref="Reset"/>.
        /// </summary>
        public string LastExportJson => _lastExportJson;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises data from the supplied SOs into a JSON string, caches it in
        /// <see cref="LastExportJson"/>, and fires <see cref="_onExportCompleted"/>.
        /// Null SOs contribute default/empty values; the export always succeeds.
        /// </summary>
        public void ExportSession(
            ZoneControlSessionSummarySO   summarySO,
            ZoneControlRoundResultSO      roundResultSO,
            ZoneControlMatchRatingHistorySO ratingHistorySO)
        {
            var payload = new ZoneControlSessionPayload();

            if (summarySO != null)
            {
                payload.TotalZonesCaptured = summarySO.TotalZonesCaptured;
                payload.MatchesPlayed      = summarySO.MatchesPlayed;
                payload.BestCaptureStreak  = summarySO.BestCaptureStreak;
            }

            if (ratingHistorySO != null)
            {
                IReadOnlyList<int> ratings = ratingHistorySO.GetRatings();
                for (int i = 0; i < ratings.Count; i++)
                    payload.RatingHistory.Add(ratings[i]);
            }

            if (roundResultSO != null)
            {
                IReadOnlyList<ZoneControlRoundResultEntry> results = roundResultSO.GetResults();
                for (int i = 0; i < results.Count; i++)
                    payload.RoundResultWins.Add(results[i].PlayerWon);
            }

            _lastExportJson = JsonUtility.ToJson(payload);
            _onExportCompleted?.Raise();
        }

        /// <summary>
        /// Clears the cached JSON string silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _lastExportJson = string.Empty;
        }
    }
}
