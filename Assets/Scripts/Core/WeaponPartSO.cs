using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data SO that defines a weapon part's elemental damage type and base damage output.
    ///
    /// ── Purpose ──────────────────────────────────────────────────────────────────
    ///   Bridges the shop's <see cref="PartDefinition"/> system to the combat
    ///   type system.  When equipped, the <see cref="WeaponPartSO"/> supplies the
    ///   <see cref="DamageType"/> stamped on outgoing <see cref="DamageInfo"/>
    ///   payloads by <see cref="BattleRobots.Physics.WeaponAttachmentController"/>,
    ///   enabling type-based <see cref="DamageResistanceConfig"/> /
    ///   <see cref="DamageVulnerabilityConfig"/> interactions on the target.
    ///
    /// ── Optional PartDefinition link ─────────────────────────────────────────────
    ///   <see cref="_partDefinition"/> references the corresponding shop
    ///   <see cref="PartDefinition"/> SO for display-name fallback and cost queries.
    ///   Leave null for standalone weapon configs.
    ///
    /// ── DisplayName fallback chain ───────────────────────────────────────────────
    ///   1. Returns <c>_displayName</c> when explicitly set (non-empty).
    ///   2. Falls back to <c>_partDefinition.DisplayName</c> when a PartDefinition
    ///      is assigned.
    ///   3. Falls back to the SO asset name (<c>name</c>) as a last resort.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All properties read-only; asset is immutable at runtime.
    ///   - Zero allocation on the hot path: property reads only.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ WeaponPartSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/WeaponPartSO")]
    public sealed class WeaponPartSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Player-facing name for this weapon config. Falls back to PartDefinition.DisplayName " +
                 "when empty and a PartDefinition is assigned; otherwise falls back to the SO asset name.")]
        [SerializeField] private string _displayName = "";

        [Tooltip("Optional flavour description shown in advisor panels.")]
        [SerializeField, TextArea(2, 4)] private string _description = "";

        [Header("Damage")]
        [Tooltip("Elemental type of outgoing damage. Applied to DamageInfo.damageType on each fire.")]
        [SerializeField] private DamageType _damageType = DamageType.Physical;

        [Tooltip("Base damage dealt per hit. WeaponAttachmentController uses this as DamageInfo.amount.")]
        [SerializeField, Min(0.1f)] private float _baseDamage = 10f;

        [Header("Part Link (optional)")]
        [Tooltip("Shop PartDefinition this weapon config extends. " +
                 "Provides DisplayName fallback and links to economy/inventory systems. " +
                 "Leave null for standalone weapon configs.")]
        [SerializeField] private PartDefinition _partDefinition;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Elemental type of outgoing damage. Defaults to Physical.</summary>
        public DamageType WeaponDamageType => _damageType;

        /// <summary>Base damage per hit. Minimum 0.1.</summary>
        public float BaseDamage => _baseDamage;

        /// <summary>
        /// Player-facing name.
        /// Returns <c>_displayName</c> when explicitly set; falls back to
        /// <c>PartDefinition.DisplayName</c> when a PartDefinition is assigned;
        /// otherwise returns the SO asset name.
        /// Zero allocation — string references only, no concatenation.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_displayName)) return _displayName;
                if (_partDefinition != null) return _partDefinition.DisplayName;
                return name;
            }
        }

        /// <summary>Optional flavour description. May be an empty string.</summary>
        public string Description => _description;

        /// <summary>Optional PartDefinition link. May be null for standalone weapon configs.</summary>
        public PartDefinition PartDefinition => _partDefinition;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_baseDamage < 0.1f)
            {
                _baseDamage = 0.1f;
                Debug.LogWarning($"[WeaponPartSO] '{name}': _baseDamage clamped to 0.1.");
            }
        }
#endif
    }
}
