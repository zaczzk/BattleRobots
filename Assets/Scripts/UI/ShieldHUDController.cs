using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI controller that displays a shield HP bar for a single robot.
    ///
    /// Subscribes to a <see cref="FloatGameEvent"/> (the ShieldSO's _onShieldChanged
    /// channel) so the fill updates reactively without polling. The optional shield
    /// panel GameObject is hidden when shield HP reaches zero and shown when the
    /// shield recharges above zero.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to a Canvas GameObject in the Arena scene (e.g., alongside
    ///      CombatHUDController on the Arena HUD Canvas).
    ///   2. Assign _shield → the ShieldSO for the robot you want to display.
    ///   3. Assign _onShieldChanged → the FloatGameEvent SO wired to
    ///      ShieldSO._onShieldChanged (same asset used inside ShieldSO).
    ///   4. Optionally assign:
    ///      • _shieldBarFill — an Image component (Filled mode) for the HP bar.
    ///      • _shieldPanel   — root GameObject of the shield bar group; auto-hidden
    ///                         when shield is empty, shown when active.
    ///
    /// ARCHITECTURE: BattleRobots.UI — no Physics references.
    /// </summary>
    public sealed class ShieldHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO for the robot whose shield this HUD displays.")]
        [SerializeField] private ShieldSO _shield;

        [Header("Event Channels")]
        [Tooltip("FloatGameEvent raised by ShieldSO whenever shield HP changes.")]
        [SerializeField] private FloatGameEvent _onShieldChanged;

        [Header("UI References (optional)")]
        [Tooltip("Image component (Filled mode) used as the shield HP bar.")]
        [SerializeField] private Image _shieldBarFill;

        [Tooltip("Root panel GameObject shown/hidden based on shield active state.")]
        [SerializeField] private GameObject _shieldPanel;

        // ── Private state ─────────────────────────────────────────────────────

        private Action<float> _shieldChangedHandler;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache delegate once to avoid alloc on every OnEnable/OnDisable.
            _shieldChangedHandler = OnShieldChanged;
        }

        private void OnEnable()
        {
            _onShieldChanged?.RegisterCallback(_shieldChangedHandler);
            Refresh();
        }

        private void OnDisable()
        {
            _onShieldChanged?.UnregisterCallback(_shieldChangedHandler);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnShieldChanged(float currentHP)
        {
            UpdateVisuals(currentHP);
        }

        /// <summary>Syncs visuals to the current ShieldSO state without waiting for an event.</summary>
        private void Refresh()
        {
            if (_shield == null) return;
            UpdateVisuals(_shield.CurrentHP);
        }

        private void UpdateVisuals(float currentHP)
        {
            float fill = (_shield != null && _shield.MaxHP > 0f)
                ? Mathf.Clamp01(currentHP / _shield.MaxHP)
                : 0f;

            if (_shieldBarFill != null)
                _shieldBarFill.fillAmount = fill;

            if (_shieldPanel != null)
                _shieldPanel.SetActive(currentHP > 0f);
        }
    }
}
