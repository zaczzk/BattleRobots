using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject that defines the currency reward for completing a
    /// <see cref="ZoneObjectiveSO"/>. The reward scales with the number of zones the
    /// player holds at match end:
    ///
    ///   Reward = BaseReward + (zonesHeld × BonusPerZone)
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   Assign to a <see cref="ZoneObjectiveRewardApplier"/> MB.
    ///   The applier reads <c>GetReward(playerZoneCount)</c> on objective completion
    ///   and credits the player wallet.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime — all fields are read-only properties.
    ///   - Zero heap allocation on <see cref="GetReward"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneObjectiveRewardConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneObjectiveRewardConfig", order = 22)]
    public sealed class ZoneObjectiveRewardConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Reward Settings")]
        [Tooltip("Base currency reward for completing the zone objective, " +
                 "regardless of how many zones are held.")]
        [SerializeField, Min(0f)] private float _baseReward = 100f;

        [Tooltip("Additional currency added per zone held by the player at match end.")]
        [SerializeField, Min(0f)] private float _bonusPerZone = 25f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_baseReward < 0f)
            {
                Debug.LogWarning($"[ZoneObjectiveRewardConfig] '{name}': " +
                                 "_baseReward cannot be negative. Clamping to 0.");
                _baseReward = 0f;
            }
            if (_bonusPerZone < 0f)
            {
                Debug.LogWarning($"[ZoneObjectiveRewardConfig] '{name}': " +
                                 "_bonusPerZone cannot be negative. Clamping to 0.");
                _bonusPerZone = 0f;
            }
        }
#endif

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Base currency reward before the per-zone bonus is applied.</summary>
        public float BaseReward => _baseReward;

        /// <summary>Currency bonus added per zone held at match end.</summary>
        public float BonusPerZone => _bonusPerZone;

        /// <summary>
        /// Short human-readable label for the base reward (e.g., "100 credits").
        /// Useful for notification banners and tooltips.
        /// </summary>
        public string RewardLabel => $"{Mathf.RoundToInt(_baseReward)} credits";

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the total currency reward for <paramref name="zonesHeld"/> zones.
        /// Negative zone counts are treated as zero. Result is clamped to ≥ 0.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        /// <param name="zonesHeld">Number of zones the player held at match end.</param>
        /// <returns>Total reward in currency units (always ≥ 0).</returns>
        public float GetReward(int zonesHeld)
        {
            float raw = _baseReward + Mathf.Max(0, zonesHeld) * _bonusPerZone;
            return Mathf.Max(0f, raw);
        }
    }
}
