using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data SO that configures critical-hit behaviour for a robot or weapon loadout.
    ///
    /// ── Critical-hit rules ──────────────────────────────────────────────────────
    ///   On each damage application, the DamageReceiver rolls a uniform random number.
    ///   If the result is less than <see cref="CriticalChance"/> the hit is "critical":
    ///   the raw incoming damage (before armor reduction) is multiplied by
    ///   <see cref="CriticalMultiplier"/>, and <see cref="RaiseOnCrit"/> fires the
    ///   optional <see cref="_onCriticalHit"/> event for VFX / audio feedback.
    ///
    /// ── Integration ─────────────────────────────────────────────────────────────
    ///   Assign to DamageReceiver._critConfig (optional header "Critical Hits").
    ///   The crit roll is applied to BOTH TakeDamage(float) and TakeDamage(DamageInfo),
    ///   with the multiplied value then passing through the normal shield→armor→health
    ///   pipeline, so crits naturally pierce defences harder.
    ///
    ///   Wire _onCriticalHit to a VoidGameEvent that triggers a hit-flash particle or
    ///   a "CRIT!" text popup controller.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Allocation-free hot path: <see cref="ComputeCritDamage"/> uses only float
    ///     arithmetic and a single Random.value call.
    ///   - Use criticalChance = 0 (never crit) or 1 (always crit) in EditMode tests
    ///     to keep tests deterministic.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ CriticalHitConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/CriticalHitConfig")]
    public sealed class CriticalHitConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Critical Hit Chance")]
        [Tooltip("Probability (0–1) that any given hit is a critical hit. " +
                 "0 = never crit, 1 = always crit.")]
        [SerializeField, Range(0f, 1f)] private float _criticalChance = 0.1f;

        [Header("Critical Hit Multiplier")]
        [Tooltip("Damage multiplier applied to the raw incoming amount on a critical hit. " +
                 "Values below 1 are clamped to 1 (crits never reduce damage).")]
        [SerializeField, Min(1f)] private float _criticalMultiplier = 2f;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised every time a critical hit lands on the receiver. " +
                 "Wire to a VFX controller or 'CRIT!' HUD popup for feedback.")]
        [SerializeField] private VoidGameEvent _onCriticalHit;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Probability per-hit that the hit is critical. Range [0, 1].</summary>
        public float CriticalChance => _criticalChance;

        /// <summary>Damage multiplier on a critical hit. Always ≥ 1.</summary>
        public float CriticalMultiplier => _criticalMultiplier;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Raises the optional <see cref="_onCriticalHit"/> event.
        /// Null-safe — no-op when the channel is unassigned.
        /// </summary>
        public void RaiseOnCrit()
        {
            _onCriticalHit?.Raise();
        }

        /// <summary>
        /// Rolls the critical-hit check and returns the (potentially multiplied) damage.
        ///
        /// Algorithm (allocation-free):
        ///   1. If <paramref name="config"/> is null → returns <paramref name="rawAmount"/> unchanged.
        ///   2. Rolls Random.value; if result &lt; CriticalChance → crit.
        ///   3. On crit: multiplies <paramref name="rawAmount"/> by CriticalMultiplier,
        ///      sets <paramref name="isCrit"/> = true, and calls <see cref="RaiseOnCrit"/>.
        ///   4. On miss: returns <paramref name="rawAmount"/> unchanged.
        ///
        /// The returned value is the pre-armor damage to pass into the shield→armor pipeline.
        /// </summary>
        /// <param name="rawAmount">Incoming damage before any crit roll.</param>
        /// <param name="config">Config asset (may be null — treated as no-crit).</param>
        /// <param name="isCrit">True when this hit was a critical hit.</param>
        /// <returns>Damage amount after crit multiplier (or unchanged on miss/null config).</returns>
        public static float ComputeCritDamage(float rawAmount, CriticalHitConfig config, out bool isCrit)
        {
            isCrit = false;
            if (config == null)
                return rawAmount;

            if (Random.value < config._criticalChance)
            {
                isCrit = true;
                config.RaiseOnCrit();
                return rawAmount * config._criticalMultiplier;
            }

            return rawAmount;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_criticalChance <= 0f)
                Debug.LogWarning(
                    $"[CriticalHitConfig] '{name}': CriticalChance is 0 — no hits will ever crit.",
                    this);
        }
#endif
    }
}
