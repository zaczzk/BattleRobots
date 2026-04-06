namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour listener for a DamageGameEvent channel.
    /// Wires the SO event → a UnityEvent&lt;DamageInfo&gt; so designers can
    /// connect damage responses in the Inspector without code coupling.
    /// </summary>
    public sealed class DamageGameEventListener : GameEventListener<DamageInfo> { }
}
