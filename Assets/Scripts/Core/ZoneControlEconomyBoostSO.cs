using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that manages a time-limited currency multiplier ("economy boost").
    /// When active, <see cref="ApplyBoost"/> scales any currency amount by
    /// <c>_boostMultiplier</c>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="ActivateBoost"/> arms the timer and fires
    ///   <c>_onBoostActivated</c>.  Calling it again while active restarts the
    ///   timer.
    ///   <see cref="Tick"/> should be called every frame (or at a fixed rate) to
    ///   decrement the remaining time; fires <c>_onBoostExpired</c> on expiry.
    ///   <see cref="BoostProgress"/> returns a [0,1] fraction of time remaining;
    ///   0 when inactive.
    ///   <see cref="ApplyBoost"/> multiplies <paramref name="baseAmount"/> by
    ///   <c>_boostMultiplier</c> when active; passes through unchanged otherwise.
    ///   <see cref="Reset"/> clears state silently; called from <c>OnEnable</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on play-mode entry.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlEconomyBoost.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlEconomyBoost", order = 74)]
    public sealed class ZoneControlEconomyBoostSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Boost Settings")]
        [Tooltip("Currency multiplier applied while the boost is active.")]
        [Min(1f)]
        [SerializeField] private float _boostMultiplier = 2f;

        [Tooltip("Duration of the boost in seconds.")]
        [Min(1f)]
        [SerializeField] private float _boostDuration = 300f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time the boost is activated.")]
        [SerializeField] private VoidGameEvent _onBoostActivated;

        [Tooltip("Raised when the boost timer expires.")]
        [SerializeField] private VoidGameEvent _onBoostExpired;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _remainingTime;
        private bool  _isActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Currency multiplier applied while the boost is active.</summary>
        public float BoostMultiplier => _boostMultiplier;

        /// <summary>Configured boost duration in seconds.</summary>
        public float BoostDuration   => _boostDuration;

        /// <summary>True while the boost timer is running.</summary>
        public bool  IsActive        => _isActive;

        /// <summary>Remaining boost time in seconds (0 when inactive).</summary>
        public float RemainingTime   => _remainingTime;

        /// <summary>
        /// Remaining time as a [0,1] fraction of the full boost duration.
        /// Returns 0 when the boost is inactive.
        /// </summary>
        public float BoostProgress   => _isActive ? _remainingTime / _boostDuration : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts (or restarts) the economy boost timer and fires
        /// <c>_onBoostActivated</c>.
        /// </summary>
        public void ActivateBoost()
        {
            _remainingTime = _boostDuration;
            _isActive      = true;
            _onBoostActivated?.Raise();
        }

        /// <summary>
        /// Decrements the remaining time by <paramref name="dt"/> seconds.
        /// No-ops when inactive.  Fires <c>_onBoostExpired</c> on expiry.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isActive) return;

            _remainingTime -= dt;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _isActive      = false;
                _onBoostExpired?.Raise();
            }
        }

        /// <summary>
        /// Returns <paramref name="baseAmount"/> scaled by <see cref="BoostMultiplier"/>
        /// when the boost is active; returns <paramref name="baseAmount"/> unchanged
        /// when inactive.
        /// </summary>
        public int ApplyBoost(int baseAmount) =>
            _isActive ? Mathf.RoundToInt(baseAmount * _boostMultiplier) : baseAmount;

        /// <summary>
        /// Clears all runtime state silently.  Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _remainingTime = 0f;
            _isActive      = false;
        }
    }
}
