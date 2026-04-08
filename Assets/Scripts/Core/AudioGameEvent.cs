using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Typed SO event channel that carries an <see cref="AudioClipSO"/> payload.
    /// Raise this to instruct the AudioManager to play a specific clip.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Events ▶ AudioGameEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/AudioGameEvent", order = 5)]
    public sealed class AudioGameEvent : GameEvent<AudioClipSO> { }
}
