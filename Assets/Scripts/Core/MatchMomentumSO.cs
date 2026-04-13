using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks "momentum" — a match-wide combat energy bar
    /// that rewards aggressive play (kills, combos, crits) and decays passively over time.
    ///
    /// ── Momentum rules ──────────────────────────────────────────────────────────
    ///   • <see cref="AddMomentum"/> increases the value (clamped to [0, MaxMomentum]).
    ///   • <see cref="Tick"/> (called from a MomentumHUDController.Update) decays the
    ///     value by <see cref="DecayRate"/> units per second toward zero.
    ///   • When momentum reaches <see cref="MaxMomentum"/> for the first time since it
    ///     last dropped below max, <see cref="_onMomentumFull"/> fires once.
    ///   • <see cref="_onMomentumChanged"/> fires on every <see cref="AddMomentum"/>
    ///     and <see cref="Tick"/> call that changes the value.
    ///   • <see cref="Reset"/> sets momentum to zero and fires _onMomentumChanged.
    ///
    /// ── Integration ─────────────────────────────────────────────────────────────
    ///   • Wire AddMomentum() via a VoidGameEventListener on each kill event, crit event,
    ///     or new-max-combo event for different reward amounts.
    ///   • Call Reset() at match start (wire MatchStarted → MatchMomentumSO.Reset).
    ///   • MomentumHUDController calls Tick(Time.deltaTime) in Update and subscribes to
    ///     _onMomentumChanged to refresh the fill bar.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot-path methods (float arithmetic only).
    ///   - SO assets are immutable at runtime — only AddMomentum/Tick/Reset mutate state.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ MatchMomentum.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/MatchMomentum")]
    public sealed class MatchMomentumSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Momentum Settings")]
        [Tooltip("Maximum momentum value. The bar fills from 0 to this value.")]
        [SerializeField, Min(1f)] private float _maxMomentum = 100f;

        [Tooltip("Momentum lost per second passively. Set 0 to disable decay.")]
        [SerializeField, Min(0f)] private float _decayRate = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised on every AddMomentum call and on each Tick that changes the value. " +
                 "Wire to MomentumHUDController to update the fill bar reactively.")]
        [SerializeField] private VoidGameEvent _onMomentumChanged;

        [Tooltip("Raised once each time momentum transitions from below-max to exactly max. " +
                 "Use for a 'FULL MOMENTUM!' sound or screen flash.")]
        [SerializeField] private VoidGameEvent _onMomentumFull;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _momentum;
        private bool  _wasAtMax;   // tracks edge for _onMomentumFull

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current momentum value. Always in [0, MaxMomentum].</summary>
        public float Momentum => _momentum;

        /// <summary>Maximum momentum capacity configured on this asset.</summary>
        public float MaxMomentum => _maxMomentum;

        /// <summary>Passive decay rate in units per second.</summary>
        public float DecayRate => _decayRate;

        /// <summary>
        /// Normalised momentum ratio in [0, 1].
        /// Suitable for driving a Slider.value directly.
        /// </summary>
        public float MomentumRatio =>
            _maxMomentum > 0f ? Mathf.Clamp01(_momentum / _maxMomentum) : 0f;

        /// <summary>True when momentum is at its maximum capacity.</summary>
        public bool IsAtMax => _momentum >= _maxMomentum;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Reset to zero each time the SO is enabled (new play session / domain reload).
            _momentum  = 0f;
            _wasAtMax  = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="amount"/> to the current momentum, clamped to MaxMomentum.
        /// Fires <see cref="_onMomentumChanged"/> on every call.
        /// Fires <see cref="_onMomentumFull"/> when momentum transitions from below-max to max.
        /// Zero allocation — value-type arithmetic only.
        /// </summary>
        public void AddMomentum(float amount)
        {
            if (amount <= 0f) return;

            bool wasBelow = !IsAtMax;
            _momentum = Mathf.Clamp(_momentum + amount, 0f, _maxMomentum);

            _onMomentumChanged?.Raise();

            if (wasBelow && IsAtMax)
            {
                _wasAtMax = true;
                _onMomentumFull?.Raise();
            }
        }

        /// <summary>
        /// Decays momentum by <see cref="DecayRate"/> × <paramref name="deltaTime"/> seconds.
        /// Must be driven externally (e.g., MomentumHUDController.Update).
        /// No-op when momentum is already zero. Fires <see cref="_onMomentumChanged"/>
        /// only when the value actually changes.
        /// Zero allocation.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_momentum <= 0f || _decayRate <= 0f) return;

            float prev    = _momentum;
            _momentum     = Mathf.Max(0f, _momentum - _decayRate * deltaTime);
            _wasAtMax     = IsAtMax;    // keep edge state in sync after decay

            if (_momentum != prev)
                _onMomentumChanged?.Raise();
        }

        /// <summary>
        /// Resets momentum to zero and fires <see cref="_onMomentumChanged"/>.
        /// Call at match start (wire MatchStarted → MatchMomentumSO.Reset).
        /// </summary>
        public void Reset()
        {
            _momentum = 0f;
            _wasAtMax = false;
            _onMomentumChanged?.Raise();
        }
    }
}
