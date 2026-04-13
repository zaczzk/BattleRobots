using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that shows a brief "EFFECTIVE / RESISTED / NEUTRAL" banner in
    /// the HUD whenever the player takes a typed hit.
    ///
    /// ── Ratio computation ──────────────────────────────────────────────────────
    ///   combinedRatio = (1 − resistance) × vulnerabilityMultiplier
    ///   Both configs are read directly (they are the same SO assets assigned to
    ///   the player robot's DamageReceiver), so no Physics code is referenced.
    ///   If either config is null the corresponding factor defaults to 1.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches _damageDelegate (Action&lt;DamageInfo&gt;).
    ///   OnEnable  → subscribes _onDamageTaken channel.
    ///   OnDisable → unsubscribes; hides the outcome panel.
    ///   OnDamage  → computes ratio for info.damageType; queries config for label
    ///               and color; activates panel; sets _displayTimer.
    ///   Update    → calls Tick(Time.deltaTime).
    ///   Tick      → decrements _displayTimer; hides panel when it reaches 0.
    ///
    /// ── Visual contract ────────────────────────────────────────────────────────
    ///   • <c>_outcomePanel</c>  — parent GameObject toggled on/off.
    ///   • <c>_outcomeLabel</c>  — Text set to e.g. "EFFECTIVE!" and tinted.
    ///   All UI references are optional; partial wiring is fully supported.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Subscribes only to Core SO event channels (DamageGameEvent).
    ///   • DamageResistanceConfig / DamageVulnerabilityConfig are Core SOs — safe.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one effectiveness banner per canvas.
    ///
    /// Assign <c>_effectivenessConfig</c>, <c>_resistanceConfig</c>, and
    /// <c>_vulnerabilityConfig</c> to match the attacked robot's DamageReceiver.
    /// Wire <c>_onDamageTaken</c> to the player robot's DamageGameEvent channel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageTypeEffectivenessHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("Thresholds and labels for the effectiveness banner.")]
        [SerializeField] private DamageTypeEffectivenessConfig _effectivenessConfig;

        [Header("Damage Pipeline Configs (must match attacked robot's DamageReceiver)")]
        [Tooltip("Optional resistance config — leave null if the robot has none.")]
        [SerializeField] private DamageResistanceConfig _resistanceConfig;

        [Tooltip("Optional vulnerability config — leave null if the robot has none.")]
        [SerializeField] private DamageVulnerabilityConfig _vulnerabilityConfig;

        [Header("Event Channel")]
        [Tooltip("DamageGameEvent raised when the player takes a hit.")]
        [SerializeField] private DamageGameEvent _onDamageTaken;

        [Header("UI References (optional)")]
        [Tooltip("Parent GameObject of the effectiveness banner — toggled on/off.")]
        [SerializeField] private GameObject _outcomePanel;

        [Tooltip("Text label displaying the outcome name and tinted by outcome color.")]
        [SerializeField] private Text _outcomeLabel;

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
            if (_effectivenessConfig == null) return;

            // Compute combined ratio: (1 − resistance) × vulnerabilityMultiplier
            float resistance = _resistanceConfig != null
                ? _resistanceConfig.GetResistance(info.damageType)
                : 0f;
            float vulnerability = _vulnerabilityConfig != null
                ? _vulnerabilityConfig.GetMultiplier(info.damageType)
                : 1f;

            float combinedRatio = (1f - resistance) * vulnerability;

            EffectivenessOutcome outcome = _effectivenessConfig.GetOutcome(combinedRatio);
            string label = _effectivenessConfig.GetLabel(outcome);
            Color  color = _effectivenessConfig.GetColor(outcome);

            if (_outcomeLabel != null)
            {
                _outcomeLabel.text  = label;
                _outcomeLabel.color = color;
            }

            ShowPanel();
            _displayTimer = _effectivenessConfig.DisplayDuration;
        }

        /// <summary>
        /// Advances the display timer by <paramref name="deltaTime"/> seconds.
        /// Hides the outcome panel when the timer reaches zero.
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
            if (_outcomePanel != null) _outcomePanel.SetActive(true);
        }

        private void HidePanel()
        {
            if (_outcomePanel != null) _outcomePanel.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Remaining seconds the outcome banner will stay visible.
        /// Zero when hidden or no hit has been received yet.
        /// </summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>The DamageTypeEffectivenessConfig asset currently assigned (may be null).</summary>
        public DamageTypeEffectivenessConfig EffectivenessConfig => _effectivenessConfig;
    }
}
