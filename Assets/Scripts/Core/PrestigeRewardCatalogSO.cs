using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single reward entry in <see cref="PrestigeRewardCatalogSO"/>.
    /// Specifies the prestige rank at which the reward unlocks, a display label,
    /// and a score bonus multiplier applied from that rank onward.
    /// </summary>
    [System.Serializable]
    public struct PrestigeRewardEntry
    {
        [Tooltip("Prestige rank at which this reward is granted (1 = first prestige, max 10).")]
        [Min(1)] public int rank;

        [Tooltip("Human-readable label shown in the 'Next Prestige Reward' panel " +
                 "(e.g. 'Bronze Frame Skin').")]
        public string label;

        [Tooltip("Score bonus multiplier applied from this prestige rank onward. " +
                 "1.0 = no bonus; 1.25 = +25 % score.")]
        [Min(1f)] public float bonusMultiplier;
    }

    /// <summary>
    /// Ordered catalog of per-prestige-rank rewards (cosmetic labels and score multipliers).
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Stores a designer-authored array of <see cref="PrestigeRewardEntry"/> items
    ///     keyed by prestige rank.
    ///   • <see cref="TryGetRewardForRank"/> looks up an entry by exact rank.
    ///   • <see cref="TryGetNextReward"/> finds the nearest upcoming reward for a given
    ///     current prestige count, used by <see cref="BattleRobots.UI.PrestigeRewardController"/>
    ///     to populate the "Next Prestige Reward" panel.
    ///   • <see cref="HasRewardAtRank"/> allows callers to probe existence without out-params.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO asset is immutable at runtime — entries are read-only data.
    ///   - Zero alloc read paths: linear scans over value-type array.
    ///   - Multiple entries at the same rank are allowed; TryGetRewardForRank returns
    ///     the first match (lowest index).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ PrestigeRewardCatalog.
    /// Assign to <see cref="BattleRobots.UI.PrestigeRewardController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/PrestigeRewardCatalog",
        fileName = "PrestigeRewardCatalogSO")]
    public sealed class PrestigeRewardCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of prestige rewards. Each entry specifies the unlock rank, " +
                 "a display label, and a score bonus multiplier.")]
        [SerializeField] private PrestigeRewardEntry[] _rewards = new PrestigeRewardEntry[0];

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of reward entries in the catalog.</summary>
        public int Count => _rewards == null ? 0 : _rewards.Length;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the catalog contains at least one entry whose
        /// <see cref="PrestigeRewardEntry.rank"/> equals <paramref name="rank"/>.
        /// </summary>
        public bool HasRewardAtRank(int rank)
        {
            if (_rewards == null) return false;
            for (int i = 0; i < _rewards.Length; i++)
                if (_rewards[i].rank == rank) return true;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the first reward entry whose rank matches
        /// <paramref name="rank"/> exactly.
        /// </summary>
        /// <param name="rank">The prestige rank to look up.</param>
        /// <param name="reward">
        ///   Set to the matching entry when found; <c>default</c> otherwise.
        /// </param>
        /// <returns>True when a matching entry was found.</returns>
        public bool TryGetRewardForRank(int rank, out PrestigeRewardEntry reward)
        {
            if (_rewards != null)
            {
                for (int i = 0; i < _rewards.Length; i++)
                {
                    if (_rewards[i].rank == rank)
                    {
                        reward = _rewards[i];
                        return true;
                    }
                }
            }
            reward = default;
            return false;
        }

        /// <summary>
        /// Finds the nearest upcoming reward for a player currently at
        /// <paramref name="currentPrestige"/>.
        ///
        /// <para>Scans all entries and returns the one with the smallest rank that is
        /// strictly greater than <paramref name="currentPrestige"/>.
        /// When multiple entries share that rank, the first (lowest index) is returned.</para>
        ///
        /// <para>Returns false when the catalog is empty or all rewards have already been
        /// reached (i.e. no entry has rank &gt; currentPrestige).</para>
        /// </summary>
        /// <param name="currentPrestige">Current player prestige count.</param>
        /// <param name="reward">
        ///   Set to the next upcoming reward entry when found; <c>default</c> otherwise.
        /// </param>
        /// <returns>True when an upcoming reward was found.</returns>
        public bool TryGetNextReward(int currentPrestige, out PrestigeRewardEntry reward)
        {
            if (_rewards != null)
            {
                int bestRank  = int.MaxValue;
                int bestIndex = -1;

                for (int i = 0; i < _rewards.Length; i++)
                {
                    if (_rewards[i].rank > currentPrestige && _rewards[i].rank < bestRank)
                    {
                        bestRank  = _rewards[i].rank;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0)
                {
                    reward = _rewards[bestIndex];
                    return true;
                }
            }

            reward = default;
            return false;
        }
    }
}
