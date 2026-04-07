using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour listener for a <see cref="StringGameEvent"/> channel.
    ///
    /// Usage (Inspector):
    ///   1. Attach to any GameObject in the arena or UI scene.
    ///   2. Assign a <see cref="StringGameEvent"/> asset to the <c>Event</c> field.
    ///   3. Wire the <c>Response</c> UnityEvent to a handler such as
    ///      <see cref="BattleRobots.UI.JoinFailureUI.ShowFailure"/>.
    ///
    /// The listener auto-registers/unregisters in OnEnable/OnDisable — no manual
    /// lifecycle management required.
    /// </summary>
    public sealed class StringGameEventListener : GameEventListener<string> { }
}
