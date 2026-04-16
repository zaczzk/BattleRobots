using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// The type of metric targeted by a <see cref="ZoneControlWeeklyChallengeSO"/>.
    /// </summary>
    public enum ZoneControlChallengeType
    {
        /// <summary>Challenge target is a cumulative zone capture count.</summary>
        ZoneCount = 0,
        /// <summary>Challenge target is a consecutive capture streak.</summary>
        Streak    = 1,
        /// <summary>Challenge target is a captures-per-minute pace.</summary>
        Pace      = 2,
    }

    /// <summary>
    /// Runtime ScriptableObject that defines a weekly challenge and tracks its
    /// completion state against a configurable target metric.
    ///
    /// ── Supported challenge types ───────────────────────────────────────────────
    ///   ZoneCount — total cumulative zones captured (from SessionSummarySO).
    ///   Streak    — best consecutive capture streak (from SessionSummarySO).
    ///   Pace      — captures-per-minute reading (caller supplies the float).
    ///
    /// ── Evaluation ─────────────────────────────────────────────────────────────
    ///   Call <see cref="EvaluateChallenge"/> with the current metric value.
    ///   Returns true and fires <c>_onChallengeComplete</c> on first completion.
    ///   Subsequent calls while <see cref="IsComplete"/> return true without re-firing.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — relies on LoadSnapshot for persistence.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlWeeklyChallenge.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlWeeklyChallenge", order = 26)]
    public sealed class ZoneControlWeeklyChallengeSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Challenge Definition")]
        [Tooltip("The metric this challenge measures.")]
        [SerializeField] private ZoneControlChallengeType _challengeType = ZoneControlChallengeType.ZoneCount;

        [Tooltip("Value the player must reach or exceed to complete this challenge.")]
        [SerializeField, Min(0.1f)] private float _targetValue = 10f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised the first time the challenge is completed.")]
        [SerializeField] private VoidGameEvent _onChallengeComplete;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool  _isComplete;
        private float _bestValue;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The metric type targeted by this challenge.</summary>
        public ZoneControlChallengeType ChallengeType => _challengeType;

        /// <summary>The target metric value required for completion.</summary>
        public float TargetValue => _targetValue;

        /// <summary>Whether the challenge has been completed this cycle.</summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// Best metric value recorded since last Reset.
        /// Set to <c>_targetValue</c> on completion.
        /// </summary>
        public float BestValue => _bestValue;

        /// <summary>
        /// Progress towards completion, clamped to [0, 1].
        /// Always 1 when complete.
        /// </summary>
        public float Progress =>
            _targetValue > 0f ? Mathf.Clamp01(_bestValue / _targetValue) : 1f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates <paramref name="value"/> against the target.
        /// Fires <c>_onChallengeComplete</c> on first completion.
        /// Returns <c>true</c> when complete (including prior completions).
        /// Zero heap allocation.
        /// </summary>
        public bool EvaluateChallenge(float value)
        {
            if (_isComplete) return true;

            _bestValue = Mathf.Max(_bestValue, value);

            if (value >= _targetValue)
            {
                _isComplete = true;
                _bestValue  = Mathf.Max(_bestValue, _targetValue);
                _onChallengeComplete?.Raise();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Restores persisted challenge state.
        /// Bootstrapper-safe; does not fire any events.
        /// </summary>
        public void LoadSnapshot(bool isComplete, float bestValue)
        {
            _isComplete = isComplete;
            _bestValue  = Mathf.Max(0f, bestValue);
        }

        /// <summary>
        /// Returns current completion state and best value for persistence.
        /// </summary>
        public (bool isComplete, float bestValue) TakeSnapshot() =>
            (_isComplete, _bestValue);

        /// <summary>
        /// Resets the challenge to its initial state.
        /// Does not fire any events.
        /// </summary>
        public void Reset()
        {
            _isComplete = false;
            _bestValue  = 0f;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            _targetValue = Mathf.Max(0.1f, _targetValue);
        }
    }
}
