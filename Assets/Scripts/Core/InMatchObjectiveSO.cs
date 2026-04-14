using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a single in-match objective
    /// (e.g. "Destroy 3 parts", "Survive 60 seconds").
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. Call <see cref="Reset"/> at match start (or when the objective resets).
    ///   2. Call <see cref="Increment"/> each time the player makes progress toward
    ///      the objective (e.g. from a part-destroyed event listener).
    ///   3. Subscribe <see cref="_onObjectiveComplete"/> for match-completion logic.
    ///
    /// ── Events ──────────────────────────────────────────────────────────────────
    ///   <see cref="_onProgressChanged"/>   — raised after every Increment and Reset.
    ///   <see cref="_onObjectiveComplete"/> — raised once when CurrentCount reaches TargetCount.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on Increment (integer arithmetic only).
    ///   - SO assets are immutable at runtime — only progress-state fields mutate.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ InMatchObjective.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/InMatchObjective")]
    public sealed class InMatchObjectiveSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Objective Settings")]
        [Tooltip("Human-readable title shown in the HUD, e.g. 'Destroy Parts'.")]
        [SerializeField] private string _objectiveTitle = "Objective";

        [Tooltip("Number of increments required to complete the objective. Minimum 1.")]
        [SerializeField, Min(1)] private int _targetCount = 1;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after every Increment and Reset call.")]
        [SerializeField] private VoidGameEvent _onProgressChanged;

        [Tooltip("Raised once when CurrentCount first reaches TargetCount.")]
        [SerializeField] private VoidGameEvent _onObjectiveComplete;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _currentCount;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable objective title.
        /// Falls back to "Objective" when the serialised field is null or empty.
        /// </summary>
        public string ObjectiveTitle =>
            string.IsNullOrEmpty(_objectiveTitle) ? "Objective" : _objectiveTitle;

        /// <summary>Number of increments required to complete the objective (minimum 1).</summary>
        public int TargetCount => _targetCount;

        /// <summary>Number of increments accumulated so far. Always in [0, TargetCount].</summary>
        public int CurrentCount => _currentCount;

        /// <summary>True when CurrentCount has reached TargetCount.</summary>
        public bool IsComplete => _currentCount >= _targetCount;

        /// <summary>
        /// Normalised progress ratio in [0, 1] (0 = not started; 1 = complete).
        /// Suitable for driving a Slider.value or Image.fillAmount directly.
        /// </summary>
        public float Progress =>
            _targetCount > 0 ? Mathf.Clamp01((float)_currentCount / _targetCount) : 0f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _currentCount = 0;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the objective by one step.
        /// No-op when the objective is already complete (<see cref="IsComplete"/> == true).
        /// Fires <see cref="_onProgressChanged"/>.
        /// Fires <see cref="_onObjectiveComplete"/> when CurrentCount first reaches TargetCount.
        /// Zero allocation — integer arithmetic only.
        /// </summary>
        public void Increment()
        {
            if (IsComplete) return;

            _currentCount++;
            _onProgressChanged?.Raise();

            if (IsComplete)
                _onObjectiveComplete?.Raise();
        }

        /// <summary>
        /// Resets CurrentCount to zero.
        /// Fires <see cref="_onProgressChanged"/>.
        /// Call at match start (wire via VoidGameEventListener MatchStarted → Reset).
        /// </summary>
        public void Reset()
        {
            _currentCount = 0;
            _onProgressChanged?.Raise();
        }
    }
}
