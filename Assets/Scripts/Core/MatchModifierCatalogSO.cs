using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable SO listing all available match modifiers shown to the player
    /// before entering the arena.
    ///
    /// Each entry is a <see cref="MatchModifierSO"/> asset that pairs a display
    /// name with four gameplay multipliers (reward, time, armor, speed).
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   Assign this SO to
    ///   <see cref="BattleRobots.UI.MatchModifierSelectionController"/>.
    ///   The controller cycles through <see cref="Modifiers"/> and writes the
    ///   chosen modifier to <see cref="SelectedModifierSO"/>.
    ///
    ///   <see cref="MatchManager"/> and
    ///   <see cref="BattleRobots.Physics.CombatStatsApplicator"/> both read from
    ///   <see cref="SelectedModifierSO"/>; they do not reference this catalog
    ///   directly.
    ///
    /// Suggested starter list (one SO per entry):
    ///   Standard       — all multipliers 1.0.
    ///   DoubleRewards  — RewardMultiplier 2.0.
    ///   ExtendedTime   — TimeMultiplier   2.0.
    ///   ShortTime      — TimeMultiplier   0.5.
    ///   FragileArmor   — ArmorMultiplier  0.0.
    ///   Overdrive      — SpeedMultiplier  1.5.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Match ▶ MatchModifierCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Match/MatchModifierCatalog", order = 2)]
    public sealed class MatchModifierCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of available match modifiers. Index 0 is the default selection.")]
        [SerializeField] private List<MatchModifierSO> _modifiers = new List<MatchModifierSO>();

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>
        /// Ordered, read-only list of available match modifier SOs.
        /// Index 0 is the default selection shown to the player.
        /// </summary>
        public IReadOnlyList<MatchModifierSO> Modifiers => _modifiers;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_modifiers == null || _modifiers.Count == 0) return;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i] == null)
                    Debug.LogWarning($"[MatchModifierCatalogSO] '{name}': " +
                                     $"Modifier at index {i} is null — remove or replace it.");
            }
        }
#endif
    }
}
