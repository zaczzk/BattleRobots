using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configuration SO that pairs a <see cref="HazardZoneSO"/> with a
    /// <see cref="StatusEffectSO"/>, declaring which status effect a hazard zone
    /// should inflict on robots per damage tick.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   Assign this asset to <c>HazardZoneController._statusEffectConfig</c>.
    ///   On each damage tick, the controller calls
    ///   <c>dr.TriggerStatusEffect(config.StatusEffect)</c>, layering the status
    ///   effect on top of the per-tick damage already dealt by the zone.
    ///
    ///   Example: a Lava zone with <see cref="StatusEffectType.Burn"/> applies
    ///   both instant tick damage (from <see cref="HazardZoneSO.DamagePerTick"/>)
    ///   and a Burn DoT effect that persists after the robot exits the zone.
    ///
    /// ── Design notes ──────────────────────────────────────────────────────────
    ///   • The <see cref="HazardZone"/> reference is informational (for editor
    ///     clarity and asset organisation) — <see cref="BattleRobots.Physics.HazardZoneController"/>
    ///     reads damage parameters from its own <c>_config</c> (<see cref="HazardZoneSO"/>) field.
    ///   • <see cref="StatusEffect"/> may be null; the controller null-guards before
    ///     calling <c>TriggerStatusEffect</c>.
    ///   • Immutable at runtime — all fields are accessed through read-only properties.
    ///
    /// ── Create via ────────────────────────────────────────────────────────────
    ///   Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ HazardZoneStatusEffectConfig
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Arena/HazardZoneStatusEffectConfig",
        fileName = "New HazardZoneStatusEffectConfig",
        order = 11)]
    public sealed class HazardZoneStatusEffectConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Paired Hazard Zone")]
        [Tooltip("The HazardZoneSO this config is associated with. " +
                 "Informational only — HazardZoneController reads damage parameters " +
                 "from its own _config field rather than from here.")]
        [SerializeField] private HazardZoneSO _hazardZone;

        [Header("Status Effect Per Tick")]
        [Tooltip("StatusEffectSO applied to each robot on every hazard damage tick. " +
                 "Leave null to add no status effect (HazardZoneController null-guards). " +
                 "The stacking rule in StatusEffectController prevents spam-refreshing " +
                 "short effects — only the longest remaining duration wins.")]
        [SerializeField] private StatusEffectSO _statusEffect;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// The <see cref="HazardZoneSO"/> this config is associated with.
        /// May be null if left unassigned in the Inspector.
        /// </summary>
        public HazardZoneSO HazardZone => _hazardZone;

        /// <summary>
        /// The status effect applied to a robot each time a hazard damage tick fires.
        /// May be null — <see cref="BattleRobots.Physics.HazardZoneController"/>
        /// null-guards before calling <c>TriggerStatusEffect</c>.
        /// </summary>
        public StatusEffectSO StatusEffect => _statusEffect;
    }
}
