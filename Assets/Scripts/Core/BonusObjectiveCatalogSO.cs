using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable catalog of <see cref="MatchBonusObjectiveSO"/> assets available
    /// for pre-match selection.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   Assign this SO to <see cref="BattleRobots.UI.BonusObjectiveSelectorController"/>.
    ///   The selector cycles through <see cref="Objectives"/> and injects the
    ///   chosen <see cref="MatchBonusObjectiveSO"/> into
    ///   <see cref="BattleRobots.UI.BonusObjectiveHUDController"/> at match start.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime — never mutate entries after Play enters.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ BonusObjectiveCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/BonusObjectiveCatalog")]
    public sealed class BonusObjectiveCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of bonus objectives the player may choose before a match. " +
                 "Index 0 is the default selection.")]
        [SerializeField] private List<MatchBonusObjectiveSO> _objectives =
            new List<MatchBonusObjectiveSO>();

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>Number of entries in the catalog.</summary>
        public int Count => _objectives != null ? _objectives.Count : 0;

        /// <summary>Read-only ordered view of all bonus objectives in this catalog.</summary>
        public IReadOnlyList<MatchBonusObjectiveSO> Objectives => _objectives;

        /// <summary>
        /// Returns the <see cref="MatchBonusObjectiveSO"/> at <paramref name="index"/>,
        /// or <c>null</c> when the index is out of range or the list is empty.
        /// </summary>
        public MatchBonusObjectiveSO Get(int index)
        {
            if (_objectives == null || index < 0 || index >= _objectives.Count)
                return null;
            return _objectives[index];
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_objectives == null) return;
            for (int i = 0; i < _objectives.Count; i++)
            {
                if (_objectives[i] == null)
                    Debug.LogWarning($"[BonusObjectiveCatalogSO] '{name}': " +
                                     $"Entry at index {i} is null — remove or replace it.");
            }
        }
#endif
    }
}
