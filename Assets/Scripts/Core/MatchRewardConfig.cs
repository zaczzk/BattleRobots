using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// SO that centralises match-reward and round-timing configuration so that
    /// balance tuning can be done from a single asset rather than hunting through
    /// per-scene MatchManager inspector fields.
    ///
    /// ── Authoring workflow ─────────────────────────────────────────────────────
    ///   Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchRewardConfig
    ///   Create one global instance and assign it to <see cref="MatchManager"/>
    ///   <c>_rewardConfig</c> to override that component's per-field inspector values.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO asset is immutable at runtime (read-only properties, no public setters).
    ///   - <see cref="MatchManager"/> reads these values at match-start and match-end.
    ///   - If <c>_rewardConfig</c> is left null on MatchManager the per-component
    ///     inspector fields are used as a fallback — backwards-compatible; no scene
    ///     changes are required.
    ///
    /// ── Design constraints (checked in OnValidate) ─────────────────────────────
    ///   • <see cref="BaseWinReward"/>     ≥ 0
    ///   • <see cref="ConsolationReward"/> ≥ 0
    ///   • <see cref="ConsolationReward"/> ≤ <see cref="BaseWinReward"/>
    ///     (consolation must not exceed the win reward)
    ///   • <see cref="RoundDuration"/>     ≥ 10 s (matches the [Min(10f)] guard on MatchManager)
    /// </summary>
    [CreateAssetMenu(fileName = "MatchRewardConfig",
                     menuName  = "BattleRobots/Core/MatchRewardConfig")]
    public sealed class MatchRewardConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Reward Settings")]
        [Tooltip("Currency awarded to the player for winning a match. Must be ≥ 0.")]
        [SerializeField, Min(0)] private int _baseWinReward = 200;

        [Tooltip("Consolation currency awarded even on a loss. " +
                 "Must be ≥ 0 and ≤ BaseWinReward so losing always rewards less than winning.")]
        [SerializeField, Min(0)] private int _consolationReward = 50;

        [Header("Timing")]
        [Tooltip("Maximum duration of one round in seconds. Must be ≥ 10.")]
        [SerializeField, Min(10f)] private float _roundDuration = 120f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Currency awarded to the winning player (≥ 0).</summary>
        public int   BaseWinReward     => _baseWinReward;

        /// <summary>Consolation currency awarded even on a loss (≥ 0).</summary>
        public int   ConsolationReward => _consolationReward;

        /// <summary>Maximum round length in seconds (≥ 10).</summary>
        public float RoundDuration     => _roundDuration;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_baseWinReward < 0)
            {
                _baseWinReward = 0;
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.LogWarning("[MatchRewardConfig] BaseWinReward was negative — clamped to 0.");
            }

            if (_consolationReward < 0)
            {
                _consolationReward = 0;
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.LogWarning("[MatchRewardConfig] ConsolationReward was negative — clamped to 0.");
            }

            if (_consolationReward > _baseWinReward)
                Debug.LogWarning("[MatchRewardConfig] ConsolationReward exceeds BaseWinReward — " +
                                 "consider reducing it so losing rewards less than winning.");

            if (_roundDuration < 10f)
            {
                _roundDuration = 10f;
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.LogWarning("[MatchRewardConfig] RoundDuration was below minimum — clamped to 10 s.");
            }
        }
#endif
    }
}
