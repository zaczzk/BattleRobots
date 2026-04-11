using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// An ordered catalogue of <see cref="BonusConditionSO"/> assets evaluated at match end
    /// by <see cref="MatchEndBonusEvaluator"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ MatchBonusCatalog.
    ///
    /// Assign to <c>MatchManager._bonusCatalog</c> (optional inspector field).
    /// When assigned, each satisfied condition adds its <c>BonusAmount</c> to the
    /// match reward before the wallet is credited.  Null entries in the list are
    /// skipped at evaluation time.
    ///
    /// Architecture notes:
    ///   • SO asset is immutable at runtime (IReadOnlyList view).
    ///   • Leaving <c>_bonusCatalog</c> null on MatchManager skips evaluation entirely
    ///     (backwards-compatible — existing wiring is unaffected).
    ///   • No Unity Physics or UI namespace references.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MatchBonusCatalog",
        menuName  = "BattleRobots/Economy/MatchBonusCatalog",
        order     = 1)]
    public sealed class MatchBonusCatalogSO : ScriptableObject
    {
        [Tooltip("Performance-bonus conditions evaluated after every match.\n" +
                 "Null entries are ignored. Order does not affect evaluation.")]
        [SerializeField] private List<BonusConditionSO> _conditions = new List<BonusConditionSO>();

        /// <summary>
        /// Read-only view of the configured bonus conditions.
        /// Null entries may be present; <see cref="MatchEndBonusEvaluator"/> skips them.
        /// </summary>
        public IReadOnlyList<BonusConditionSO> Conditions => _conditions;

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < _conditions.Count; i++)
            {
                if (_conditions[i] == null)
                    Debug.LogWarning($"[MatchBonusCatalogSO] '{name}': " +
                                     $"Null entry at index {i}. Assign or remove.", this);
            }
        }
#endif
    }
}
