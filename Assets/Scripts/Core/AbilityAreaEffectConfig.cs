using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data asset that configures an ability's area-of-effect explosion:
    /// radius, flat damage dealt to every target within range, an optional
    /// <see cref="StatusEffectSO"/> applied alongside the damage, and an optional
    /// event channel raised once after all targets are processed.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────
    ///   Assign to <see cref="BattleRobots.Physics.AbilityAreaEffectController"/>
    ///   alongside a <see cref="VoidGameEvent"/> that fires when the activating
    ///   <see cref="BattleRobots.Physics.AbilityController"/> succeeds.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core only — no Physics / UI namespace references.
    ///   • Immutable at runtime — all fields accessed via read-only properties.
    ///   • Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ AbilityAreaEffectConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/AbilityAreaEffectConfig",
                     fileName = "New AbilityAreaEffectConfig")]
    public sealed class AbilityAreaEffectConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Area of Effect")]
        [Tooltip("Radius of the sphere overlap used to find targets (world-space units). " +
                 "Minimum 0.1 to prevent a zero-radius query.")]
        [SerializeField, Min(0.1f)] private float _radius = 3f;

        [Header("Damage")]
        [Tooltip("Flat damage applied to every DamageReceiver found within the radius. " +
                 "0 is valid — pure status-effect AoE with no direct damage.")]
        [SerializeField, Min(0f)] private float _damage = 15f;

        [Tooltip("Identifier written to DamageInfo.sourceId so VFX/audio handlers and " +
                 "analytics can distinguish ability-AoE hits from other sources. " +
                 "Defaults to \"Ability\". Leave non-empty.")]
        [SerializeField] private string _damageSourceId = "Ability";

        [Header("Status Effect (optional)")]
        [Tooltip("When assigned, every target that takes AoE damage also receives this " +
                 "status effect via DamageReceiver.TriggerStatusEffect(). " +
                 "Leave null for a damage-only AoE.")]
        [SerializeField] private StatusEffectSO _statusEffect;

        [Header("Event Channels — Out (optional)")]
        [Tooltip("Raised once after all targets within the radius have been processed. " +
                 "Wire to audio or VFX systems. Leave null to skip.")]
        [SerializeField] private VoidGameEvent _onEffectTriggered;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Sphere overlap radius (world units). Minimum 0.1.</summary>
        public float Radius => _radius;

        /// <summary>Flat damage dealt to each target. 0 = status-only AoE.</summary>
        public float Damage => _damage;

        /// <summary>
        /// Source identifier written into every <see cref="DamageInfo"/> payload.
        /// Falls back to "Ability" at runtime if null or whitespace.
        /// </summary>
        public string DamageSourceId =>
            string.IsNullOrWhiteSpace(_damageSourceId) ? "Ability" : _damageSourceId;

        /// <summary>Optional status effect applied to each hit target. Null = none.</summary>
        public StatusEffectSO StatusEffect => _statusEffect;

        /// <summary>
        /// Fires <see cref="_onEffectTriggered"/> if the channel is assigned.
        /// Null-safe — no-op when no channel is wired.
        /// </summary>
        public void RaiseEffectTriggered() => _onEffectTriggered?.Raise();

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _radius = Mathf.Max(0.1f, _radius);
            _damage = Mathf.Max(0f,   _damage);

            if (string.IsNullOrWhiteSpace(_damageSourceId))
                Debug.LogWarning("[AbilityAreaEffectConfig] DamageSourceId is empty — " +
                                 "DamageInfo.sourceId will fall back to \"Ability\" at runtime.",
                                 this);
        }
#endif
    }
}
