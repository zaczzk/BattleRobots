using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match HUD controller that displays the type-matchup effectiveness of the
    /// player's currently equipped weapon against the selected opponent.
    ///
    /// ── Computation ─────────────────────────────────────────────────────────────
    ///   1. Resolves the equipped <see cref="WeaponPartSO"/> by iterating
    ///      <see cref="PlayerLoadout.EquippedPartIds"/> and calling
    ///      <see cref="WeaponPartCatalogSO.Lookup"/> for the first matching ID.
    ///   2. Reads <see cref="OpponentProfileSO.OpponentResistance"/> and
    ///      <see cref="OpponentProfileSO.OpponentVulnerability"/> from the selected
    ///      opponent's profile (both default to neutral when null).
    ///   3. Computes:
    ///      <c>ratio = (1 − resistance) × vulnerabilityMultiplier</c>
    ///   4. Classifies the ratio via <see cref="DamageTypeEffectivenessConfig.GetOutcome"/>
    ///      and writes the outcome label + color to <c>_outcomeLabel</c>.
    ///
    /// ── Panel visibility ────────────────────────────────────────────────────────
    ///   <c>_outcomePanel</c> is shown only when all required data is present:
    ///   a resolved weapon, a selected opponent, and an effectiveness config.
    ///   It is hidden in all other cases (null loadout, no opponent, no config,
    ///   or no catalog match for any equipped ID).
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (zero-alloc after Awake).
    ///   OnEnable  → subscribes _onOpponentChanged + _onLoadoutChanged; calls Refresh.
    ///   OnDisable → unsubscribes both channels.
    ///   Refresh   → resolves weapon; updates _weaponTypeLabel; hides or shows panel.
    ///
    /// ── Architecture rules ───────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All referenced configs are Core SOs — safe cross-namespace reference.
    ///   • DisallowMultipleComponent — one matchup panel per canvas.
    ///   • No allocations inside Refresh() — IReadOnlyList scan + field reads only.
    ///
    /// Assign <c>_playerLoadout</c>, <c>_weaponCatalog</c>, <c>_selectedOpponent</c>,
    /// and <c>_effectivenessConfig</c> in the Inspector; optionally wire
    /// <c>_onOpponentChanged</c> and <c>_onLoadoutChanged</c> to keep the panel
    /// in sync as selections change.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquippedWeaponMatchupController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime loadout SO.  EquippedPartIds are iterated to find the active weapon. " +
                 "Leave null to disable — Refresh hides the panel gracefully.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Catalog that maps PartIds to WeaponPartSO assets. " +
                 "Leave null to disable — Refresh hides the panel gracefully.")]
        [SerializeField] private WeaponPartCatalogSO _weaponCatalog;

        [Tooltip("Runtime SO carrying the currently-selected opponent profile. " +
                 "The profile's OpponentResistance and OpponentVulnerability are read. " +
                 "Leave null to hide the panel (no opponent to compare against).")]
        [SerializeField] private SelectedOpponentSO _selectedOpponent;

        // ── Inspector — Config ────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("Thresholds and labels for classifying the ratio as " +
                 "Effective / Resisted / Neutral.  Leave null to hide the panel.")]
        [SerializeField] private DamageTypeEffectivenessConfig _effectivenessConfig;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised when the opponent selection changes.  Triggers Refresh(). " +
                 "Leave null to refresh only at OnEnable time.")]
        [SerializeField] private VoidGameEvent _onOpponentChanged;

        [Tooltip("Raised when the player confirms a new loadout.  Triggers Refresh(). " +
                 "Leave null to refresh only at OnEnable time.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Text label that shows the equipped weapon's DamageType outcome " +
                 "(e.g. 'EFFECTIVE!' or 'RESISTED!').  Color is also driven per-outcome.")]
        [SerializeField] private Text _outcomeLabel;

        [Tooltip("Panel root activated when weapon+opponent+config are all resolved. " +
                 "Hidden in all other states.")]
        [SerializeField] private GameObject _outcomePanel;

        [Tooltip("Text label that shows the equipped weapon's DamageType " +
                 "(e.g. 'Weapon: Energy').  Shows 'Weapon: —' when no match is found.")]
        [SerializeField] private Text _weaponTypeLabel;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onOpponentChanged?.RegisterCallback(_refreshDelegate);
            _onLoadoutChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onOpponentChanged?.UnregisterCallback(_refreshDelegate);
            _onLoadoutChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the equipped weapon, updates the weapon-type label, and either
        /// shows the outcome panel (when all data is available) or hides it.
        ///
        /// Safe to call at any time — fully null-safe; no allocation.
        /// </summary>
        public void Refresh()
        {
            WeaponPartSO weapon = ResolveWeapon();

            // ── Weapon-type label (independent of panel visibility) ────────────
            if (_weaponTypeLabel != null)
            {
                _weaponTypeLabel.text = weapon != null
                    ? $"Weapon: {weapon.WeaponDamageType}"
                    : "Weapon: \u2014";
            }

            // ── Guard: hide panel when any required data is missing ────────────
            if (weapon == null
                || _selectedOpponent == null
                || !_selectedOpponent.HasSelection
                || _effectivenessConfig == null)
            {
                _outcomePanel?.SetActive(false);
                return;
            }

            // ── Compute effectiveness ratio ────────────────────────────────────
            OpponentProfileSO profile    = _selectedOpponent.Current;
            DamageType        type       = weapon.WeaponDamageType;
            float             resistance = profile?.OpponentResistance?.GetResistance(type) ?? 0f;
            float             vuln       = profile?.OpponentVulnerability?.GetMultiplier(type) ?? 1f;
            float             ratio      = (1f - resistance) * vuln;

            // ── Classify and update outcome label ──────────────────────────────
            EffectivenessOutcome outcome = _effectivenessConfig.GetOutcome(ratio);
            if (_outcomeLabel != null)
            {
                _outcomeLabel.text  = _effectivenessConfig.GetLabel(outcome);
                _outcomeLabel.color = _effectivenessConfig.GetColor(outcome);
            }

            _outcomePanel?.SetActive(true);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Iterates <see cref="PlayerLoadout.EquippedPartIds"/> and returns the first
        /// <see cref="WeaponPartSO"/> resolved via <see cref="WeaponPartCatalogSO.Lookup"/>.
        /// Returns null when _playerLoadout or _weaponCatalog is null, or no ID matches.
        /// </summary>
        private WeaponPartSO ResolveWeapon()
        {
            if (_playerLoadout == null || _weaponCatalog == null) return null;

            IReadOnlyList<string> ids = _playerLoadout.EquippedPartIds;
            for (int i = 0; i < ids.Count; i++)
            {
                WeaponPartSO found = _weaponCatalog.Lookup(ids[i]);
                if (found != null) return found;
            }

            return null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="SelectedOpponentSO"/>. May be null.</summary>
        public SelectedOpponentSO SelectedOpponent => _selectedOpponent;

        /// <summary>The currently assigned <see cref="WeaponPartCatalogSO"/>. May be null.</summary>
        public WeaponPartCatalogSO WeaponCatalog => _weaponCatalog;

        /// <summary>The currently assigned <see cref="DamageTypeEffectivenessConfig"/>. May be null.</summary>
        public DamageTypeEffectivenessConfig EffectivenessConfig => _effectivenessConfig;
    }
}
