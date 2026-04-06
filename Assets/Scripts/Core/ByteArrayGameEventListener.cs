using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour listener for a <see cref="ByteArrayGameEvent"/> channel.
    /// Wires the SO event to a <c>UnityEvent&lt;byte[]&gt;</c> so any MonoBehaviour
    /// method accepting a <c>byte[]</c> can be wired in the Inspector.
    ///
    /// Typical usage: add alongside <see cref="NetworkMatchSync"/>, assign the
    /// <c>ByteArrayGameEvent</c> channel, and wire the UnityEvent to
    /// <c>NetworkMatchSync.OnMatchStateReceived(byte[])</c>.
    /// </summary>
    public sealed class ByteArrayGameEventListener : GameEventListener<byte[]> { }
}
