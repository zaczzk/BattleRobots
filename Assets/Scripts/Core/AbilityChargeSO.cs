using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a charge-up ability bar.
    ///
    /// The ability charges by accumulating damage dealt by the player
    /// (<see cref="AddCharge"/>).  Once <see cref="IsFullyCharged"/> is true the
    /// player can call <see cref="Activate"/> to discharge and fire the ability.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. Wire <see cref="_onChargeChanged"/> → AbilityChargeHUDController.Refresh()
    ///      so the HUD updates reactively.
    ///   2. Call <see cref="AddCharge"/> from a damage-dealt event listener, passing
    ///      the damage value — <see cref="ChargePerDamage"/> converts it to charge.
    ///   3. Call <see cref="Activate"/> from a button/input event — discharges only
    ///      when fully charged.
    ///   4. Call <see cref="Reset"/> at match start (wire via VoidGameEventListener).
    ///
    /// ── Events ──────────────────────────────────────────────────────────────────
    ///   <see cref="_onChargeChanged"/>  — raised after every AddCharge, Activate, Reset.
    ///   <see cref="_onFullyCharged"/>   — raised once when the bar crosses 100 %.
    ///   <see cref="_onActivated"/>      — raised when Activate() discharges successfully.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on AddCharge / Activate (float arithmetic only).
    ///   - SO assets are immutable at runtime — only charge-state fields mutate.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ AbilityCharge.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/AbilityCharge")]
    public sealed class AbilityChargeSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Charge Settings")]
        [Tooltip("Maximum charge required to activate the ability.")]
        [SerializeField, Min(1f)] private float _maxCharge = 100f;

        [Tooltip("Charge gained per point of damage dealt by the player. " +
                 "E.g. 1 = 1 charge per 1 damage; 0.5 = half charge per damage.")]
        [SerializeField, Min(0f)] private float _chargePerDamage = 1f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after AddCharge, Activate, and Reset.")]
        [SerializeField] private VoidGameEvent _onChargeChanged;

        [Tooltip("Raised once when current charge first reaches MaxCharge.")]
        [SerializeField] private VoidGameEvent _onFullyCharged;

        [Tooltip("Raised when Activate() successfully discharges the ability.")]
        [SerializeField] private VoidGameEvent _onActivated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _currentCharge;
        private bool  _fullyChargedFired; // edge-guard — fires _onFullyCharged once per fill

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum charge required to activate the ability.</summary>
        public float MaxCharge => _maxCharge;

        /// <summary>Charge gained per point of damage dealt.</summary>
        public float ChargePerDamage => _chargePerDamage;

        /// <summary>Current accumulated charge. Always in [0, MaxCharge].</summary>
        public float CurrentCharge => _currentCharge;

        /// <summary>
        /// Normalised charge ratio in [0, 1] (0 = empty; 1 = full).
        /// Suitable for driving a fill-bar Image.fillAmount or Slider.value directly.
        /// </summary>
        public float ChargeRatio =>
            _maxCharge > 0f ? Mathf.Clamp01(_currentCharge / _maxCharge) : 0f;

        /// <summary>True when CurrentCharge has reached MaxCharge.</summary>
        public bool IsFullyCharged => _currentCharge >= _maxCharge;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _currentCharge      = 0f;
            _fullyChargedFired  = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="damageDealt"/> × <see cref="ChargePerDamage"/> to the
        /// current charge, clamped to [0, MaxCharge].
        /// No-op when damageDealt ≤ 0.
        /// Fires <see cref="_onChargeChanged"/>.
        /// Fires <see cref="_onFullyCharged"/> once when the bar first becomes full.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void AddCharge(float damageDealt)
        {
            if (damageDealt <= 0f) return;

            bool wasFull = IsFullyCharged;
            _currentCharge = Mathf.Min(_maxCharge, _currentCharge + damageDealt * _chargePerDamage);
            _onChargeChanged?.Raise();

            if (IsFullyCharged && !wasFull && !_fullyChargedFired)
            {
                _fullyChargedFired = true;
                _onFullyCharged?.Raise();
            }
        }

        /// <summary>
        /// Activates the ability if the charge bar is full.
        /// Resets <see cref="CurrentCharge"/> to zero, then fires
        /// <see cref="_onActivated"/> and <see cref="_onChargeChanged"/>.
        /// No-op when <see cref="IsFullyCharged"/> is false.
        /// </summary>
        public void Activate()
        {
            if (!IsFullyCharged) return;

            _currentCharge     = 0f;
            _fullyChargedFired = false;
            _onActivated?.Raise();
            _onChargeChanged?.Raise();
        }

        /// <summary>
        /// Resets the charge bar to zero.
        /// Fires <see cref="_onChargeChanged"/>.
        /// Call at match start (wire via VoidGameEventListener MatchStarted → Reset).
        /// </summary>
        public void Reset()
        {
            _currentCharge     = 0f;
            _fullyChargedFired = false;
            _onChargeChanged?.Raise();
        }
    }
}
