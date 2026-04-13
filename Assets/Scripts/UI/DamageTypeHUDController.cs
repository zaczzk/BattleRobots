using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that shows a brief damage-type indicator in the HUD whenever
    /// the player takes a typed hit.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches _damageDelegate (Action&lt;DamageInfo&gt;).
    ///   OnEnable  → subscribes _onDamageTaken channel.
    ///   OnDisable → unsubscribes; hides the indicator panel.
    ///   OnDamage  → reads info.damageType; queries DamageTypeIconConfig for color
    ///               and label; activates panel; sets _displayTimer.
    ///   Update    → ticks the display timer; deactivates panel when it expires.
    ///
    /// ── Visual contract ────────────────────────────────────────────────────────
    ///   • <c>_indicatorPanel</c>   — parent GameObject toggled on/off.
    ///   • <c>_typeLabel</c>        — Text set to e.g. "THERMAL", "ENERGY".
    ///   • <c>_typeIcon</c>         — Image whose color is tinted per damage type.
    ///   All UI references are optional; partial wiring is fully supported.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Subscribes only to Core SO event channels (DamageGameEvent).
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one damage-type indicator per canvas.
    ///
    /// Assign <c>_iconConfig</c> to the shared DamageTypeIconConfig asset.
    /// Wire <c>_onDamageTaken</c> to the player robot's DamageGameEvent channel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageTypeHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("Maps DamageType to color and label. Leave null to disable display.")]
        [SerializeField] private DamageTypeIconConfig _iconConfig;

        [Header("Event Channel")]
        [Tooltip("DamageGameEvent raised when the player takes a hit.")]
        [SerializeField] private DamageGameEvent _onDamageTaken;

        [Header("UI References (optional)")]
        [Tooltip("Parent GameObject of the damage-type indicator — toggled on/off.")]
        [SerializeField] private GameObject _indicatorPanel;

        [Tooltip("Text label displaying the damage type name (e.g. 'THERMAL').")]
        [SerializeField] private Text _typeLabel;

        [Tooltip("Image element whose color is tinted to match the damage type.")]
        [SerializeField] private Image _typeIcon;

        // ── Private state ─────────────────────────────────────────────────────

        private Action<DamageInfo> _damageDelegate;
        private float              _displayTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _damageDelegate = OnDamageTaken;
        }

        private void OnEnable()
        {
            _onDamageTaken?.RegisterCallback(_damageDelegate);
        }

        private void OnDisable()
        {
            _onDamageTaken?.UnregisterCallback(_damageDelegate);
            HidePanel();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void OnDamageTaken(DamageInfo info)
        {
            if (_iconConfig == null) return;

            Color  color = _iconConfig.GetColor(info.damageType);
            string label = _iconConfig.GetLabel(info.damageType);

            if (_typeLabel != null) _typeLabel.text  = label;
            if (_typeIcon  != null) _typeIcon.color  = color;

            ShowPanel();
            _displayTimer = _iconConfig.DisplayDuration;
        }

        /// <summary>
        /// Advances the display timer by <paramref name="deltaTime"/> seconds.
        /// Hides the indicator panel when the timer reaches zero.
        /// Driven by <see cref="Update"/>; can also be called directly in tests.
        /// Zero allocation — value-type arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_displayTimer <= 0f) return;

            _displayTimer -= deltaTime;
            if (_displayTimer <= 0f)
            {
                _displayTimer = 0f;
                HidePanel();
            }
        }

        private void ShowPanel()
        {
            if (_indicatorPanel != null) _indicatorPanel.SetActive(true);
        }

        private void HidePanel()
        {
            if (_indicatorPanel != null) _indicatorPanel.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Remaining seconds the indicator will stay visible.
        /// Zero when the panel is hidden or no hit has been received.
        /// </summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>The DamageTypeIconConfig asset currently assigned (may be null).</summary>
        public DamageTypeIconConfig IconConfig => _iconConfig;
    }
}
