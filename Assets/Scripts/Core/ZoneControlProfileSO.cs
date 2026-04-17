using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a player's zone-control career profile
    /// across multiple matches: wins, losses, and total zones captured.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="RecordMatchResult"/> at each match end to accumulate wins,
    ///   losses, and zone captures.  <see cref="WinRate"/> and
    ///   <see cref="AverageZonesPerMatch"/> are computed properties with safe division.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlProfile.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlProfile", order = 65)]
    public sealed class ZoneControlProfileSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each RecordMatchResult call.")]
        [SerializeField] private VoidGameEvent _onProfileUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _totalWins;
        private int _totalLosses;
        private int _totalZonesCaptured;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Career win count.</summary>
        public int TotalWins => _totalWins;

        /// <summary>Career loss count.</summary>
        public int TotalLosses => _totalLosses;

        /// <summary>Total matches played (wins + losses).</summary>
        public int MatchesPlayed => _totalWins + _totalLosses;

        /// <summary>Cumulative zone captures across all recorded matches.</summary>
        public int TotalZonesCaptured => _totalZonesCaptured;

        /// <summary>Win rate in [0, 1]; returns 0 when no matches have been played.</summary>
        public float WinRate => MatchesPlayed > 0 ? (float)_totalWins / MatchesPlayed : 0f;

        /// <summary>Average zones captured per match; 0 when no matches played.</summary>
        public float AverageZonesPerMatch => MatchesPlayed > 0 ? (float)_totalZonesCaptured / MatchesPlayed : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records the outcome of a finished match.
        /// <paramref name="zones"/> is clamped to [0, ∞).
        /// Fires <c>_onProfileUpdated</c>.
        /// Zero allocation.
        /// </summary>
        public void RecordMatchResult(bool playerWon, int zones)
        {
            if (playerWon)
                _totalWins++;
            else
                _totalLosses++;

            _totalZonesCaptured += Mathf.Max(0, zones);

            _onProfileUpdated?.Raise();
        }

        /// <summary>
        /// Clears all profile state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _totalWins          = 0;
            _totalLosses        = 0;
            _totalZonesCaptured = 0;
        }
    }
}
