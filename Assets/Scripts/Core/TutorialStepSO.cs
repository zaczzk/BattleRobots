using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single immutable step in an in-game tutorial sequence.
    /// Each step supplies a header, a body paragraph, and a unique identifier used
    /// by <see cref="TutorialProgressSO"/> to track completion.
    ///
    /// ── Scene / SO wiring ────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TutorialStep.
    ///   2. Add to a <see cref="TutorialSequenceSO"/> Steps list.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All fields are read-only at runtime (SO assets immutable after creation).
    ///   - OnValidate warns when StepId is empty — steps must have a unique ID.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/TutorialStep",
        fileName = "TutorialStepSO")]
    public sealed class TutorialStepSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Unique identifier for this step, used by TutorialProgressSO to track completion. " +
                 "Must be non-empty and unique within the sequence.")]
        [SerializeField] private string _stepId = "";

        [Tooltip("Short heading displayed at the top of the tutorial overlay (e.g. 'Assemble Your Robot').")]
        [SerializeField] private string _headerText = "";

        [Tooltip("Instructional body paragraph shown beneath the header.")]
        [SerializeField] private string _bodyText = "";

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Unique string identifier for this tutorial step.
        /// Used by <see cref="TutorialProgressSO.MarkStepComplete"/> and
        /// <see cref="TutorialProgressSO.HasCompletedStep"/>.
        /// </summary>
        public string StepId => _stepId;

        /// <summary>
        /// Short heading text displayed at the top of the tutorial overlay panel.
        /// </summary>
        public string HeaderText => _headerText;

        /// <summary>
        /// Instructional body text displayed beneath the header.
        /// </summary>
        public string BodyText => _bodyText;

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_stepId))
                Debug.LogWarning($"[TutorialStepSO] '{name}': StepId is empty. " +
                                 "Each step must have a unique, non-empty StepId.");
        }
    }
}
