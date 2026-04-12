using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Serializable struct that pairs a player level with a one-time credit reward
    /// granted the first time that level is reached.
    ///
    /// Multiple entries may share the same level value — all matching entries are
    /// applied when the player reaches that level.
    /// </summary>
    [Serializable]
    public struct LevelRewardEntry
    {
        [Tooltip("The player level that triggers this reward (minimum 2 — level 1 is the start).")]
        [Min(2)]
        public int level;

        [Tooltip("Credits granted when the player reaches this level. 0 = no credit reward.")]
        [Min(0)]
        public int rewardCredits;

        [Tooltip("Short label shown in toast notifications, e.g. \"Reached Level 5!\". " +
                 "Leave empty to suppress the notification for this entry.")]
        public string displayName;
    }

    /// <summary>
    /// Immutable SO catalogue that maps player level milestones to one-time credit rewards.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ LevelRewardConfig.
    ///   2. Add one <see cref="LevelRewardEntry"/> per milestone level.
    ///   3. Assign to <see cref="LevelRewardManager._rewardConfig"/> in the scene.
    ///
    /// ── Runtime immutability ──────────────────────────────────────────────────
    ///   The internal list is exposed only as <see cref="IReadOnlyList{T}"/>; no
    ///   mutator methods exist.  Designers edit entries in the Inspector only.
    ///
    /// ── Lookup ────────────────────────────────────────────────────────────────
    ///   <see cref="GetRewardsForLevel"/> iterates the list linearly — it is called
    ///   at most once per level-up event (cold path), so no additional index is needed.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/LevelRewardConfig",
        fileName = "LevelRewardConfig")]
    public sealed class LevelRewardConfigSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of milestone reward entries. " +
                 "Multiple entries at the same level are all granted on that level-up.")]
        [SerializeField] private List<LevelRewardEntry> _rewards = new List<LevelRewardEntry>();

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>Read-only view of all configured level reward entries.</summary>
        public IReadOnlyList<LevelRewardEntry> Rewards => _rewards;

        /// <summary>
        /// Returns all reward entries whose <see cref="LevelRewardEntry.level"/> matches
        /// <paramref name="level"/>.  Returns an empty list when no entries match.
        /// </summary>
        /// <param name="level">Player level reached (1-based).</param>
        public IReadOnlyList<LevelRewardEntry> GetRewardsForLevel(int level)
        {
            // Lazily populated result list — allocated only when at least one entry matches.
            List<LevelRewardEntry> results = null;

            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i].level == level)
                {
                    results ??= new List<LevelRewardEntry>();
                    results.Add(_rewards[i]);
                }
            }

            return results ?? (IReadOnlyList<LevelRewardEntry>)Array.Empty<LevelRewardEntry>();
        }

        /// <summary>
        /// Returns true when at least one reward entry is configured for
        /// <paramref name="level"/>.
        /// </summary>
        public bool HasRewardAtLevel(int level)
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i].level == level) return true;
            }
            return false;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_rewards == null || _rewards.Count == 0) return;

            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i].rewardCredits < 0)
                    Debug.LogWarning(
                        $"[LevelRewardConfig] '{name}': Entry [{i}] has negative rewardCredits " +
                        $"({_rewards[i].rewardCredits}). It will be treated as 0.", this);

                if (_rewards[i].level < 2)
                    Debug.LogWarning(
                        $"[LevelRewardConfig] '{name}': Entry [{i}] has level {_rewards[i].level}. " +
                        $"Rewards should target level ≥ 2 (level 1 is the starting level).", this);
            }
        }
#endif
    }
}
