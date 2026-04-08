using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject that holds the master list of every
    /// <see cref="AchievementDefinition"/> in the game.
    ///
    /// ── Design rules ──────────────────────────────────────────────────────────
    ///   • The list is read-only at runtime — assign definitions in the Inspector
    ///     and never mutate <see cref="Achievements"/> in play mode.
    ///   • IDs must be unique; the Editor OnValidate guard warns when duplicates
    ///     are detected.
    ///   • <see cref="GetById"/> performs a linear scan, which is acceptable for
    ///     small catalogs (&lt; 100 entries).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   Assign this SO to <see cref="AchievementProgressSO._catalog"/>.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ Achievements ▶ AchievementCatalogSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Achievements/AchievementCatalogSO", order = 1)]
    public sealed class AchievementCatalogSO : ScriptableObject
    {
        [Tooltip("Ordered list of all AchievementDefinition assets. IDs must be unique and non-empty.")]
        [SerializeField] private List<AchievementDefinition> _achievements = new List<AchievementDefinition>();

        /// <summary>All achievement definitions, in the order they appear in the Inspector.</summary>
        public IReadOnlyList<AchievementDefinition> Achievements => _achievements;

        /// <summary>Total number of achievements in the catalog.</summary>
        public int Count => _achievements.Count;

        /// <summary>
        /// Returns the <see cref="AchievementDefinition"/> whose
        /// <see cref="AchievementDefinition.AchievementId"/> matches <paramref name="id"/>,
        /// or <c>null</c> if no match is found.
        /// Comparison is case-sensitive (Ordinal). Linear scan — suitable for small catalogs.
        /// </summary>
        public AchievementDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            for (int i = 0; i < _achievements.Count; i++)
            {
                var def = _achievements[i];
                if (def != null && def.AchievementId == id)
                    return def;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < _achievements.Count; i++)
            {
                var def = _achievements[i];
                if (def == null) continue;

                if (string.IsNullOrEmpty(def.AchievementId))
                {
                    Debug.LogWarning($"[AchievementCatalogSO] Entry [{i}] has an empty AchievementId.", this);
                    continue;
                }

                if (!seen.Add(def.AchievementId))
                    Debug.LogWarning($"[AchievementCatalogSO] Duplicate AchievementId '{def.AchievementId}' " +
                                     $"at index [{i}]. IDs must be unique.", this);
            }
        }
#endif
    }
}
