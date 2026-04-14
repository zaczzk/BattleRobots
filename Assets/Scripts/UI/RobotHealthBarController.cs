using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that displays a robot's health bar, numeric label,
    /// and an optional critical-health overlay.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _healthDelegate (Action&lt;float&gt;).
    ///   OnEnable  → subscribes _onHealthChanged FloatGameEvent → Refresh();
    ///               calls Refresh() immediately.
    ///   OnDisable → unsubscribes channel; hides panel.
    ///   Refresh() → reads HealthSO; hides panel when null; otherwise:
    ///                 • _healthBar.value = CurrentHealth / MaxHealth
    ///                 • _healthLabel.text = "current / max"
    ///                 • _criticalOverlay shown when ratio ≤ _criticalThreshold
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — event-driven via FloatGameEvent channel.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one health bar per combatant HUD slot.
    ///
    /// Scene wiring:
    ///   _healthSO          → shared HealthSO asset for this combatant.
    ///   _onHealthChanged   → FloatGameEvent fired by HealthSO on every change.
    ///   _healthBar         → Slider whose value maps to health ratio [0,1].
    ///   _healthLabel       → Text showing "current / max" integers.
    ///   _criticalOverlay   → GameObject shown when health ≤ critical threshold.
    ///   _healthPanel       → Root panel; hidden when no HealthSO is assigned.
    ///   _robotNameLabel    → Optional Text for the robot's display name.
    ///   _robotName         → Display name written to _robotNameLabel on Refresh.
    ///   _criticalThreshold → Health ratio below which critical overlay appears (default 0.25).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotHealthBarController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("HealthSO for the combatant this bar represents. Leave null to hide the panel.")]
        [SerializeField] private HealthSO _healthSO;

        // ── Inspector — Event Channel ────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("FloatGameEvent raised by HealthSO on every health change. Payload = current HP.")]
        [SerializeField] private FloatGameEvent _onHealthChanged;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI References (optional)")]
        [Tooltip("Slider showing health as a fill ratio [0, 1].")]
        [SerializeField] private Slider _healthBar;

        [Tooltip("Text label showing 'current / max' integer health values.")]
        [SerializeField] private Text _healthLabel;

        [Tooltip("Overlay GameObject activated when health ratio ≤ _criticalThreshold.")]
        [SerializeField] private GameObject _criticalOverlay;

        [Tooltip("Root panel for this health bar; hidden when no HealthSO is assigned.")]
        [SerializeField] private GameObject _healthPanel;

        [Tooltip("Optional Text label for this combatant's display name.")]
        [SerializeField] private Text _robotNameLabel;

        [Header("Settings")]
        [Tooltip("Display name written to _robotNameLabel on each Refresh.")]
        [SerializeField] private string _robotName = string.Empty;

        [Tooltip("Health ratio [0, 1] at or below which the critical overlay is shown.")]
        [SerializeField, Range(0.01f, 0.5f)] private float _criticalThreshold = 0.25f;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action<float> _healthDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _healthDelegate = OnHealthChanged;
        }

        private void OnEnable()
        {
            _onHealthChanged?.RegisterCallback(_healthDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHealthChanged?.UnregisterCallback(_healthDelegate);
            _healthPanel?.SetActive(false);
        }

        // ── Private implementation ─────────────────────────────────────────────

        private void OnHealthChanged(float currentHealth)
        {
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="HealthSO"/> state and pushes it to all UI elements.
        /// Hides the panel when no SO is assigned. Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_healthSO == null)
            {
                _healthPanel?.SetActive(false);
                _criticalOverlay?.SetActive(false);
                return;
            }

            _healthPanel?.SetActive(true);

            float maxHealth     = _healthSO.MaxHealth;
            float currentHealth = _healthSO.CurrentHealth;
            float ratio         = maxHealth > 0f
                ? Mathf.Clamp01(currentHealth / maxHealth)
                : 0f;

            if (_healthBar != null)
                _healthBar.value = ratio;

            if (_healthLabel != null)
                _healthLabel.text = string.Format(
                    "{0}/{1}",
                    Mathf.RoundToInt(currentHealth),
                    Mathf.RoundToInt(maxHealth));

            if (_criticalOverlay != null)
                _criticalOverlay.SetActive(ratio <= _criticalThreshold);

            if (_robotNameLabel != null)
                _robotNameLabel.text = _robotName ?? string.Empty;
        }

        /// <summary>The assigned <see cref="HealthSO"/>. May be null.</summary>
        public HealthSO HealthSO => _healthSO;

        /// <summary>Health ratio below which the critical overlay is shown.</summary>
        public float CriticalThreshold => _criticalThreshold;

        /// <summary>Robot display name written to the name label.</summary>
        public string RobotName => _robotName;
    }
}
