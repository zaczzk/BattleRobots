namespace BattleRobots.Core
{
    /// <summary>
    /// Inspector-wirable listener for <see cref="AudioGameEvent"/> channels.
    /// Attach to any GameObject, assign the AudioGameEvent SO and a UnityEvent&lt;AudioClipSO&gt;
    /// response (e.g. AudioManager.PlayClip) to play sounds via the SO bus without code.
    /// </summary>
    public sealed class AudioGameEventListener : GameEventListener<AudioClipSO> { }
}
