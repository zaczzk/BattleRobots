using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays per-type mastery progress and mastery badge
    /// visibility from <see cref="DamageTypeMasterySO"/>.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────────
    ///   For each <see cref="DamageType"/> (Physical / Energy / Thermal / Shock):
    ///   • A Text label shows either "MASTERED" or the progress percentage (e.g. "47%").
    ///   • An optional badge <c>GameObject</c> is shown when the type is mastered,
    ///     hidden otherwise.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onMasteryUnlocked → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → reads <see cref="DamageTypeMasterySO.GetProgress"/> and
    ///               <see cref="DamageTypeMasterySO.IsTypeMastered"/> per type; null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one mastery panel per canvas.
    ///   • All UI fields optional — assign only what the scene has.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _mastery             → DamageTypeMasterySO (per-type accumulators + mastery flags).
    ///   _onMasteryUnlocked   → same VoidGameEvent as DamageTypeMasterySO._onMasteryUnlocked.
    ///   _physicalText        → Text for Physical progress / mastery status.
    ///   _energyText          → Text for Energy progress / mastery status.
    ///   _thermalText         → Text for Thermal progress / mastery status.
    ///   _shockText           → Text for Shock progress / mastery status.
    ///   _physicalBadge       → Badge GO shown when Physical is mastered.
    ///   _energyBadge         → Badge GO shown when Energy is mastered.
    ///   _thermalBadge        → Badge GO shown when Thermal is mastered.
    ///   _shockBadge          → Badge GO shown when Shock is mastered.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MasteryController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime mastery SO. Provides per-type progress and mastery flags.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised by DamageTypeMasterySO when any type first reaches its threshold. " +
                 "Triggers Refresh(). Leave null to disable auto-refresh.")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        // ── Inspector — Progress Labels (optional) ────────────────────────────

        [Header("Progress Labels (optional)")]
        [SerializeField] private Text _physicalText;
        [SerializeField] private Text _energyText;
        [SerializeField] private Text _thermalText;
        [SerializeField] private Text _shockText;

        // ── Inspector — Mastery Badges (optional) ─────────────────────────────

        [Header("Mastery Badges (optional)")]
        [Tooltip("Shown when Physical damage type is mastered.")]
        [SerializeField] private GameObject _physicalBadge;

        [Tooltip("Shown when Energy damage type is mastered.")]
        [SerializeField] private GameObject _energyBadge;

        [Tooltip("Shown when Thermal damage type is mastered.")]
        [SerializeField] private GameObject _thermalBadge;

        [Tooltip("Shown when Shock damage type is mastered.")]
        [SerializeField] private GameObject _shockBadge;

        // ── Cached state ──────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMasteryUnlocked?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMasteryUnlocked?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads mastery state from <c>_mastery</c> and updates all wired labels and badges.
        /// Fully null-safe — skips any unassigned UI ref.
        /// </summary>
        public void Refresh()
        {
            RefreshType(DamageType.Physical, _physicalText, _physicalBadge);
            RefreshType(DamageType.Energy,   _energyText,   _energyBadge);
            RefreshType(DamageType.Thermal,  _thermalText,  _thermalBadge);
            RefreshType(DamageType.Shock,    _shockText,    _shockBadge);
        }

        private void RefreshType(DamageType type, Text label, GameObject badge)
        {
            bool mastered = _mastery != null && _mastery.IsTypeMastered(type);
            float progress = _mastery != null ? _mastery.GetProgress(type) : 0f;

            if (label != null)
            {
                label.text = mastered
                    ? "MASTERED"
                    : $"{Mathf.RoundToInt(progress * 100f)}%";
            }

            badge?.SetActive(mastered);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;
    }
}
