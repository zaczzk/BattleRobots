using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-game HUD component showing a health bar for one combatant.
    ///
    /// Usage:
    ///   1. Add one HealthBarUI to the HUD canvas for each robot (player + opponent).
    ///   2. Assign _healthChangedChannel to the <c>FloatGameEvent</c> SO wired as the
    ///      HealthSO's <c>_onHealthChanged</c> event (same asset, separate field).
    ///   3. Set _maxHp to match the HealthSO's asset max HP.
    ///      If RobotSpawner applies HP bonuses, call <see cref="SetMaxHp"/> at runtime.
    ///   4. Optionally set _robotName for the name label.
    ///
    /// Architecture rules observed:
    ///   • <c>BattleRobots.UI</c> namespace — no <c>BattleRobots.Physics</c> references.
    ///   • <c>HandleHealthChanged</c> does only float arithmetic — zero heap allocation.
    ///   • Subscribes to the SO event channel via <c>RegisterCallback</c> (no extra
    ///     <c>FloatGameEventListener</c> component required).
    ///
    /// Inspector wiring checklist:
    ///   □ _slider              → Slider  (fill bar)
    ///   □ _nameLabel           → Text    (robot name, optional)
    ///   □ _hpLabel             → Text    "currentHp / maxHp" readout, optional
    ///   □ _healthChangedChannel → FloatGameEvent SO (same asset as HealthSO._onHealthChanged)
    ///   □ _maxHp               → float  (asset max HP; override at runtime via SetMaxHp)
    ///   □ _robotName           → string (displayed in _nameLabel)
    /// </summary>
    public sealed class HealthBarUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Widgets")]
        [Tooltip("Slider that visualises remaining HP. Value range is set at Awake.")]
        [SerializeField] private Slider _slider;

        [Tooltip("Optional text label showing the robot's name.")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("Optional text label showing 'current / max' HP as integers.")]
        [SerializeField] private Text _hpLabel;

        [Header("Data")]
        [Tooltip("FloatGameEvent SO raised by HealthSO._onHealthChanged. "
               + "Assign the same SO asset used in the HealthSO Inspector.")]
        [SerializeField] private FloatGameEvent _healthChangedChannel;

        [Tooltip("Maximum HP for this robot. Sets the Slider.maxValue in Awake. "
               + "Call SetMaxHp() at runtime when RobotSpawner applies HP bonuses.")]
        [SerializeField, Min(1f)] private float _maxHp = 100f;

        [Tooltip("Display name shown in _nameLabel. Set to the robot's display name.")]
        [SerializeField] private string _robotName = "Robot";

        // ── Testable state ────────────────────────────────────────────────────

        /// <summary>The HP value most recently received via the health-changed channel.</summary>
        public float DisplayedHp  { get; private set; }

        /// <summary>The current max HP used for the slider range and HP label denominator.</summary>
        public float DisplayedMaxHp => _maxHp;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Cached delegate prevents a new Action<float> allocation each OnEnable.
        private System.Action<float> _handleHealthChanged;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleHealthChanged = HandleHealthChanged;

            if (_nameLabel != null)
                _nameLabel.text = _robotName;

            ApplyMaxHp(_maxHp);
        }

        private void OnEnable()
        {
            _healthChangedChannel?.RegisterCallback(_handleHealthChanged);
        }

        private void OnDisable()
        {
            _healthChangedChannel?.UnregisterCallback(_handleHealthChanged);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the bar's maximum HP and resets the display to full.
        /// Call after <c>HealthSO.InitializeWithBonus</c> has been applied so the
        /// effective max is reflected correctly.
        /// Zero heap allocation (float arithmetic only).
        /// </summary>
        public void SetMaxHp(float newMaxHp)
        {
            _maxHp = Mathf.Max(1f, newMaxHp);
            ApplyMaxHp(_maxHp);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        // Called via RegisterCallback each time HealthSO fires _onHealthChanged.
        // Zero heap allocation — float arithmetic and Text.set only.
        private void HandleHealthChanged(float currentHp)
        {
            DisplayedHp = currentHp;

            if (_slider != null)
                _slider.value = currentHp;

            UpdateHpLabel(currentHp);
        }

        private void ApplyMaxHp(float maxHp)
        {
            DisplayedHp = maxHp;

            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = maxHp;
                _slider.value    = maxHp;
            }

            UpdateHpLabel(maxHp);
        }

        private void UpdateHpLabel(float currentHp)
        {
            if (_hpLabel != null)
                _hpLabel.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(_maxHp)}";
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_slider == null)
                Debug.LogWarning("[HealthBarUI] _slider not assigned.", this);
            if (_healthChangedChannel == null)
                Debug.LogWarning("[HealthBarUI] _healthChangedChannel not assigned — bar will not update.", this);
        }
#endif
    }
}
