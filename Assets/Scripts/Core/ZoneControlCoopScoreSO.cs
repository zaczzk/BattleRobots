using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a cooperative two-player zone-capture score.
    /// Accumulates independent capture counts for the player and an ally, and fires a
    /// milestone event when their combined total reaches <see cref="SharedMilestone"/>.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   Call <see cref="AddPlayerCapture"/> / <see cref="AddAllyCapture"/> each time
    ///   a capture is recorded for the respective participant.
    ///   Subscribe to <see cref="_onMilestoneReached"/> to award the shared bonus reward.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCoopScore.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCoopScore", order = 33)]
    public sealed class ZoneControlCoopScoreSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Coop Settings")]
        [Tooltip("Combined captures required to reach the shared milestone.")]
        [Min(1)]
        [SerializeField] private int _sharedMilestone = 20;

        [Tooltip("Bonus reward granted when the shared milestone is reached.")]
        [Min(0)]
        [SerializeField] private int _bonusReward = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCoopScoreUpdated;
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int  _playerCaptures;
        private int  _allyCaptures;
        private bool _milestoneReached;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of captures recorded for the player.</summary>
        public int  PlayerCaptures   => _playerCaptures;

        /// <summary>Number of captures recorded for the ally.</summary>
        public int  AllyCaptures     => _allyCaptures;

        /// <summary>Combined capture count (player + ally).</summary>
        public int  TotalCaptures    => _playerCaptures + _allyCaptures;

        /// <summary>True once the combined total has reached <see cref="SharedMilestone"/>.</summary>
        public bool MilestoneReached => _milestoneReached;

        /// <summary>Combined captures required for the milestone.</summary>
        public int  SharedMilestone  => _sharedMilestone;

        /// <summary>Reward granted on milestone completion.</summary>
        public int  BonusReward      => _bonusReward;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a zone capture for the player.
        /// Fires <see cref="_onCoopScoreUpdated"/> and checks for milestone completion.
        /// </summary>
        public void AddPlayerCapture()
        {
            _playerCaptures++;
            _onCoopScoreUpdated?.Raise();
            CheckMilestone();
        }

        /// <summary>
        /// Records a zone capture for the ally.
        /// Fires <see cref="_onCoopScoreUpdated"/> and checks for milestone completion.
        /// </summary>
        public void AddAllyCapture()
        {
            _allyCaptures++;
            _onCoopScoreUpdated?.Raise();
            CheckMilestone();
        }

        /// <summary>
        /// Resets all capture counts and the milestone flag silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _playerCaptures  = 0;
            _allyCaptures    = 0;
            _milestoneReached = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void CheckMilestone()
        {
            if (_milestoneReached) return;
            if (TotalCaptures >= _sharedMilestone)
            {
                _milestoneReached = true;
                _onMilestoneReached?.Raise();
            }
        }
    }
}
