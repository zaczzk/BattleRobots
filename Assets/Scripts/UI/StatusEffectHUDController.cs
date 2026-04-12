using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives status-effect indicators on the combat HUD (Burn / Stun / Slow icons + timers).
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   StatusEffectController (Physics) ──UpdateState()──► StatusEffectStateSO (Core)
    ///   StatusEffectStateSO raises _onEffectsChanged
    ///   ──► StatusEffectHUDController.Refresh() reads SO properties and updates UI.
    ///
    ///   This controller never references any BattleRobots.Physics type — all state
    ///   is consumed from <see cref="StatusEffectStateSO"/> via a VoidGameEvent trigger.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to any persistent Canvas GameObject in the Arena scene
    ///      (e.g., alongside CombatHUDController on the Arena HUD Canvas).
    ///   2. Assign _effectsState → the StatusEffectStateSO asset (same one assigned to
    ///      StatusEffectController._statusDisplay).
    ///   3. Assign _onEffectsChanged → the VoidGameEvent SO assigned to
    ///      StatusEffectStateSO._onEffectsChanged (NOT the controller's own event
    ///      channel, to avoid double-firing).
    ///   4. Optionally assign any UI references below; all are silently skipped if null.
    ///      • _effectsPanel       — root GO shown when AnyEffectActive, hidden otherwise.
    ///      • _burnIndicator      — icon GO enabled when Burn is active.
    ///      • _burnTimerText      — Text showing "N.Ns" while Burn is active.
    ///      • _stunIndicator      — icon GO enabled when Stun is active.
    ///      • _stunTimerText      — Text showing "N.Ns" while Stun is active.
    ///      • _slowIndicator      — icon GO enabled when Slow is active.
    ///      • _slowTimerText      — Text showing "N.Ns" while Slow is active.
    ///      • _slowFactorText     — Text showing "x0.N" slow multiplier while active.
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — no BattleRobots.Physics references.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Zero heap allocation on the event-handler hot path
    ///     (cached delegate + string.Format for optional labels).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusEffectHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("State Source")]
        [Tooltip("Runtime blackboard written by StatusEffectController. " +
                 "Required — without this the controller will early-return in Refresh().")]
        [SerializeField] private StatusEffectStateSO _effectsState;

        [Header("Event Channel — In")]
        [Tooltip("VoidGameEvent raised by StatusEffectStateSO._onEffectsChanged " +
                 "(NOT the controller's own event — assign a dedicated display event " +
                 "to the SO to avoid double-firing). " +
                 "Leave null; all indicators are hidden when no state source is assigned.")]
        [SerializeField] private VoidGameEvent _onEffectsChanged;

        [Header("Panel Root (optional)")]
        [Tooltip("Root GO shown when at least one effect is active, hidden otherwise.")]
        [SerializeField] private GameObject _effectsPanel;

        [Header("Burn Indicator (optional)")]
        [Tooltip("Icon GO — enabled while Burn is active.")]
        [SerializeField] private GameObject _burnIndicator;

        [Tooltip("Text label showing remaining Burn seconds (e.g. '2.7s').")]
        [SerializeField] private Text _burnTimerText;

        [Header("Stun Indicator (optional)")]
        [Tooltip("Icon GO — enabled while Stun is active.")]
        [SerializeField] private GameObject _stunIndicator;

        [Tooltip("Text label showing remaining Stun seconds.")]
        [SerializeField] private Text _stunTimerText;

        [Header("Slow Indicator (optional)")]
        [Tooltip("Icon GO — enabled while Slow is active.")]
        [SerializeField] private GameObject _slowIndicator;

        [Tooltip("Text label showing remaining Slow seconds.")]
        [SerializeField] private Text _slowTimerText;

        [Tooltip("Text label showing the current slow multiplier (e.g. 'x0.4').")]
        [SerializeField] private Text _slowFactorText;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshHandler;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache delegate once — RegisterCallback/UnregisterCallback compare by
            // reference, so the same object must be passed to both.
            _refreshHandler = Refresh;
        }

        private void OnEnable()
        {
            _onEffectsChanged?.RegisterCallback(_refreshHandler);
            Refresh();
        }

        private void OnDisable()
        {
            _onEffectsChanged?.UnregisterCallback(_refreshHandler);

            // Hide everything while inactive so no stale icons remain visible
            // if the component is toggled (e.g. between matches).
            HideAll();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Synchronises all indicators to the current <see cref="_effectsState"/>.
        /// Called automatically on each <see cref="_onEffectsChanged"/> event.
        /// Safe to call manually (e.g., from an editor button or integration test).
        /// </summary>
        public void Refresh()
        {
            if (_effectsState == null)
            {
                HideAll();
                return;
            }

            // Panel: visible when at least one effect is active.
            if (_effectsPanel != null)
                _effectsPanel.SetActive(_effectsState.AnyEffectActive);

            // Burn indicator.
            UpdateIndicator(
                _burnIndicator,
                _burnTimerText,
                _effectsState.IsBurnActive,
                _effectsState.BurnTimeRemaining);

            // Stun indicator.
            UpdateIndicator(
                _stunIndicator,
                _stunTimerText,
                _effectsState.IsStunActive,
                _effectsState.StunTimeRemaining);

            // Slow indicator + multiplier text.
            UpdateIndicator(
                _slowIndicator,
                _slowTimerText,
                _effectsState.IsSlowActive,
                _effectsState.SlowTimeRemaining);

            if (_slowFactorText != null)
            {
                _slowFactorText.text = _effectsState.IsSlowActive
                    ? string.Format("x{0:F1}", _effectsState.CurrentSlowFactor)
                    : string.Empty;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Sets an indicator GO active/inactive and updates its optional timer label.
        /// All params are optional — null indicators and labels are silently skipped.
        /// </summary>
        private static void UpdateIndicator(
            GameObject indicator,
            Text       timerText,
            bool       active,
            float      timeRemaining)
        {
            if (indicator != null)
                indicator.SetActive(active);

            if (timerText != null)
                timerText.text = active
                    ? string.Format("{0:F1}s", timeRemaining)
                    : string.Empty;
        }

        /// <summary>
        /// Hides all optional visual elements without modifying the state source.
        /// Called on OnDisable and when <see cref="_effectsState"/> is null.
        /// </summary>
        private void HideAll()
        {
            if (_effectsPanel    != null) _effectsPanel.SetActive(false);
            if (_burnIndicator   != null) _burnIndicator.SetActive(false);
            if (_burnTimerText   != null) _burnTimerText.text   = string.Empty;
            if (_stunIndicator   != null) _stunIndicator.SetActive(false);
            if (_stunTimerText   != null) _stunTimerText.text   = string.Empty;
            if (_slowIndicator   != null) _slowIndicator.SetActive(false);
            if (_slowTimerText   != null) _slowTimerText.text   = string.Empty;
            if (_slowFactorText  != null) _slowFactorText.text  = string.Empty;
        }
    }
}
