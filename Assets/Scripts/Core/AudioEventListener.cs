using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour listener for an <see cref="AudioEvent"/> channel.
    /// Exposes a UnityEvent&lt;AudioClip&gt; response wirable in the Inspector.
    /// Typically wired to <see cref="SFXPlayer.Play"/> for one-shot sound effects.
    /// </summary>
    public sealed class AudioEventListener : GameEventListener<AudioClip> { }
}
