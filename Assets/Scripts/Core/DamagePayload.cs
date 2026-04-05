using System;

namespace BattleRobots.Core
{
    /// <summary>
    /// Value passed through the DamageEvent channel whenever a robot takes a hit.
    /// Carries enough data for MatchRecord accumulation and floating-damage UI.
    /// </summary>
    [Serializable]
    public struct DamagePayload
    {
        /// <summary>Raw damage amount applied to the target.</summary>
        public float amount;

        /// <summary>
        /// Identifier of the entity that dealt the damage, e.g. a robot's instanceId
        /// or a named weapon slot.  Empty string means source is unknown / environmental.
        /// </summary>
        public string sourceId;
    }
}
