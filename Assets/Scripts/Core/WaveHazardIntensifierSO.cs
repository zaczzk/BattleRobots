using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configuration ScriptableObject that defines how hazard toggle speed scales with
    /// each successive wave in a survival run.
    ///
    /// ── Formula ───────────────────────────────────────────────────────────────
    ///   interval = Max(<see cref="MinimumInterval"/>,
    ///                  <see cref="BaseInterval"/> − wave × <see cref="IntensityReduction"/>)
    ///
    ///   At wave 0 the interval equals <see cref="BaseInterval"/>.
    ///   Each subsequent wave subtracts <see cref="IntensityReduction"/> seconds,
    ///   clamped at <see cref="MinimumInterval"/> so hazards never toggle faster than
    ///   the designer-specified minimum.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • Pure config — no runtime state.
    ///   • <see cref="GetIntervalForWave(int)"/> is the sole computation entry point.
    ///   • Negative wave values are treated as 0 (clamped inside the formula).
    ///   • OnValidate warns when MinimumInterval > BaseInterval.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ WaveHazardIntensifier.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/WaveHazardIntensifier", order = 18)]
    public sealed class WaveHazardIntensifierSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Intensifier Settings")]
        [Tooltip("Toggle interval (seconds) used at wave 0. Minimum 0.5 s.")]
        [SerializeField, Min(0.5f)] private float _baseInterval = 5f;

        [Tooltip("Seconds subtracted from the toggle interval per wave. " +
                 "Set to 0 for constant interval across all waves.")]
        [SerializeField, Min(0f)] private float _intensityReduction = 0.5f;

        [Tooltip("Floor for the toggle interval — hazards will never toggle faster than " +
                 "this value regardless of wave number. Minimum 0.5 s.")]
        [SerializeField, Min(0.5f)] private float _minimumInterval = 1f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Toggle interval at wave 0.</summary>
        public float BaseInterval => _baseInterval;

        /// <summary>Seconds removed from the interval each wave.</summary>
        public float IntensityReduction => _intensityReduction;

        /// <summary>Minimum toggle interval — the floor applied by <see cref="GetIntervalForWave"/>.</summary>
        public float MinimumInterval => _minimumInterval;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the toggle interval in seconds for <paramref name="wave"/>.
        /// Negative wave values are clamped to 0 so the result never exceeds
        /// <see cref="BaseInterval"/>.
        /// </summary>
        public float GetIntervalForWave(int wave)
        {
            int clampedWave = Mathf.Max(0, wave);
            float raw       = _baseInterval - clampedWave * _intensityReduction;
            return Mathf.Max(_minimumInterval, raw);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_minimumInterval > _baseInterval)
                Debug.LogWarning($"[WaveHazardIntensifierSO] '{name}': " +
                                 $"_minimumInterval ({_minimumInterval}s) exceeds " +
                                 $"_baseInterval ({_baseInterval}s) — interval will always " +
                                 "equal _minimumInterval regardless of wave.");
        }
#endif
    }
}
