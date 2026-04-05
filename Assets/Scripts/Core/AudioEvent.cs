using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// SO event channel that carries an <see cref="AudioClip"/> payload.
    ///
    /// Usage — raise from any system (damage, UI, match state) without
    /// taking a direct dependency on audio components:
    ///   _audioEvent.Raise(clip);
    ///
    /// Create via: Assets ▶ BattleRobots ▶ Events ▶ AudioEvent
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/AudioEvent", order = 5)]
    public sealed class AudioEvent : GameEvent<AudioClip> { }
}
