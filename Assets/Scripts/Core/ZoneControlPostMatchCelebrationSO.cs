using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that drives a sequential post-match celebration
    /// flow through four steps: Winner → ScoreTally → MVPReveal → Complete.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="StartCelebration"/> to arm the sequence.
    ///   Each <see cref="Tick"/> call decrements the step timer; when it expires the
    ///   sequence advances.  On reaching <see cref="CelebrationStep.Complete"/> the
    ///   <c>_onCelebrationComplete</c> event fires and <see cref="IsRunning"/> becomes
    ///   false.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlPostMatchCelebration.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlPostMatchCelebration", order = 62)]
    public sealed class ZoneControlPostMatchCelebrationSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Seconds spent on each celebration step before advancing.")]
        [Min(0.1f)]
        [SerializeField] private float _stepDuration = 2f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised whenever the current celebration step changes.")]
        [SerializeField] private VoidGameEvent _onStepChanged;

        [Tooltip("Raised when the sequence reaches the Complete step.")]
        [SerializeField] private VoidGameEvent _onCelebrationComplete;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private CelebrationStep _currentStep;
        private float           _stepTimer;
        private bool            _isRunning;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The celebration step currently being displayed.</summary>
        public CelebrationStep CurrentStep => _currentStep;

        /// <summary>True while the celebration sequence is actively ticking.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Seconds each step is shown before advancing.</summary>
        public float StepDuration => _stepDuration;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Arms the celebration sequence at the <see cref="CelebrationStep.Winner"/> step.
        /// Fires <c>_onStepChanged</c>.
        /// </summary>
        public void StartCelebration()
        {
            _currentStep = CelebrationStep.Winner;
            _stepTimer   = Mathf.Max(0.1f, _stepDuration);
            _isRunning   = true;
            _onStepChanged?.Raise();
        }

        /// <summary>
        /// Advances the celebration by <paramref name="dt"/> seconds.
        /// No-op when <see cref="IsRunning"/> is false.
        /// Fires <c>_onStepChanged</c> on each step transition and
        /// <c>_onCelebrationComplete</c> when the sequence reaches
        /// <see cref="CelebrationStep.Complete"/>.
        /// Zero allocation.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isRunning) return;

            _stepTimer -= dt;
            if (_stepTimer > 0f) return;

            // Advance to the next step.
            int next = (int)_currentStep + 1;
            _currentStep = (CelebrationStep)next;

            if (_currentStep >= CelebrationStep.Complete)
            {
                _currentStep = CelebrationStep.Complete;
                _isRunning   = false;
                _onCelebrationComplete?.Raise();
                return;
            }

            _stepTimer = Mathf.Max(0.1f, _stepDuration);
            _onStepChanged?.Raise();
        }

        /// <summary>
        /// Resets the celebration to its initial stopped state.
        /// Called automatically by <c>OnEnable</c>.
        /// No events fired.
        /// </summary>
        public void Reset()
        {
            _currentStep = CelebrationStep.Winner;
            _stepTimer   = 0f;
            _isRunning   = false;
        }

        private void OnValidate()
        {
            _stepDuration = Mathf.Max(0.1f, _stepDuration);
        }
    }

    /// <summary>Ordered steps in the post-match celebration sequence.</summary>
    public enum CelebrationStep
    {
        Winner    = 0,
        ScoreTally = 1,
        MVPReveal  = 2,
        Complete   = 3,
    }
}
