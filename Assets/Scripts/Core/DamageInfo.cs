using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Value payload carried by a DamageGameEvent.
    /// Kept as a plain serializable struct so it works with UnityEvent&lt;DamageInfo&gt;
    /// and causes zero heap allocation when passed through the SO event bus.
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

        /// <summary>World-space position of the hit; used by VFX handlers to spawn effects.</summary>
        public Vector3 hitPoint;

        /// <summary>Create a DamageInfo with hitPoint defaulting to Vector3.zero.</summary>
        public DamageInfo(float amount, string sourceId = "")
        {
            this.amount   = amount;
            this.sourceId = sourceId ?? string.Empty;
            this.hitPoint = Vector3.zero;
        }

        /// <summary>Create a DamageInfo with an explicit world-space hit position.</summary>
        public DamageInfo(float amount, Vector3 hitPoint, string sourceId = "")
        {
            this.amount   = amount;
            this.hitPoint = hitPoint;
            this.sourceId = sourceId ?? string.Empty;
        }

        public override string ToString() =>
            $"DamageInfo(amount={amount:F1}, source='{sourceId}', hitPoint={hitPoint})";
    }
}
