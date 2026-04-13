using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// An ordered list of <see cref="TutorialStepSO"/> assets that define a complete
    /// tutorial flow.  Consumed by <see cref="BattleRobots.UI.TutorialController"/> to
    /// walk the player through the sequence one step at a time.
    ///
    /// ── Scene / SO wiring ────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TutorialSequence.
    ///   2. Populate the Steps list with TutorialStepSO assets in the desired order.
    ///   3. Assign to <see cref="BattleRobots.UI.TutorialController._sequence"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - The Steps list is immutable at runtime (SO assets immutable after creation).
    ///   - OnValidate warns on null entries and logs duplicate StepIds.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/TutorialSequence",
        fileName = "TutorialSequenceSO")]
    public sealed class TutorialSequenceSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of tutorial steps. Add TutorialStepSO assets in the sequence " +
                 "the player should encounter them. Null entries are ignored with a warning.")]
        [SerializeField] private List<TutorialStepSO> _steps = new List<TutorialStepSO>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Immutable ordered list of tutorial steps.
        /// Never null; may be empty if no steps have been added.
        /// </summary>
        public IReadOnlyList<TutorialStepSO> Steps => _steps;

        /// <summary>Number of steps in this sequence.</summary>
        public int Count => _steps.Count;

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            var seenIds = new HashSet<string>();
            for (int i = 0; i < _steps.Count; i++)
            {
                if (_steps[i] == null)
                {
                    Debug.LogWarning($"[TutorialSequenceSO] '{name}': Null entry at index {i}. " +
                                     "Remove or replace it.");
                    continue;
                }

                string id = _steps[i].StepId;
                if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id))
                    Debug.LogWarning($"[TutorialSequenceSO] '{name}': Duplicate StepId '{id}' " +
                                     $"at index {i}. Each step must have a unique StepId.");
            }
        }
    }
}
