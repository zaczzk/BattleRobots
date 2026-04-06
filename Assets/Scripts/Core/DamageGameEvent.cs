using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// SO event channel for damage signals.
    /// Raise() broadcasts a DamageInfo to every registered DamageGameEventListener.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Events ▶ DamageGameEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/DamageGameEvent", order = 4)]
    public sealed class DamageGameEvent : GameEvent<DamageInfo> { }
}
