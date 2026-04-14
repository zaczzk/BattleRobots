using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Maps a prestige rank label to the badge <see cref="Sprite"/> that represents it.
    /// </summary>
    [Serializable]
    public sealed class RankBadgeEntry
    {
        [Tooltip("Rank label exactly as returned by PrestigeSystemSO.GetRankLabel() " +
                 "(e.g. 'None', 'Bronze I', 'Silver II', 'Legend').")]
        public string rankLabel;

        [Tooltip("Sprite to display for this rank.")]
        public Sprite badgeSprite;
    }

    /// <summary>
    /// Data-driven mapping of prestige rank labels to badge sprites.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   • Populate <c>_badgeEntries</c> with one <see cref="RankBadgeEntry"/> per rank.
    ///   • <see cref="GetBadge"/> performs a linear scan and returns the first
    ///     matching sprite, or <c>null</c> when no entry matches.
    ///   • Assign to <see cref="BattleRobots.UI.RankBadgeController._badgeConfig"/>.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO treated as immutable at runtime (read-only list).
    ///   - Zero heap allocations in <see cref="GetBadge"/> after initialisation.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ RankBadgeConfig.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/RankBadgeConfig",
        fileName = "RankBadgeConfig")]
    public sealed class RankBadgeConfig : ScriptableObject
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Tooltip("One entry per prestige rank label. Order does not affect lookup.")]
        [SerializeField]
        private List<RankBadgeEntry> _badgeEntries = new List<RankBadgeEntry>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Number of configured badge entries.</summary>
        public int Count => _badgeEntries != null ? _badgeEntries.Count : 0;

        /// <summary>
        /// Returns the <see cref="Sprite"/> configured for <paramref name="rankLabel"/>,
        /// or <c>null</c> when no entry matches or the config contains no entries.
        /// The search is case-sensitive. Null or whitespace input always returns null.
        /// </summary>
        public Sprite GetBadge(string rankLabel)
        {
            if (string.IsNullOrWhiteSpace(rankLabel) || _badgeEntries == null)
                return null;

            for (int i = 0; i < _badgeEntries.Count; i++)
            {
                var entry = _badgeEntries[i];
                if (entry == null) continue;
                if (entry.rankLabel == rankLabel)
                    return entry.badgeSprite;
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_badgeEntries == null || _badgeEntries.Count == 0)
                Debug.LogWarning("[RankBadgeConfig] No badge entries configured.", this);
        }
#endif
    }
}
