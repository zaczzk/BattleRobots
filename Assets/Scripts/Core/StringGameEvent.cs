using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// SO event channel that broadcasts a <see cref="string"/> payload to all registered
    /// <see cref="StringGameEventListener"/> components.
    ///
    /// Primary use-case: surfacing network failure reasons (room full, wrong password,
    /// room not found) from <see cref="NetworkEventBridge"/> to <see cref="BattleRobots.UI.JoinFailureUI"/>
    /// without creating a compile-time dependency between the layers.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Events ▶ StringGameEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/StringGameEvent", order = 4)]
    public sealed class StringGameEvent : GameEvent<string> { }
}
