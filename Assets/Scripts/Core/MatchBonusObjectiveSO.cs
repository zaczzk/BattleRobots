using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject representing an optional time-limited bonus objective
    /// (e.g. "Win without taking damage", "Destroy 3 parts in 30s").
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. <see cref="OnEnable"/> (or <see cref="Reset"/>) initialises state.
    ///   2. Tick <see cref="Tick"/> each frame (dt = Time.deltaTime) to countdown the
    ///      time limit when <see cref="HasTimeLimit"/> is true.
    ///   3. Call <see cref="Complete"/> when the bonus condition is satisfied.
    ///   4. <see cref="Expire"/> is called automatically by <see cref="Tick"/> when the
    ///      timer reaches zero; may also be called externally (e.g. match ended early).
    ///
    /// ── Event semantics ──────────────────────────────────────────────────────────
    ///   <see cref="_onCompleted"/> — fired once when the objective is completed.
    ///   <see cref="_onExpired"/>   — fired once when the time limit runs out (not on Complete).
    ///   <see cref="_onChanged"/>   — fired on every state change (Complete/Expire/Tick/Reset).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on Tick (float arithmetic only).
    ///   - SO state resets on OnEnable so Play-mode restarts begin clean.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchBonusObjective.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/MatchBonusObjective")]
    public sealed class MatchBonusObjectiveSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Objective Settings")]
        [Tooltip("Human-readable title shown in the HUD, e.g. 'Survive without damage'.")]
        [SerializeField] private string _bonusTitle = "Bonus Objective";

        [Tooltip("Currency reward awarded when the objective is completed.")]
        [SerializeField, Min(0)] private int _bonusReward = 50;

        [Tooltip("Time limit in seconds. Set to 0 for no time limit (the objective never expires).")]
        [SerializeField, Min(0f)] private float _timeLimit = 60f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once when the objective is completed (Complete() called).")]
        [SerializeField] private VoidGameEvent _onCompleted;

        [Tooltip("Raised once when the time limit expires (Expire() called or Tick reaches 0).")]
        [SerializeField] private VoidGameEvent _onExpired;

        [Tooltip("Raised on every state change: Complete, Expire, Tick, and Reset.")]
        [SerializeField] private VoidGameEvent _onChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool  _isComplete;
        private bool  _isExpired;
        private float _timeRemaining;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _isComplete    = false;
            _isExpired     = false;
            _timeRemaining = _timeLimit;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable bonus objective title.
        /// Falls back to "Bonus Objective" when the serialised field is null or empty.
        /// </summary>
        public string BonusTitle =>
            string.IsNullOrEmpty(_bonusTitle) ? "Bonus Objective" : _bonusTitle;

        /// <summary>Currency reward for completing this objective.</summary>
        public int BonusReward => _bonusReward;

        /// <summary>True when the objective has been successfully completed.</summary>
        public bool IsComplete => _isComplete;

        /// <summary>True when the time limit ran out before the objective was completed.</summary>
        public bool IsExpired => _isExpired;

        /// <summary>Seconds remaining on the timer (0 when expired or no time limit).</summary>
        public float TimeRemaining => _timeRemaining;

        /// <summary>True when a time limit is set (<see cref="_timeLimit"/> &gt; 0).</summary>
        public bool HasTimeLimit => _timeLimit > 0f;

        /// <summary>
        /// Normalised timer ratio in [0, 1].
        /// 1 = full time remaining; 0 = expired.
        /// Always 1 when <see cref="HasTimeLimit"/> is false (no time pressure).
        /// Suitable for driving a Slider or radial progress bar.
        /// </summary>
        public float TimeRatio =>
            HasTimeLimit ? Mathf.Clamp01(_timeRemaining / _timeLimit) : 1f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Marks the objective as complete.
        /// No-op when already complete or already expired.
        /// Fires <see cref="_onCompleted"/> and <see cref="_onChanged"/>.
        /// </summary>
        public void Complete()
        {
            if (_isComplete || _isExpired) return;

            _isComplete = true;
            _onCompleted?.Raise();
            _onChanged?.Raise();
        }

        /// <summary>
        /// Marks the objective as expired (time ran out).
        /// No-op when already expired or already completed.
        /// Fires <see cref="_onExpired"/> and <see cref="_onChanged"/>.
        /// </summary>
        public void Expire()
        {
            if (_isExpired || _isComplete) return;

            _isExpired = true;
            _onExpired?.Raise();
            _onChanged?.Raise();
        }

        /// <summary>
        /// Advances the timer by <paramref name="deltaTime"/> seconds.
        /// No-op when complete, expired, or <see cref="HasTimeLimit"/> is false.
        /// Raises <see cref="_onChanged"/> on every decrement.
        /// Automatically calls <see cref="Expire"/> when the timer reaches zero.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_isComplete || _isExpired || !HasTimeLimit) return;

            _timeRemaining = Mathf.Max(0f, _timeRemaining - deltaTime);
            _onChanged?.Raise();

            if (_timeRemaining <= 0f)
                Expire();
        }

        /// <summary>
        /// Resets all runtime state (including the timer) to initial values.
        /// Raises <see cref="_onChanged"/> after reset.
        /// Does NOT fire <see cref="_onCompleted"/> or <see cref="_onExpired"/>.
        /// Call at match start via a VoidGameEventListener.
        /// </summary>
        public void Reset()
        {
            _isComplete    = false;
            _isExpired     = false;
            _timeRemaining = _timeLimit;
            _onChanged?.Raise();
        }
    }
}
