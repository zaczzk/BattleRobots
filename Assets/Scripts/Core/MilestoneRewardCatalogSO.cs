using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data SO that lists a flat currency reward for each
    /// <see cref="MasteryProgressMilestoneSO"/> milestone index.
    ///
    /// ── Reward mechanic ──────────────────────────────────────────────────────────
    ///   The catalog holds an ordered array of rewards indexed by milestone number
    ///   (0-based).  When the player clears milestone N for the first time across any
    ///   damage type, the corresponding reward at index N is granted as flat currency
    ///   via <see cref="BattleRobots.Physics.MilestoneRewardApplier"/>.
    ///
    ///   Example: _rewardPerMilestone = [100, 250, 500]
    ///   • Milestone 0 cleared → 100 currency.
    ///   • Milestone 1 cleared → 250 currency.
    ///   • Milestone 2 cleared → 500 currency.
    ///   • Index beyond array length → 0 (no reward).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO asset is immutable at runtime — all methods are read-only.
    ///   - GetReward is zero-alloc (array index lookup).
    ///   - Out-of-range or negative indices silently return 0.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ MilestoneRewardCatalog.
    /// Assign to <see cref="BattleRobots.Physics.MilestoneRewardApplier"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/MilestoneRewardCatalog",
        fileName = "MilestoneRewardCatalogSO")]
    public sealed class MilestoneRewardCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Reward Settings")]
        [Tooltip("Flat currency reward granted when a milestone at that index is cleared " +
                 "for the first time (index 0 = first milestone, etc.). " +
                 "Out-of-range indices return 0.")]
        [SerializeField] private float[] _rewardPerMilestone = new float[0];

        [Tooltip("Human-readable label used by notification systems " +
                 "when a milestone reward is granted (e.g. 'Milestone Reward').")]
        [SerializeField] private string _rewardLabel = "Milestone Reward";

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Number of reward entries configured in the catalog.
        /// </summary>
        public int Count => _rewardPerMilestone == null ? 0 : _rewardPerMilestone.Length;

        /// <summary>
        /// Human-readable label for notifications.  Never null or empty; falls back to
        /// "Milestone Reward" when the inspector field is blank.
        /// </summary>
        public string RewardLabel =>
            string.IsNullOrEmpty(_rewardLabel) ? "Milestone Reward" : _rewardLabel;

        /// <summary>
        /// Returns the flat currency reward for the milestone at <paramref name="index"/>.
        /// Returns <c>0</c> when <paramref name="index"/> is negative, out of range, or
        /// the reward array is null / empty.  Zero alloc.
        /// </summary>
        /// <param name="index">Zero-based milestone index.</param>
        public float GetReward(int index)
        {
            if (_rewardPerMilestone == null || index < 0 || index >= _rewardPerMilestone.Length)
                return 0f;

            return _rewardPerMilestone[index];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_rewardPerMilestone == null || _rewardPerMilestone.Length == 0)
                Debug.LogWarning(
                    $"[MilestoneRewardCatalogSO] No rewards configured in '{name}'.", this);
        }
#endif
    }
}
