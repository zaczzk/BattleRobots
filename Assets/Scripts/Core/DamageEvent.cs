using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Typed SO event channel for damage notifications.
    /// Raised by HealthSO whenever a TakeDamage call succeeds.
    /// Listeners receive a <see cref="DamagePayload"/> with the amount and source.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/DamageEvent", order = 4)]
    public sealed class DamageEvent : GameEvent<DamagePayload> { }
}
