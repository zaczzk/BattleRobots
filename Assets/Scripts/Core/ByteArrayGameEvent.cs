using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// SO event channel carrying a raw <c>byte[]</c> payload.
    /// Used to relay serialised match-state packets through the SO event bus
    /// without coupling the network layer to game logic.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Events ▶ ByteArrayGameEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/ByteArrayGameEvent", order = 5)]
    public sealed class ByteArrayGameEvent : GameEvent<byte[]> { }
}
