using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives an energy HUD widget from <see cref="EnergySystemSO"/> state.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────
    ///   OnEnable  → subscribes _onEnergyChanged → Refresh(); calls Refresh() immediately.
    ///   Refresh() → reads EnergySystemSO; hides panel when null;
    ///               otherwise sets _energyFillBar.value (EnergyRatio),
    ///               _energyLabel.text ("current/max"), and shows _energyPanel.
    ///   OnDisable → unsubscribes _onEnergyChanged.
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Delegate cached in Awake; zero allocations after initialisation.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • All fields optional and null-guarded.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Assign _energySystem → the robot's EnergySystemSO asset.
    ///   2. Assign _onEnergyChanged → the EnergySystemSO's _onEnergyChanged event SO.
    ///   3. Assign _energyPanel → the root panel GameObject (shown while active).
    ///   4. Assign _energyFillBar → a Slider component (value driven to EnergyRatio).
    ///   5. Assign _energyLabel → a Text component (shows "current/max" integers).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnergyHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO for the robot's energy pool. All reads are from this asset.")]
        [SerializeField] private EnergySystemSO _energySystem;

        [Header("Event Channels In")]
        [Tooltip("Raised by EnergySystemSO on every energy change. Drives Refresh().")]
        [SerializeField] private VoidGameEvent _onEnergyChanged;

        [Header("UI Refs")]
        [Tooltip("Root panel GameObject. Hidden when no EnergySystemSO is assigned.")]
        [SerializeField] private GameObject _energyPanel;

        [Tooltip("Slider whose .value is set to EnergySystemSO.EnergyRatio each refresh.")]
        [SerializeField] private Slider _energyFillBar;

        [Tooltip("Text label showing 'current/max' as rounded integers.")]
        [SerializeField] private Text _energyLabel;

        // ── Cached delegates ─────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Unity messages ────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onEnergyChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onEnergyChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Internal logic ────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_energySystem == null)
            {
                Hide();
                return;
            }

            if (_energyFillBar != null)
                _energyFillBar.value = _energySystem.EnergyRatio;

            if (_energyLabel != null)
                _energyLabel.text = FormatEnergy(_energySystem.CurrentEnergy, _energySystem.MaxEnergy);

            _energyPanel?.SetActive(true);
        }

        private void Hide()
        {
            _energyPanel?.SetActive(false);
        }

        // ── Formatting ────────────────────────────────────────────────────────

        /// <summary>
        /// Formats current and max energy as rounded integers separated by '/'.
        /// Example: 73.6f / 100f → "74/100".
        /// </summary>
        internal static string FormatEnergy(float current, float max)
        {
            return $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
        }
    }
}
