using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable catalog listing every <see cref="AchievementDefinitionSO"/> in the game.
    /// Assign one instance project-wide and reference it from <see cref="AchievementManager"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - Read-only at runtime; treated as immutable in play mode.
    ///   - OnValidate warns on null entries and duplicate achievement IDs.
    ///
    /// ── Scene / SO wiring ─────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ AchievementCatalogSO.
    ///   2. Populate the <c>_achievements</c> list with AchievementDefinitionSO assets.
    ///   3. Assign to <see cref="AchievementManager._catalog"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/AchievementCatalogSO",
        fileName = "AchievementCatalogSO")]
    public sealed class AchievementCatalogSO : ScriptableObject
    {
        [Tooltip("All achievement definitions in display order. " +
                 "Each entry must have a unique, non-empty Id.")]
        [SerializeField]
        private List<AchievementDefinitionSO> _achievements =
            new List<AchievementDefinitionSO>();

        /// <summary>
        /// Read-only view of all achievement definitions in their configured order.
        /// Never null; may be empty on a freshly created asset.
        /// </summary>
        public IReadOnlyList<AchievementDefinitionSO> Achievements => _achievements;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _achievements.Count; i++)
            {
                if (_achievements[i] == null)
                {
                    Debug.LogWarning(
                        $"[AchievementCatalogSO] Null entry at index {i}.", this);
                    continue;
                }

                string id = _achievements[i].Id;
                if (!string.IsNullOrWhiteSpace(id) && !seen.Add(id))
                    Debug.LogWarning(
                        $"[AchievementCatalogSO] Duplicate achievement ID '{id}' " +
                        $"at index {i}.", this);
            }
        }
#endif
    }
}
