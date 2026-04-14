using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO representing an optional bonus objective that a player may
    /// complete during a match for an extra credit reward (e.g. "Win without
    /// taking damage", "Defeat opponent in under 60 seconds").
    ///
    /// ── Typical flow ─────────────────────────────────────────────────────────
    ///   1. At match start, call <see cref="Reset"/> to clear completion state.
    ///   2. Any system that detects the bonus condition calls <see cref="Complete"/>.
    ///   3. <see cref="BonusObjectiveHUDController"/> subscribes to the completion
    ///      event and updates the HUD overlay.
    ///
    /// ── Mutators ─────────────────────────────────────────────────────────────
    ///   <see cref="Complete"/> — marks the objective done and raises <c>_onCompleted</c>.
    ///   <see cref="Reset"/>    — clears completion state silently (no event).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state (<see cref="IsCompleted"/>) is not serialised to the SO asset.
    ///   - <see cref="Reset"/> is silent (bootstrapper / match-start safe).
    ///   - <see cref="Complete"/> fires the event even if already completed — callers
    ///     that want one-shot behaviour should guard with <see cref="IsCompleted"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ MatchBonusObjective.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/MatchBonusObjective",
        fileName = "MatchBonusObjectiveSO")]
    public sealed class MatchBonusObjectiveSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Short title shown in the HUD, e.g. 'Win Without Taking Damage'.")]
        [SerializeField] private string _objectiveTitle = "Bonus Objective";

        [Tooltip("Bonus credits awarded when this objective is completed. " +
                 "Add reward logic in the system that calls Complete().")]
        [SerializeField, Min(0)] private int _bonusReward = 100;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised when Complete() is called. " +
                 "Subscribe BonusObjectiveHUDController to show the completed overlay.")]
        [SerializeField] private VoidGameEvent _onCompleted;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _isCompleted;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            _isCompleted = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Display title for this bonus objective.
        /// Falls back to "Bonus Objective" if the serialised field is empty.
        /// </summary>
        public string ObjectiveTitle =>
            string.IsNullOrEmpty(_objectiveTitle) ? "Bonus Objective" : _objectiveTitle;

        /// <summary>Credit reward for completing this objective.</summary>
        public int BonusReward => _bonusReward;

        /// <summary>True once <see cref="Complete"/> has been called this match.</summary>
        public bool IsCompleted => _isCompleted;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Marks this objective as completed and raises <c>_onCompleted</c>.
        /// <para>
        /// Fires the event even if already completed — callers that require one-shot
        /// behaviour should guard with <c>if (!IsCompleted)</c> before calling.
        /// </para>
        /// </summary>
        public void Complete()
        {
            _isCompleted = true;
            _onCompleted?.Raise();
        }

        /// <summary>
        /// Clears <see cref="IsCompleted"/> without raising any event.
        /// Call at match start to prepare a fresh challenge.
        /// </summary>
        public void Reset()
        {
            _isCompleted = false;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_objectiveTitle))
                Debug.LogWarning($"[MatchBonusObjectiveSO] '{name}': " +
                                 "_objectiveTitle is empty — will fall back to 'Bonus Objective'.");
        }
#endif
    }
}
