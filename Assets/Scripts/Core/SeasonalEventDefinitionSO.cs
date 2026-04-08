using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single reward tier within a seasonal event.
    /// Array entries in <see cref="SeasonalEventDefinitionSO"/> should be ordered
    /// by ascending <see cref="requiredScore"/>.
    /// </summary>
    [Serializable]
    public struct SeasonalEventRewardTier
    {
        /// <summary>Player-facing label, e.g. "Bronze", "Silver", "Gold".</summary>
        public string tierName;

        /// <summary>Cumulative seasonal score needed to unlock this tier.</summary>
        [Min(1)] public int requiredScore;

        /// <summary>Currency added to <see cref="PlayerWallet"/> when the tier is first claimed.</summary>
        [Min(1)] public int rewardCurrency;
    }

    /// <summary>
    /// Immutable data asset describing a single seasonal event: its active window,
    /// scoring rules, and reward tier thresholds.
    ///
    /// ── How scoring works ────────────────────────────────────────────────────
    ///   After every match <see cref="SeasonalEventProgressSO.RecordMatch"/> is called.
    ///   While <see cref="IsActive"/> returns true the player earns:
    ///   <list type="bullet">
    ///     <item><see cref="PointsPerWin"/> — for a winning match.</item>
    ///     <item><see cref="PointsPerMatch"/> — for any completed match (loss / draw).</item>
    ///   </list>
    ///   Score accumulates across the whole season; tiers are permanent once reached.
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Core</c> only — no Physics or UI references.
    ///   • Immutable at runtime — all serialised fields accessed via read-only properties.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ SeasonalEvent ▶ Definition
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/SeasonalEvent/Definition",
        order    = 1)]
    public sealed class SeasonalEventDefinitionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Unique machine identifier, e.g. 'season_spring_2026'. " +
                 "Used as the persistence key — never change after shipping.")]
        [SerializeField] private string _eventId;

        [Tooltip("Player-facing event name, e.g. 'Spring Smackdown 2026'.")]
        [SerializeField] private string _eventName;

        [Tooltip("UTC start of the season stored as DateTime.ToBinary(). " +
                 "Set via a custom Editor tool or directly as a long literal.")]
        [SerializeField] private long _startUtcBinary;

        [Tooltip("UTC end of the season stored as DateTime.ToBinary(). " +
                 "The event is active when UtcNow is in [start, end].")]
        [SerializeField] private long _endUtcBinary;

        [Tooltip("Points awarded for winning a match during the event.")]
        [SerializeField, Min(0)] private int _pointsPerWin = 100;

        [Tooltip("Points awarded for completing any match (loss/draw) during the event. " +
                 "A win earns PointsPerWin only (not both).")]
        [SerializeField, Min(0)] private int _pointsPerMatch = 10;

        [Tooltip("Reward tier thresholds, ordered from lowest to highest required score. " +
                 "OnValidate warns if the order is wrong.")]
        [SerializeField] private SeasonalEventRewardTier[] _rewardTiers = new SeasonalEventRewardTier[0];

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Unique machine identifier for this seasonal event.</summary>
        public string EventId => _eventId;

        /// <summary>Player-facing event name.</summary>
        public string EventName => _eventName;

        /// <summary>Points awarded for a winning match during the event (exclusive with PointsPerMatch).</summary>
        public int PointsPerWin => _pointsPerWin;

        /// <summary>Points awarded for any completed non-winning match during the event.</summary>
        public int PointsPerMatch => _pointsPerMatch;

        /// <summary>Number of reward tiers defined for this event.</summary>
        public int TierCount => _rewardTiers != null ? _rewardTiers.Length : 0;

        /// <summary>
        /// Returns true if the current UTC wall-clock time falls within the event window.
        /// Returns false when both binary fields are 0 (unset in the Inspector).
        /// </summary>
        public bool IsActive()
        {
            if (_startUtcBinary == 0 && _endUtcBinary == 0) return false;
            long nowBinary = DateTime.UtcNow.ToBinary();
            return nowBinary >= _startUtcBinary && nowBinary <= _endUtcBinary;
        }

        /// <summary>
        /// Duration remaining until the event ends.
        /// Returns <see cref="TimeSpan.Zero"/> when the event has ended or was never configured.
        /// </summary>
        public TimeSpan TimeRemaining()
        {
            if (_endUtcBinary == 0) return TimeSpan.Zero;
            TimeSpan remaining = DateTime.FromBinary(_endUtcBinary) - DateTime.UtcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        /// <summary>
        /// Returns the tier definition at <paramref name="index"/> by value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If index is out of range.</exception>
        public SeasonalEventRewardTier GetTier(int index)
        {
            if (_rewardTiers == null || index < 0 || index >= _rewardTiers.Length)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Tier index {index} is out of range [0, {TierCount - 1}].");
            return _rewardTiers[index];
        }

        /// <summary>
        /// Returns the highest tier index whose <c>requiredScore</c> is ≤ <paramref name="score"/>,
        /// or <c>-1</c> if no tier threshold has been met.
        /// </summary>
        public int GetHighestReachedTierIndex(int score)
        {
            if (_rewardTiers == null) return -1;
            int highest = -1;
            for (int i = 0; i < _rewardTiers.Length; i++)
            {
                if (score >= _rewardTiers[i].requiredScore)
                    highest = i;
            }
            return highest;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_rewardTiers == null || _rewardTiers.Length < 2) return;
            for (int i = 1; i < _rewardTiers.Length; i++)
            {
                if (_rewardTiers[i].requiredScore <= _rewardTiers[i - 1].requiredScore)
                    Debug.LogWarning(
                        $"[SeasonalEventDefinitionSO] '{name}': tier[{i}].requiredScore " +
                        $"({_rewardTiers[i].requiredScore}) should be > tier[{i - 1}].requiredScore " +
                        $"({_rewardTiers[i - 1].requiredScore}).", this);
            }
        }
#endif
    }
}
