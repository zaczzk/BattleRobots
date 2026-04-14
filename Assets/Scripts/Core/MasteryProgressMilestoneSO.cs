using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data SO defining per-damage-type milestone thresholds used to track incremental
    /// mastery progress in the career stats screen.
    ///
    /// ── Milestone mechanic ───────────────────────────────────────────────────────
    ///   Each damage type (Physical / Energy / Thermal / Shock) has an ordered array
    ///   of ascending damage thresholds.  As the player's cumulative damage accumulation
    ///   in <see cref="DamageTypeMasterySO"/> grows, they clear milestones sequentially.
    ///
    ///   Example Physical milestones: [500, 1000, 5000]
    ///   • At 300 damage: 0 / 3 cleared. Next = 500.  Progress ≈ 60 %.
    ///   • At 800 damage: 1 / 3 cleared. Next = 1000. Progress = 30 % (800−500)/(1000−500).
    ///   • At 5000 damage: 3 / 3 cleared. Progress = 1.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO asset is immutable at runtime — all methods are read-only.
    ///   - Milestone arrays are sorted ascending by OnValidate.
    ///   - All public API methods are zero-alloc (linear scans over value-type arrays).
    ///   - Null arrays treated as empty (0 milestones).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ MasteryProgressMilestone.
    /// Assign to <see cref="BattleRobots.UI.MasteryProgressMilestoneController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/MasteryProgressMilestone",
        fileName = "MasteryProgressMilestoneSO")]
    public sealed class MasteryProgressMilestoneSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Milestone Thresholds (ascending)")]
        [Tooltip("Cumulative Physical damage milestones in ascending order " +
                 "(e.g. [500, 1000, 5000]).")]
        [SerializeField] private float[] _physicalMilestones = new float[0];

        [Tooltip("Cumulative Energy damage milestones in ascending order.")]
        [SerializeField] private float[] _energyMilestones = new float[0];

        [Tooltip("Cumulative Thermal damage milestones in ascending order.")]
        [SerializeField] private float[] _thermalMilestones = new float[0];

        [Tooltip("Cumulative Shock damage milestones in ascending order.")]
        [SerializeField] private float[] _shockMilestones = new float[0];

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the milestone array for the given <paramref name="type"/>.
        /// Returns null for unknown types (rather than an empty array) so callers can
        /// distinguish "no config for this type" from "configured but empty".
        /// </summary>
        public float[] GetMilestonesForType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalMilestones;
                case DamageType.Energy:   return _energyMilestones;
                case DamageType.Thermal:  return _thermalMilestones;
                case DamageType.Shock:    return _shockMilestones;
                default:                  return null;
            }
        }

        /// <summary>
        /// Returns how many milestones the player has cleared for <paramref name="type"/>
        /// given <paramref name="accumulation"/>.
        ///
        /// <para>A milestone is cleared when <paramref name="accumulation"/> ≥ threshold.</para>
        /// <para>Returns 0 when there are no milestones or the array is null.</para>
        /// </summary>
        public int GetClearedCount(DamageType type, float accumulation)
        {
            float[] milestones = GetMilestonesForType(type);
            if (milestones == null) return 0;

            int cleared = 0;
            for (int i = 0; i < milestones.Length; i++)
            {
                if (accumulation >= milestones[i])
                    cleared++;
            }
            return cleared;
        }

        /// <summary>
        /// Returns the value of the next uncleared milestone for <paramref name="type"/>,
        /// or <c>null</c> when all milestones have been cleared or there are no milestones.
        /// </summary>
        public float? GetNextMilestone(DamageType type, float accumulation)
        {
            float[] milestones = GetMilestonesForType(type);
            if (milestones == null) return null;

            for (int i = 0; i < milestones.Length; i++)
            {
                if (accumulation < milestones[i])
                    return milestones[i];
            }
            return null;
        }

        /// <summary>
        /// Returns the progress toward the next milestone for <paramref name="type"/>
        /// in [0, 1].
        ///
        /// <para>When all milestones are cleared, returns 1. When there are no milestones
        /// or the type is unknown, returns 0.</para>
        ///
        /// <para>Progress is computed as:
        ///   <c>(accumulation − prevMilestone) / (nextMilestone − prevMilestone)</c>
        /// where <c>prevMilestone</c> is 0 for the first segment.</para>
        /// </summary>
        public float GetProgress(DamageType type, float accumulation)
        {
            float[] milestones = GetMilestonesForType(type);
            if (milestones == null || milestones.Length == 0) return 0f;

            int cleared = GetClearedCount(type, accumulation);

            // All milestones cleared → fully filled bar.
            if (cleared >= milestones.Length) return 1f;

            float prevThreshold = cleared > 0 ? milestones[cleared - 1] : 0f;
            float nextThreshold = milestones[cleared];
            float range         = nextThreshold - prevThreshold;

            if (range <= 0f) return 0f;

            return Mathf.Clamp01((accumulation - prevThreshold) / range);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SortAscending(ref _physicalMilestones);
            SortAscending(ref _energyMilestones);
            SortAscending(ref _thermalMilestones);
            SortAscending(ref _shockMilestones);
        }

        private static void SortAscending(ref float[] array)
        {
            if (array == null || array.Length < 2) return;
            System.Array.Sort(array);
        }
#endif
    }
}
