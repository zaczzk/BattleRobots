using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject managing a multi-round zone-control tournament bracket.
    /// Each round has an individual zone-capture target; the player wins a round by
    /// meeting or exceeding that target.  A trophy reward is granted on tournament
    /// completion.
    ///
    /// ── Round flow ──────────────────────────────────────────────────────────────
    ///   Call <see cref="SubmitRoundResult"/> at the end of each match.
    ///   The SO advances <see cref="CurrentRound"/> and fires events accordingly.
    ///   When all rounds are played <see cref="IsComplete"/> becomes true and
    ///   <see cref="_onTournamentComplete"/> fires.
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
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlTournament.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlTournament", order = 34)]
    public sealed class ZoneControlTournamentSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Tournament Settings")]
        [Tooltip("Per-round zone-capture targets.  The number of entries defines the round count.")]
        [SerializeField] private int[] _roundZoneTargets = { 5, 10, 15 };

        [Tooltip("Currency reward granted when the entire tournament is completed.")]
        [Min(0)]
        [SerializeField] private int _trophyReward = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRoundComplete;
        [SerializeField] private VoidGameEvent _onTournamentComplete;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int  _currentRound;
        private int  _roundWins;
        private int  _roundLosses;
        private bool _isComplete;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Zero-based index of the round currently being played.</summary>
        public int  CurrentRound => _currentRound;

        /// <summary>Total number of rounds in the tournament.</summary>
        public int  RoundCount   => _roundZoneTargets != null ? _roundZoneTargets.Length : 0;

        /// <summary>Number of rounds won so far.</summary>
        public int  RoundWins    => _roundWins;

        /// <summary>Number of rounds lost so far.</summary>
        public int  RoundLosses  => _roundLosses;

        /// <summary>True when all rounds have been played.</summary>
        public bool IsComplete   => _isComplete;

        /// <summary>Currency reward for completing the tournament.</summary>
        public int  TrophyReward => _trophyReward;

        /// <summary>
        /// Zone-capture target for the current round, or -1 when the tournament is complete.
        /// </summary>
        public int  CurrentRoundTarget
        {
            get
            {
                if (_isComplete || _roundZoneTargets == null || _currentRound >= _roundZoneTargets.Length)
                    return -1;
                return _roundZoneTargets[_currentRound];
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records the result of the current round and advances to the next.
        /// No-op when the tournament is already complete.
        /// Fires <see cref="_onRoundComplete"/> per round and
        /// <see cref="_onTournamentComplete"/> once all rounds are done.
        /// </summary>
        /// <param name="zonesCaptured">Zones captured by the player this match.</param>
        /// <param name="targetZones">Target that must be met to win the round.</param>
        public void SubmitRoundResult(int zonesCaptured, int targetZones)
        {
            if (_isComplete) return;

            bool won = zonesCaptured >= targetZones;
            if (won) _roundWins++;
            else     _roundLosses++;

            _onRoundComplete?.Raise();

            _currentRound = Mathf.Min(_currentRound + 1, RoundCount);

            if (_currentRound >= RoundCount)
            {
                _isComplete = true;
                _onTournamentComplete?.Raise();
            }
        }

        /// <summary>
        /// Restores persisted tournament state.  Bootstrapper-safe; no events fired.
        /// </summary>
        public void LoadSnapshot(int currentRound, int roundWins, int roundLosses, bool isComplete)
        {
            _currentRound = Mathf.Clamp(currentRound, 0, RoundCount);
            _roundWins    = Mathf.Max(0, roundWins);
            _roundLosses  = Mathf.Max(0, roundLosses);
            _isComplete   = isComplete;
        }

        /// <summary>Returns all runtime fields as a value tuple for persistence.</summary>
        public (int currentRound, int roundWins, int roundLosses, bool isComplete) TakeSnapshot() =>
            (_currentRound, _roundWins, _roundLosses, _isComplete);

        /// <summary>
        /// Resets all runtime state silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _currentRound = 0;
            _roundWins    = 0;
            _roundLosses  = 0;
            _isComplete   = false;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_roundZoneTargets != null)
            {
                for (int i = 0; i < _roundZoneTargets.Length; i++)
                    _roundZoneTargets[i] = Mathf.Max(1, _roundZoneTargets[i]);
            }
        }
    }
}
