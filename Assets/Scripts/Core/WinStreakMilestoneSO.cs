using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Serializable struct that pairs a win-streak target with a credit reward granted
    /// every time the player's consecutive win streak reaches that value.
    ///
    /// Unlike level rewards (which fire once per lifetime), streak milestones are
    /// repeatable — players earn the reward each time they rebuild a streak after a loss.
    ///
    /// Multiple entries may share the same <see cref="streakTarget"/> — all are applied
    /// when the streak reaches that count.
    /// </summary>
    [Serializable]
    public struct WinStreakMilestoneEntry
    {
        [Tooltip("The consecutive-win count that triggers this reward (minimum 1).")]
        [Min(1)]
        public int streakTarget;

        [Tooltip("Credits granted when the player reaches this streak count. 0 = no credit reward.")]
        [Min(0)]
        public int rewardCredits;

        [Tooltip("Short label shown in toast notifications, e.g. \"3-Win Streak!\". " +
                 "Leave empty to suppress the notification for this entry.")]
        public string displayName;
    }

    /// <summary>
    /// Immutable SO catalogue that maps win-streak targets to repeatable credit rewards.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ WinStreakMilestone.
    ///   2. Add one <see cref="WinStreakMilestoneEntry"/> per milestone streak count.
    ///   3. Assign to <see cref="WinStreakMilestoneManager._milestoneConfig"/> in the scene.
    ///
    /// ── Repeatability ─────────────────────────────────────────────────────────
    ///   Because a player's streak resets on loss and can reach the same milestone
    ///   multiple times across a session, <see cref="WinStreakMilestoneManager"/>
    ///   fires the reward every time <see cref="WinStreakSO.CurrentStreak"/> equals
    ///   a configured <see cref="WinStreakMilestoneEntry.streakTarget"/>.
    ///   This makes milestones an ongoing engagement incentive.
    ///
    /// ── Runtime immutability ──────────────────────────────────────────────────
    ///   The internal list is exposed only as <see cref="IReadOnlyList{T}"/>; no
    ///   mutator methods exist.  Designers edit entries in the Inspector only.
    ///
    /// ── Lookup ────────────────────────────────────────────────────────────────
    ///   <see cref="GetRewardsForStreak"/> iterates the list linearly — it is called
    ///   at most once per match-end event (cold path), so no additional index is needed.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ WinStreakMilestone.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/WinStreakMilestone",
        fileName = "WinStreakMilestoneSO")]
    public sealed class WinStreakMilestoneSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of milestone reward entries. " +
                 "Multiple entries at the same streakTarget are all granted when that streak is reached.")]
        [SerializeField] private List<WinStreakMilestoneEntry> _milestones
            = new List<WinStreakMilestoneEntry>();

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>Read-only view of all configured milestone entries.</summary>
        public IReadOnlyList<WinStreakMilestoneEntry> Milestones => _milestones;

        /// <summary>
        /// Returns all reward entries whose <see cref="WinStreakMilestoneEntry.streakTarget"/>
        /// matches <paramref name="streak"/>.  Returns an empty list when no entries match.
        /// </summary>
        /// <param name="streak">
        /// The player's current consecutive win count
        /// (from <see cref="WinStreakSO.CurrentStreak"/>).
        /// </param>
        public IReadOnlyList<WinStreakMilestoneEntry> GetRewardsForStreak(int streak)
        {
            // Lazily populated — allocated only when at least one entry matches.
            List<WinStreakMilestoneEntry> results = null;

            for (int i = 0; i < _milestones.Count; i++)
            {
                if (_milestones[i].streakTarget == streak)
                {
                    results ??= new List<WinStreakMilestoneEntry>();
                    results.Add(_milestones[i]);
                }
            }

            return results ?? (IReadOnlyList<WinStreakMilestoneEntry>)Array.Empty<WinStreakMilestoneEntry>();
        }

        /// <summary>
        /// Returns <c>true</c> when at least one reward entry is configured for
        /// <paramref name="streak"/>.
        /// </summary>
        public bool HasMilestoneAtStreak(int streak)
        {
            for (int i = 0; i < _milestones.Count; i++)
            {
                if (_milestones[i].streakTarget == streak) return true;
            }
            return false;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_milestones == null || _milestones.Count == 0) return;

            for (int i = 0; i < _milestones.Count; i++)
            {
                if (_milestones[i].rewardCredits < 0)
                    Debug.LogWarning(
                        $"[WinStreakMilestoneSO] '{name}': Entry [{i}] has negative rewardCredits " +
                        $"({_milestones[i].rewardCredits}). It will be treated as 0.", this);

                if (_milestones[i].streakTarget < 1)
                    Debug.LogWarning(
                        $"[WinStreakMilestoneSO] '{name}': Entry [{i}] has streakTarget " +
                        $"{_milestones[i].streakTarget}. Rewards should target a streak ≥ 1.", this);
            }
        }
#endif
    }
}
