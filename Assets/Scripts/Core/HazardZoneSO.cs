using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Categorises the nature of an arena hazard zone.
    /// Used by <see cref="HazardZoneSO"/> for display and audio/VFX selection.
    /// </summary>
    public enum HazardZoneType
    {
        Lava,
        Electric,
        Spikes,
        Acid,
    }

    /// <summary>
    /// Immutable configuration ScriptableObject for an arena hazard zone.
    ///
    /// Paired with a <see cref="BattleRobots.Physics.HazardZoneController"/> MonoBehaviour
    /// that owns a trigger collider; any robot with a DamageReceiver that enters the zone
    /// receives <see cref="DamagePerTick"/> damage every <see cref="TickInterval"/> seconds.
    ///
    /// ── Design notes ──────────────────────────────────────────────────────────
    ///   • Assets are immutable at runtime — mutate nothing here; all state lives
    ///     in HazardZoneController.
    ///   • DamageSourceId uses an empty-string fallback ("Environment") so
    ///     MatchStatisticsSO can attribute environment damage separately from
    ///     robot-on-robot damage.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Arena ▶ HazardZone.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/HazardZone", order = 10)]
    public sealed class HazardZoneSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Hazard Type")]
        [Tooltip("Categorises this zone — drives display name, audio cue selection, and VFX. " +
                 "Does not alter damage mechanics; adjust DamagePerTick / TickInterval for balance.")]
        [SerializeField] private HazardZoneType _hazardType = HazardZoneType.Lava;

        [Header("Damage")]
        [Tooltip("Damage dealt to a robot each tick while it remains inside the zone.")]
        [SerializeField, Min(0.1f)] private float _damagePerTick = 5f;

        [Tooltip("Seconds between damage ticks. Shorter intervals feel more immediate; " +
                 "longer intervals give robots time to escape before the next tick.")]
        [SerializeField, Min(0.1f)] private float _tickInterval = 1f;

        [Tooltip("Source identifier embedded in the DamageInfo payload. " +
                 "Use a consistent string (e.g. \"Environment\") so MatchStatisticsSO " +
                 "can attribute hazard damage separately from robot-on-robot hits. " +
                 "Defaults to \"Environment\".")]
        [SerializeField] private string _damageSourceId = "Environment";

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Category of hazard — for display and VFX selection only.</summary>
        public HazardZoneType HazardType => _hazardType;

        /// <summary>Damage per tick while a robot is inside the zone.</summary>
        public float DamagePerTick => _damagePerTick;

        /// <summary>Seconds between damage ticks.</summary>
        public float TickInterval => _tickInterval;

        /// <summary>
        /// Source string embedded in the DamageInfo payload.
        /// Falls back to "Environment" when the serialised field is null or empty.
        /// </summary>
        public string DamageSourceId =>
            string.IsNullOrEmpty(_damageSourceId) ? "Environment" : _damageSourceId;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_damageSourceId))
                Debug.LogWarning($"[HazardZoneSO] '{name}': _damageSourceId is empty — " +
                                 "will fall back to \"Environment\" at runtime.");
        }
#endif
    }
}
