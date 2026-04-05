using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour listener for a <see cref="DamageEvent"/> channel.
    /// Wire the <c>Response</c> UnityEvent in the Inspector to react to damage hits
    /// (e.g. spawn floating damage numbers, update MatchRecord accumulators).
    /// </summary>
    public sealed class DamageEventListener : GameEventListener<DamagePayload> { }
}
