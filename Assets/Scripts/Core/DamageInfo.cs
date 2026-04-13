using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Value payload carried by a DamageGameEvent.
    /// Kept as a plain serializable struct so it works with UnityEvent&lt;DamageInfo&gt;
    /// and causes zero heap allocation when passed through the SO event bus.
    ///
    /// ── Status effect delivery ─────────────────────────────────────────────────
    ///   Optionally carry a <see cref="StatusEffectSO"/> reference to trigger a
    ///   Burn / Stun / Slow effect on the target simultaneously with raw damage.
    ///   The <see cref="DamageReceiver"/> reads this field in
    ///   <c>TakeDamage(DamageInfo)</c> and delegates to its optional
    ///   <c>StatusEffectController</c>. Null (default) means no status effect.
    /// </summary>
    [Serializable]
    public struct DamageInfo
    {
        /// <summary>Raw damage to apply (must be &gt; 0).</summary>
        [Min(0f)]
        public float amount;

        /// <summary>
        /// Optional identifier of the damage source robot / part (empty = environment).
        /// Uses a string ID rather than a GameObject reference to avoid cross-SO coupling.
        /// </summary>
        public string sourceId;

        /// <summary>
        /// World-space point where the hit occurred. Used by VFX handlers to spawn
        /// impact particles at the correct location. Defaults to Vector3.zero.
        /// </summary>
        public Vector3 hitPoint;

        /// <summary>
        /// Optional status effect applied to the target alongside raw damage.
        /// When non-null, <see cref="DamageReceiver.TakeDamage(DamageInfo)"/> routes
        /// this to the target's <c>StatusEffectController.ApplyEffect()</c>.
        /// Null (default) means no status effect — backwards-compatible with all
        /// existing callers that use the three-argument constructor.
        /// </summary>
        public StatusEffectSO statusEffect;

        /// <summary>
        /// Elemental type of the incoming damage.
        /// Used by <see cref="DamageResistanceConfig.ApplyResistance"/> to select the
        /// correct resistance fraction before damage is applied to <see cref="HealthSO"/>.
        /// Defaults to <see cref="DamageType.Physical"/> for all legacy callers that
        /// do not specify a type — fully backward-compatible.
        /// </summary>
        public DamageType damageType;

        public DamageInfo(float amount, string sourceId = "", Vector3 hitPoint = default(Vector3),
                          StatusEffectSO statusEffect = null,
                          DamageType damageType = DamageType.Physical)
        {
            this.amount       = amount;
            this.sourceId     = sourceId ?? string.Empty;
            this.hitPoint     = hitPoint;
            this.statusEffect = statusEffect;
            this.damageType   = damageType;
        }

        public override string ToString() =>
            $"DamageInfo(amount={amount:F1}, source='{sourceId}', hitPoint={hitPoint}" +
            $", type={damageType}" +
            (statusEffect != null ? $", effect={statusEffect.DisplayName}" : "") + ")";
    }
}
