using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a rapid consecutive zone-capture combo.
    /// Captures within <see cref="ComboWindow"/> seconds of each other increment the
    /// combo count and raise the multiplier.  When the window expires the combo is lost.
    ///
    /// ── Combo flow ──────────────────────────────────────────────────────────────
    ///   Call <see cref="RecordCapture"/> each time a zone is captured.
    ///   Call <see cref="Tick"/> every frame (or from a MonoBehaviour Update) with
    ///   <c>Time.deltaTime</c> to advance the expiry window.
    ///   Subscribe to <see cref="_onComboLost"/> to react when the window expires.
    ///
    /// ── Multiplier formula ──────────────────────────────────────────────────────
    ///   CurrentMultiplier = Clamp(1 + ComboCount × MultiplierPerCombo, 1, MaxMultiplier)
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlComboTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlComboTracker", order = 32)]
    public sealed class ZoneControlComboTrackerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Combo Settings")]
        [Tooltip("Maximum seconds between captures to maintain the combo.")]
        [Min(0.5f)]
        [SerializeField] private float _comboWindow = 5f;

        [Tooltip("Multiplier added to the base of 1 per combo step.")]
        [Min(0.01f)]
        [SerializeField] private float _multiplierPerCombo = 0.5f;

        [Tooltip("Upper bound for the combo multiplier.")]
        [Min(1f)]
        [SerializeField] private float _maxMultiplier = 5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onComboIncreased;
        [SerializeField] private VoidGameEvent _onComboLost;
        [SerializeField] private VoidGameEvent _onComboUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _comboCount;
        private float _currentMultiplier;
        private float _timeSinceLastCapture;
        private bool  _isActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current consecutive capture count since last reset or loss.</summary>
        public int   ComboCount           => _comboCount;

        /// <summary>Current score multiplier (always ≥ 1).</summary>
        public float CurrentMultiplier    => _currentMultiplier;

        /// <summary>Seconds allowed between captures before the combo expires.</summary>
        public float ComboWindow          => _comboWindow;

        /// <summary>Upper bound on the combo multiplier.</summary>
        public float MaxMultiplier        => _maxMultiplier;

        /// <summary>True while a combo chain is active (at least one capture recorded and window not expired).</summary>
        public bool  IsActive             => _isActive;

        /// <summary>Elapsed seconds since the most recent capture.</summary>
        public float TimeSinceLastCapture => _timeSinceLastCapture;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a zone capture, incrementing the combo and resetting the expiry window.
        /// Raises <see cref="_onComboIncreased"/> and <see cref="_onComboUpdated"/>.
        /// </summary>
        public void RecordCapture()
        {
            _comboCount++;
            _currentMultiplier    = Mathf.Min(1f + _comboCount * _multiplierPerCombo, _maxMultiplier);
            _timeSinceLastCapture = 0f;
            _isActive             = true;

            _onComboIncreased?.Raise();
            _onComboUpdated?.Raise();
        }

        /// <summary>
        /// Advances the expiry timer.  Must be called once per frame with
        /// <c>Time.deltaTime</c>.  When <see cref="ComboWindow"/> elapses without a
        /// new capture <see cref="LoseCombo"/> is called automatically.
        /// Zero heap allocation.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isActive) return;
            _timeSinceLastCapture += dt;
            if (_timeSinceLastCapture >= _comboWindow)
                LoseCombo();
        }

        /// <summary>
        /// Immediately ends the current combo chain, resetting count and multiplier.
        /// Raises <see cref="_onComboLost"/> and <see cref="_onComboUpdated"/>.
        /// No-op when no combo is active.
        /// </summary>
        public void LoseCombo()
        {
            if (!_isActive) return;
            _isActive          = false;
            _comboCount        = 0;
            _currentMultiplier = 1f;
            _onComboLost?.Raise();
            _onComboUpdated?.Raise();
        }

        /// <summary>
        /// Resets all runtime state silently (no events raised).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _comboCount           = 0;
            _currentMultiplier    = 1f;
            _timeSinceLastCapture = 0f;
            _isActive             = false;
        }
    }
}
