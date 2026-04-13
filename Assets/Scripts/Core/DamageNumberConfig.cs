using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data SO that configures the appearance and animation of floating damage number
    /// popups spawned by <see cref="BattleRobots.UI.DamageNumberController"/>.
    ///
    /// ── Visual rules ────────────────────────────────────────────────────────────
    ///   Normal hits render in <see cref="NormalColor"/> at scale 1×.
    ///   Critical hits (flagged by the _onCriticalHit channel) render in
    ///   <see cref="CriticalColor"/> scaled by <see cref="CritScaleMultiplier"/>.
    ///   Each label floats upward <see cref="FloatDistance"/> world-units over
    ///   <see cref="FloatDuration"/> seconds, then fades out and returns to the pool.
    ///
    /// ── Integration ─────────────────────────────────────────────────────────────
    ///   Assign to <c>DamageNumberController._config</c>. The controller reads these
    ///   settings whenever it spawns or recycles a label — no runtime mutation needed.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All properties are read-only; assets are immutable at runtime.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ UI ▶ DamageNumberConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/UI/DamageNumberConfig")]
    public sealed class DamageNumberConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Colors")]
        [Tooltip("Color used for a normal (non-critical) damage number.")]
        [SerializeField] private Color _normalColor = Color.white;

        [Tooltip("Color used when the hit was flagged as a critical hit.")]
        [SerializeField] private Color _criticalColor = Color.yellow;

        [Header("Animation")]
        [Tooltip("World-units the number floats upward before fading out.")]
        [SerializeField, Min(0.1f)] private float _floatDistance = 1.5f;

        [Tooltip("Seconds the number takes to complete the float-and-fade animation.")]
        [SerializeField, Min(0.1f)] private float _floatDuration = 0.8f;

        [Tooltip("Scale multiplier applied to critical-hit numbers (>= 1). " +
                 "E.g., 1.5 makes crits 50 % larger than normal numbers.")]
        [SerializeField, Min(1f)] private float _critScaleMultiplier = 1.5f;

        [Header("Pool")]
        [Tooltip("Number of label objects pre-allocated in the pool. " +
                 "Increase if many simultaneous hits cause label pop-in.")]
        [SerializeField, Min(1)] private int _poolSize = 20;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Color applied to normal (non-crit) damage numbers.</summary>
        public Color NormalColor => _normalColor;

        /// <summary>Color applied to critical-hit damage numbers.</summary>
        public Color CriticalColor => _criticalColor;

        /// <summary>World-units the number rises before fading. Always >= 0.1.</summary>
        public float FloatDistance => _floatDistance;

        /// <summary>Seconds from spawn to fully faded. Always >= 0.1.</summary>
        public float FloatDuration => _floatDuration;

        /// <summary>Scale multiplier for crits (>= 1). Normal hits use scale 1x.</summary>
        public float CritScaleMultiplier => _critScaleMultiplier;

        /// <summary>Pre-allocated pool size. Increase for busy arenas.</summary>
        public int PoolSize => _poolSize;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _floatDistance      = Mathf.Max(0.1f, _floatDistance);
            _floatDuration      = Mathf.Max(0.1f, _floatDuration);
            _critScaleMultiplier = Mathf.Max(1f,  _critScaleMultiplier);
            _poolSize           = Mathf.Max(1,    _poolSize);
        }
#endif
    }
}
