using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives a live in-match damage statistics HUD widget,
    /// showing total damage dealt and per-type ratio bars sourced from a
    /// <see cref="MatchStatisticsSO"/> blackboard.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action).
    ///   OnEnable  → subscribes _onStatisticsUpdated → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes _onStatisticsUpdated; hides _statsPanel.
    ///   Refresh() → reads MatchStatisticsSO; hides panel when null;
    ///               otherwise sets _totalDealtText ("Dealt: N") and updates
    ///               four optional Sliders to DamageTypeRatio values.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one stats HUD per canvas.
    ///   • No Update / FixedUpdate — fully event-driven via _onStatisticsUpdated.
    ///   • All UI refs are optional.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _statistics          → shared MatchStatisticsSO asset.
    ///   _onStatisticsUpdated → same VoidGameEvent asset assigned to
    ///                          MatchStatisticsSO._onStatisticsUpdated.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchStatisticsHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime MatchStatisticsSO blackboard. Leave null to hide the panel.")]
        [SerializeField] private MatchStatisticsSO _statistics;

        [Header("Event Channel (optional)")]
        [Tooltip("Fired by MatchStatisticsSO after every RecordDamage call. " +
                 "Assign the same asset referenced by MatchStatisticsSO._onStatisticsUpdated.")]
        [SerializeField] private VoidGameEvent _onStatisticsUpdated;

        [Header("Panel (optional)")]
        [Tooltip("Root panel GameObject. Hidden when _statistics is null.")]
        [SerializeField] private GameObject _statsPanel;

        [Header("Labels (optional)")]
        [Tooltip("Displays 'Dealt: N' where N = Mathf.RoundToInt(TotalDamageDealt).")]
        [SerializeField] private Text _totalDealtText;

        [Header("Type Ratio Bars (optional)")]
        [Tooltip("Slider.value = DamageTypeRatio(Physical) in [0, 1].")]
        [SerializeField] private Slider _physicalBar;

        [Tooltip("Slider.value = DamageTypeRatio(Energy) in [0, 1].")]
        [SerializeField] private Slider _energyBar;

        [Tooltip("Slider.value = DamageTypeRatio(Thermal) in [0, 1].")]
        [SerializeField] private Slider _thermalBar;

        [Tooltip("Slider.value = DamageTypeRatio(Shock) in [0, 1].")]
        [SerializeField] private Slider _shockBar;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onStatisticsUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onStatisticsUpdated?.UnregisterCallback(_refreshDelegate);
            _statsPanel?.SetActive(false);
        }

        // ── UI logic ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="MatchStatisticsSO"/> state and pushes it to
        /// the HUD. Hides the panel when no SO is assigned; shows it otherwise.
        /// Allocation-free — Mathf.RoundToInt and Slider.value are pure arithmetic.
        /// </summary>
        public void Refresh()
        {
            if (_statistics == null)
            {
                _statsPanel?.SetActive(false);
                return;
            }

            _statsPanel?.SetActive(true);

            if (_totalDealtText != null)
                _totalDealtText.text =
                    $"Dealt: {Mathf.RoundToInt(_statistics.TotalDamageDealt)}";

            if (_physicalBar != null)
                _physicalBar.value = _statistics.DamageTypeRatio(DamageType.Physical);

            if (_energyBar != null)
                _energyBar.value = _statistics.DamageTypeRatio(DamageType.Energy);

            if (_thermalBar != null)
                _thermalBar.value = _statistics.DamageTypeRatio(DamageType.Thermal);

            if (_shockBar != null)
                _shockBar.value = _statistics.DamageTypeRatio(DamageType.Shock);
        }
    }
}
