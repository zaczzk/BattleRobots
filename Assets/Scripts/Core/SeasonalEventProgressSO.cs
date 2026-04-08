using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a player's cumulative score and claimed
    /// reward tiers for the current seasonal event.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadFromData"/> on startup.
    ///   2. After every match <see cref="GameBootstrapper.RecordMatchAndSave"/> calls
    ///      <see cref="RecordMatch"/>, then saves via <see cref="BuildData"/>.
    ///   3. <see cref="BattleRobots.UI.SeasonalEventUI"/> calls <see cref="TryClaimTier"/>
    ///      when the player taps a tier's Claim button.
    ///
    /// ── Season change detection ───────────────────────────────────────────────
    ///   <see cref="LoadFromData"/> compares the stored season ID against the currently
    ///   assigned <see cref="Definition"/>'s <c>EventId</c>. A mismatch triggers a full
    ///   reset so stale data from a previous season never carries over.
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Core</c> only — no Physics or UI references.
    ///   • SO assets are immutable at runtime except for designated mutators.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ SeasonalEvent ▶ Progress
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/SeasonalEvent/Progress",
        order    = 2)]
    public sealed class SeasonalEventProgressSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Definition")]
        [Tooltip("The SeasonalEventDefinitionSO for the current season. Swap this asset " +
                 "each season; the season-ID mismatch auto-resets all progress.")]
        [SerializeField] private SeasonalEventDefinitionSO _definition;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after every RecordMatch call that awards points.")]
        [SerializeField] private VoidGameEvent _onScoreChanged;

        [Tooltip("Raised once when a player successfully claims a tier reward.")]
        [SerializeField] private VoidGameEvent _onTierUnlocked;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _score;
        private readonly HashSet<int> _claimedTierIndices = new HashSet<int>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned seasonal-event definition, or <c>null</c> if unset.</summary>
        public SeasonalEventDefinitionSO Definition => _definition;

        /// <summary>Player's cumulative score for the current season.</summary>
        public int Score => _score;

        /// <summary>True if the event is defined and currently within its active window.</summary>
        public bool IsActive => _definition != null && _definition.IsActive();

        /// <summary>
        /// True if the player's score has reached or exceeded <paramref name="tierIndex"/>'s threshold.
        /// Returns false for out-of-range indices.
        /// </summary>
        public bool IsTierReached(int tierIndex)
        {
            if (_definition == null || tierIndex < 0 || tierIndex >= _definition.TierCount)
                return false;
            return _score >= _definition.GetTier(tierIndex).requiredScore;
        }

        /// <summary>True if the player has already claimed the reward for <paramref name="tierIndex"/>.</summary>
        public bool IsTierClaimed(int tierIndex) => _claimedTierIndices.Contains(tierIndex);

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Processes a completed match and awards seasonal points if the event is active.
        /// <list type="bullet">
        ///   <item>Win → <see cref="SeasonalEventDefinitionSO.PointsPerWin"/> points.</item>
        ///   <item>Loss / draw → <see cref="SeasonalEventDefinitionSO.PointsPerMatch"/> points.</item>
        /// </list>
        /// Fires <c>_onScoreChanged</c> (VoidGameEvent) when points are awarded.
        /// No-ops when: event is inactive, definition is null, or record is null.
        /// </summary>
        public void RecordMatch(MatchRecord record)
        {
            if (_definition == null || !_definition.IsActive() || record == null) return;

            int points = record.playerWon
                ? _definition.PointsPerWin
                : _definition.PointsPerMatch;

            if (points <= 0) return;

            _score += points;
            _onScoreChanged?.Raise();
        }

        /// <summary>
        /// Attempts to claim the reward for the given tier index.
        /// Credits <see cref="SeasonalEventRewardTier.rewardCurrency"/> to
        /// <paramref name="wallet"/> and marks the tier as claimed.
        /// Raises <c>_onTierUnlocked</c> (VoidGameEvent) on success.
        ///
        /// Returns <c>false</c> when: tier not reached, already claimed, definition null,
        /// or index out of range.
        /// </summary>
        public bool TryClaimTier(int tierIndex, PlayerWallet wallet)
        {
            if (_definition == null) return false;
            if (tierIndex < 0 || tierIndex >= _definition.TierCount) return false;
            if (!IsTierReached(tierIndex)) return false;
            if (IsTierClaimed(tierIndex)) return false;

            wallet?.AddFunds(_definition.GetTier(tierIndex).rewardCurrency);
            _claimedTierIndices.Add(tierIndex);
            _onTierUnlocked?.Raise();
            return true;
        }

        // ── Persistence ───────────────────────────────────────────────────────

        /// <summary>
        /// Restores seasonal progress from <paramref name="data"/>.
        /// Resets all state when: data is null, data's season ID is empty, or the stored
        /// season ID does not match <c>_definition.EventId</c> (new season detected).
        /// </summary>
        public void LoadFromData(SeasonalEventData data)
        {
            string currentId = _definition != null ? _definition.EventId : string.Empty;

            bool isValidData = data != null
                && !string.IsNullOrEmpty(data.seasonId)
                && data.seasonId == currentId;

            _claimedTierIndices.Clear();

            if (isValidData)
            {
                _score = data.cumulativeScore;
                if (data.claimedTierIndices != null)
                {
                    for (int i = 0; i < data.claimedTierIndices.Count; i++)
                        _claimedTierIndices.Add(data.claimedTierIndices[i]);
                }
            }
            else
            {
                _score = 0;
            }
        }

        /// <summary>
        /// Snapshots the current seasonal progress into a <see cref="SeasonalEventData"/> POCO
        /// for XOR-SaveSystem persistence. Called by <see cref="GameBootstrapper.RecordMatchAndSave"/>.
        /// </summary>
        public SeasonalEventData BuildData()
        {
            var data = new SeasonalEventData
            {
                seasonId        = _definition != null ? _definition.EventId ?? string.Empty : string.Empty,
                cumulativeScore = _score,
            };

            foreach (int idx in _claimedTierIndices)
                data.claimedTierIndices.Add(idx);

            return data;
        }
    }
}
