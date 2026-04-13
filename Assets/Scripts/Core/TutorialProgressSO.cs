using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks which tutorial steps the player has completed and whether
    /// the full tutorial has been marked finished.
    ///
    /// ── Typical flow ─────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> on startup to
    ///      rehydrate persisted completion state from <see cref="SaveData.tutorialComplete"/>
    ///      and <see cref="SaveData.completedTutorialStepIds"/>.
    ///   2. <see cref="BattleRobots.UI.TutorialController"/> calls
    ///      <see cref="MarkStepComplete"/> after each step is acknowledged.
    ///   3. <see cref="BattleRobots.UI.TutorialController"/> calls <see cref="Complete"/>
    ///      (or <see cref="MarkStepComplete"/> reaching the final step) to flag the whole
    ///      tutorial done; this fires <c>_onTutorialCompleted</c>.
    ///   4. On subsequent boots, <see cref="IsComplete"/> is true so the tutorial is skipped.
    ///
    /// ── Mutators ─────────────────────────────────────────────────────────────────
    ///   • <see cref="MarkStepComplete"/>  — records a single step completion.
    ///   • <see cref="Complete"/>          — flags entire tutorial done; fires event.
    ///   • <see cref="LoadSnapshot"/>      — bootstrapper-safe rehydration (no events).
    ///   • <see cref="Reset"/>             — silent clear (no events).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised to the SO asset.
    ///   - <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire events.
    ///
    /// ── Scene / SO wiring ────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TutorialProgress.
    ///   2. Assign to <see cref="GameBootstrapper"/> (Tutorial optional header).
    ///   3. Assign to <see cref="BattleRobots.UI.TutorialController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/TutorialProgress",
        fileName = "TutorialProgressSO")]
    public sealed class TutorialProgressSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("VoidGameEvent raised when a single tutorial step is marked complete. " +
                 "Leave null if no system needs to react per-step.")]
        [SerializeField] private VoidGameEvent _onStepCompleted;

        [Tooltip("VoidGameEvent raised when the entire tutorial is completed or skipped. " +
                 "Subscribe here to hide the tutorial overlay or unlock a feature. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onTutorialCompleted;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly HashSet<string> _completedStepIds = new HashSet<string>();
        private bool _isComplete;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// True when the player has completed or explicitly skipped the full tutorial.
        /// Persisted via <see cref="SaveData.tutorialComplete"/>.
        /// </summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// Returns true when the step identified by <paramref name="stepId"/> has been
        /// marked complete via <see cref="MarkStepComplete"/>.
        /// Returns false for null or whitespace <paramref name="stepId"/>.
        /// </summary>
        public bool HasCompletedStep(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId)) return false;
            return _completedStepIds.Contains(stepId);
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a step completion and fires <c>_onStepCompleted</c>.
        /// Null or whitespace <paramref name="stepId"/> is a silent no-op.
        /// Idempotent — calling with an already-completed ID is safe.
        /// </summary>
        public void MarkStepComplete(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId)) return;
            _completedStepIds.Add(stepId);
            _onStepCompleted?.Raise();
        }

        /// <summary>
        /// Flags the entire tutorial as complete and fires <c>_onTutorialCompleted</c>.
        /// Idempotent — calling when already complete is safe (event still fires).
        /// </summary>
        public void Complete()
        {
            _isComplete = true;
            _onTutorialCompleted?.Raise();
        }

        /// <summary>
        /// Silently rehydrates state from a <see cref="SaveData"/> snapshot.
        /// Does NOT fire any events — safe to call from <see cref="GameBootstrapper"/>.
        /// Null <paramref name="completedIds"/> is treated as an empty set.
        /// </summary>
        /// <param name="isComplete">Persisted tutorial-complete flag.</param>
        /// <param name="completedIds">
        /// Persisted completed step IDs from <see cref="SaveData.completedTutorialStepIds"/>.
        /// May be null (treated as empty).
        /// </param>
        public void LoadSnapshot(bool isComplete, IReadOnlyList<string> completedIds)
        {
            _isComplete = isComplete;
            _completedStepIds.Clear();
            if (completedIds != null)
            {
                for (int i = 0; i < completedIds.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(completedIds[i]))
                        _completedStepIds.Add(completedIds[i]);
                }
            }
        }

        /// <summary>
        /// Silently clears all completion state. Does NOT fire any events.
        /// Intended for fresh-install resets or test teardown.
        /// </summary>
        public void Reset()
        {
            _isComplete = false;
            _completedStepIds.Clear();
        }
    }
}
