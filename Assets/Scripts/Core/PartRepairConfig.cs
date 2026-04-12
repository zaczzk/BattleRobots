using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable config SO that defines the credit cost to repair a damaged part.
    ///
    /// Cost formula (per part):
    ///   cost = Ceil( missingHP × CreditsPerHPPoint )
    ///
    /// Setting a higher <see cref="CreditsPerHPPoint"/> makes repairs more expensive,
    /// creating a stronger credits-sink that balances post-match loot and reward income.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO assets are immutable at runtime — only the Inspector changes the config.
    ///   - <see cref="GetRepairCost"/> is zero-allocation: pure arithmetic.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ PartRepairConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/PartRepairConfig",
                     fileName = "PartRepairConfig")]
    public sealed class PartRepairConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Repair Economy")]
        [Tooltip("Credits charged per missing HP point. Minimum 0.1 to prevent free repairs. " +
                 "Example: 2.0 means a part missing 25 HP costs ceil(25 × 2.0) = 50 credits.")]
        [SerializeField, Min(0.1f)] private float _creditsPerHPPoint = 2f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Credits charged per missing HP point. Always ≥ 0.1 (Inspector enforced).</summary>
        public float CreditsPerHPPoint => _creditsPerHPPoint;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the credit cost to fully repair <paramref name="condition"/> to MaxHP.
        /// Returns 0 when <paramref name="condition"/> is null or already at full HP.
        /// <br/>
        /// Formula: <c>Ceil( (MaxHP − CurrentHP) × CreditsPerHPPoint )</c>.
        /// Zero-allocation: pure float arithmetic.
        /// </summary>
        public int GetRepairCost(PartConditionSO condition)
        {
            if (condition == null) return 0;
            float missingHP = condition.MaxHP - condition.CurrentHP;
            if (missingHP <= 0f) return 0;
            return Mathf.CeilToInt(missingHP * _creditsPerHPPoint);
        }
    }
}
