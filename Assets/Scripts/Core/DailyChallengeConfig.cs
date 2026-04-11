using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configures the pool of <see cref="BonusConditionSO"/> assets rotated as daily
    /// challenges and the credit-reward multiplier applied when a player completes the
    /// daily challenge.
    ///
    /// ── How daily challenges work ──────────────────────────────────────────────
    ///   Each UTC day a single <see cref="BonusConditionSO"/> is selected from
    ///   <see cref="ChallengePool"/> using a deterministic date-based index so all
    ///   players see the same challenge.  The condition is evaluated by
    ///   <see cref="DailyChallengeManager"/> after each match; on success the player
    ///   receives <c>condition.BonusAmount × RewardMultiplier</c> bonus credits (on
    ///   top of the normal match reward) and the challenge is marked done for the day.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • SO asset is immutable at runtime (IReadOnlyList view).
    ///   • Reuses existing <see cref="BonusConditionSO"/> assets — no new condition
    ///     types needed.
    ///   • No Unity Physics or UI namespace references.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ DailyChallengeConfig.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DailyChallengeConfig",
        menuName  = "BattleRobots/Economy/DailyChallengeConfig",
        order     = 2)]
    public sealed class DailyChallengeConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Challenge Pool")]
        [Tooltip("Pool of BonusConditionSO assets rotated daily.\n" +
                 "Each day one entry is selected by a deterministic date-based index.\n" +
                 "Null entries are ignored (skipped during selection).")]
        [SerializeField] private List<BonusConditionSO> _challengePool = new List<BonusConditionSO>();

        [Header("Reward")]
        [Tooltip("Multiplier applied to the selected condition's BonusAmount when the daily " +
                 "challenge is completed.  1.0 = same as a normal match bonus; " +
                 "2.0 = double the normal bonus reward.  Minimum 1.0.")]
        [SerializeField, Min(1f)] private float _rewardMultiplier = 2f;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Read-only ordered list of conditions used as daily challenge candidates.
        /// Null entries may be present; <see cref="DailyChallengeSO.RefreshIfNeeded"/>
        /// skips them during selection.
        /// </summary>
        public IReadOnlyList<BonusConditionSO> ChallengePool => _challengePool;

        /// <summary>
        /// Multiplier applied to the daily-challenge condition's
        /// <see cref="BonusConditionSO.BonusAmount"/> when the player completes it.
        /// Always ≥ 1.0.
        /// </summary>
        public float RewardMultiplier => _rewardMultiplier;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_rewardMultiplier < 1f) _rewardMultiplier = 1f;

            for (int i = 0; i < _challengePool.Count; i++)
            {
                if (_challengePool[i] == null)
                    Debug.LogWarning($"[DailyChallengeConfig] '{name}': " +
                                     $"Null entry at index {i}. Assign or remove.", this);
            }
        }
#endif
    }
}
