using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject that stores per-zone capture cooldown durations,
    /// allowing each <see cref="ZoneTimerSO"/> in the arena to use a different
    /// cooldown without requiring separate inspector wiring per zone.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   Assign to a <see cref="ZoneTimerExtensionController"/>. The controller
    ///   calls <see cref="GetCooldownDuration"/> for each index and applies the
    ///   result via <c>ZoneTimerSO.SetCooldownDuration</c> on <c>OnEnable</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime — all fields are read-only properties.
    ///   - Out-of-range indices fall back to <see cref="DefaultCooldown"/>.
    ///   - Zero heap allocation on <see cref="GetCooldownDuration"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneTimerExtensionConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneTimerExtensionConfig", order = 23)]
    public sealed class ZoneTimerExtensionConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Per-Zone Cooldown Durations")]
        [Tooltip("Cooldown duration (seconds) for each zone, indexed to match the " +
                 "ZoneTimerSO array in ZoneTimerExtensionController. " +
                 "Out-of-range indices fall back to DefaultCooldown.")]
        [SerializeField] private float[] _cooldownDurations;

        [Header("Fallback")]
        [Tooltip("Cooldown duration used when an index is out of range.")]
        [SerializeField, Min(0.1f)] private float _defaultCooldown = 5f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_cooldownDurations == null || _cooldownDurations.Length == 0)
                Debug.LogWarning($"[ZoneTimerExtensionConfig] '{name}' has no " +
                                 "cooldown durations configured.");
        }
#endif

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Fallback cooldown duration used when an index is out of range.
        /// </summary>
        public float DefaultCooldown => _defaultCooldown;

        /// <summary>
        /// Number of per-zone duration entries defined in the config.
        /// </summary>
        public int EntryCount => _cooldownDurations?.Length ?? 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the cooldown duration for zone at <paramref name="index"/>.
        /// Falls back to <see cref="DefaultCooldown"/> when the array is null,
        /// empty, or the index is out of range. Result is clamped to ≥ 0.1 s.
        /// Zero allocation — array index lookup only.
        /// </summary>
        public float GetCooldownDuration(int index)
        {
            if (_cooldownDurations == null || index < 0 || index >= _cooldownDurations.Length)
                return _defaultCooldown;

            return Mathf.Max(0.1f, _cooldownDurations[index]);
        }
    }
}
