using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that computes and displays weapon type-matchup effectiveness
    /// in a pre-match advisor panel.
    ///
    /// ── Purpose ──────────────────────────────────────────────────────────────────
    ///   Reads the equipped weapon's <see cref="DamageType"/> from
    ///   <see cref="_weaponPart"/> and the selected opponent's resistance and
    ///   vulnerability configs from <see cref="_selectedOpponent"/>.Current, then
    ///   classifies the matchup as Effective / Resisted / Neutral and updates the
    ///   UI text and color accordingly.
    ///
    /// ── Computation ─────────────────────────────────────────────────────────────
    ///   combinedRatio = (1 − resistance) × vulnerabilityMultiplier
    ///   Both resistance and vulnerability default to neutral values (0 and 1
    ///   respectively) when their configs are null or no opponent is selected.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   OnEnable → Refresh().
    ///   Refresh  → compute combinedRatio using weapon DamageType + opponent configs;
    ///              classify outcome via _effectivenessConfig.GetOutcome(ratio);
    ///              update _advisorText label and color; update _weaponTypeText.
    ///
    /// ── Null-safety ─────────────────────────────────────────────────────────────
    ///   All inspector fields are optional.  Missing configs default to
    ///   resistance = 0 / vulnerability = 1 (neutral pass-through).
    ///   Missing UI Text refs are no-ops.
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All referenced configs are Core SOs — safe cross-assembly reference.
    ///   • DisallowMultipleComponent — one advisor panel per canvas.
    ///   • No allocations inside Refresh() — float arithmetic + struct enum only.
    ///
    /// Assign <c>_weaponPart</c>, <c>_selectedOpponent</c>, and
    /// <c>_effectivenessConfig</c> in the Inspector; wire the UI Text fields.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TypeMatchupAdvisorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Player's equipped weapon SO. Supplies the outgoing DamageType to evaluate against " +
                 "the selected opponent's resistance and vulnerability.")]
        [SerializeField] private WeaponPartSO _weaponPart;

        [Tooltip("Runtime SO carrying the currently-selected opponent profile. " +
                 "The profile's OpponentResistance and OpponentVulnerability configs are read. " +
                 "Leave null to evaluate against a neutral opponent (no resistance, no vulnerability).")]
        [SerializeField] private SelectedOpponentSO _selectedOpponent;

        [Header("Config")]
        [Tooltip("Thresholds and labels for classifying the matchup outcome as Effective / " +
                 "Resisted / Neutral.  Leave null to show only the weapon type name.")]
        [SerializeField] private DamageTypeEffectivenessConfig _effectivenessConfig;

        [Header("UI References (optional)")]
        [Tooltip("Text label updated with the effectiveness outcome label (e.g. \"EFFECTIVE!\"). " +
                 "Falls back to showing the weapon type name when _effectivenessConfig is null.")]
        [SerializeField] private Text _advisorText;

        [Tooltip("Secondary text label showing the outgoing DamageType name (e.g. \"Energy\").")]
        [SerializeField] private Text _weaponTypeText;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            Refresh();
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Recomputes and displays the type-matchup effectiveness for the current
        /// weapon versus the selected opponent's configs.
        ///
        /// Safe to call at any time from outside code (e.g. when the loadout or
        /// opponent selection changes).  Null inputs default to neutral values.
        /// Zero allocation — float arithmetic and struct enum only.
        /// </summary>
        public void Refresh()
        {
            DamageType weaponType = _weaponPart != null
                ? _weaponPart.WeaponDamageType
                : DamageType.Physical;

            OpponentProfileSO profile = _selectedOpponent != null && _selectedOpponent.HasSelection
                ? _selectedOpponent.Current
                : null;

            DamageResistanceConfig    resistCfg = profile?.OpponentResistance;
            DamageVulnerabilityConfig vulnCfg   = profile?.OpponentVulnerability;

            float resistance    = resistCfg != null ? resistCfg.GetResistance(weaponType) : 0f;
            float vulnerability = vulnCfg   != null ? vulnCfg.GetMultiplier(weaponType)   : 1f;
            float combinedRatio = (1f - resistance) * vulnerability;

            if (_weaponTypeText != null)
                _weaponTypeText.text = weaponType.ToString();

            if (_advisorText == null) return;

            if (_effectivenessConfig == null)
            {
                _advisorText.text = weaponType.ToString();
                return;
            }

            EffectivenessOutcome outcome = _effectivenessConfig.GetOutcome(combinedRatio);
            _advisorText.text  = _effectivenessConfig.GetLabel(outcome);
            _advisorText.color = _effectivenessConfig.GetColor(outcome);
        }

        /// <summary>
        /// Swaps the active weapon part and refreshes the advisor display.
        /// Intended for the loadout builder to update the panel reactively as the
        /// player cycles through weapon options.  Null clears the part — Refresh
        /// defaults to <see cref="DamageType.Physical"/>.
        /// </summary>
        public void SetWeaponPart(WeaponPartSO part)
        {
            _weaponPart = part;
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned weapon part SO. May be null.</summary>
        public WeaponPartSO WeaponPart => _weaponPart;

        /// <summary>The currently assigned SelectedOpponentSO. May be null.</summary>
        public SelectedOpponentSO SelectedOpponent => _selectedOpponent;

        /// <summary>The currently assigned DamageTypeEffectivenessConfig. May be null.</summary>
        public DamageTypeEffectivenessConfig EffectivenessConfig => _effectivenessConfig;
    }
}
