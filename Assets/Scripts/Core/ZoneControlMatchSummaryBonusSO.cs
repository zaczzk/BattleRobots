using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that computes an end-of-match multi-factor summary bonus from
    /// captures, efficiency and combo count.  The weighted sum is scaled by
    /// <c>_bonusScale</c>.  Call <see cref="ApplySummaryBonus"/> to cache the result
    /// and fire <c>_onBonusApplied</c>.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchSummaryBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchSummaryBonus", order = 109)]
    public sealed class ZoneControlMatchSummaryBonusSO : ScriptableObject
    {
        [Header("Weights")]
        [Min(0f)]
        [SerializeField] private float _captureWeight   = 2f;
        [Min(0f)]
        [SerializeField] private float _efficiencyWeight = 3f;
        [Min(0f)]
        [SerializeField] private float _comboWeight     = 1f;

        [Header("Scale")]
        [Min(1)]
        [SerializeField] private int _bonusScale = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBonusApplied;

        private int _lastBonus;
        private int _totalBonus;

        private void OnEnable() => Reset();

        public int   LastBonus       => _lastBonus;
        public int   TotalBonus      => _totalBonus;
        public float CaptureWeight   => _captureWeight;
        public float EfficiencyWeight => _efficiencyWeight;
        public float ComboWeight     => _comboWeight;
        public int   BonusScale      => _bonusScale;

        /// <summary>
        /// Computes a summary bonus from <paramref name="captures"/>,
        /// <paramref name="efficiency"/> [0,1], and <paramref name="combos"/>.
        /// Each factor is normalised by weight then scaled by <c>_bonusScale</c>.
        /// </summary>
        public int ComputeSummaryBonus(int captures, float efficiency, int combos)
        {
            float totalWeight = _captureWeight + _efficiencyWeight + _comboWeight;
            if (totalWeight <= 0f) return 0;

            float normCaptures   = Mathf.Clamp01(captures   / Mathf.Max(1f, captures));
            float normEfficiency = Mathf.Clamp01(efficiency);
            float normCombos     = Mathf.Clamp01(combos / Mathf.Max(1f, combos));

            float weighted = (_captureWeight   * normCaptures
                            + _efficiencyWeight * normEfficiency
                            + _comboWeight     * normCombos)
                           / totalWeight;

            return Mathf.RoundToInt(weighted * _bonusScale);
        }

        /// <summary>
        /// Applies the computed summary bonus, caches it, accumulates total, and fires
        /// <c>_onBonusApplied</c> when bonus &gt; 0.  Returns the bonus amount.
        /// </summary>
        public int ApplySummaryBonus(int captures, float efficiency, int combos)
        {
            int bonus = ComputeSummaryBonus(captures, efficiency, combos);
            _lastBonus   = bonus;
            _totalBonus += bonus;
            if (bonus > 0)
                _onBonusApplied?.Raise();
            return bonus;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _lastBonus  = 0;
            _totalBonus = 0;
        }
    }
}
