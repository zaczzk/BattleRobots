using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject that configures the visual hit-flash effect applied by
    /// <see cref="BattleRobots.Physics.DamageFlashController"/> whenever a robot
    /// takes damage.
    ///
    /// Designer-tunable without any code changes:
    ///   • <see cref="FlashColor"/>    — tint colour applied to all child Renderers.
    ///   • <see cref="FlashDuration"/> — how long (seconds) the tint persists.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Combat ▶ DamageFlashConfig.
    /// One global instance is recommended; swap assets on individual robots for
    /// per-robot flash styles (e.g. red for player, yellow for enemy).
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/DamageFlashConfig", order = 6)]
    public sealed class DamageFlashConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Flash Colour")]
        [Tooltip("Colour tint applied to all child Renderers on damage. " +
                 "Use a saturated colour (e.g. red) for clarity.")]
        [SerializeField] private Color _flashColor = Color.red;

        [Header("Timing")]
        [Tooltip("Duration in seconds the flash colour persists. Clamped to ≥ 0.05 s.")]
        [SerializeField, Min(0.05f)] private float _flashDuration = 0.15f;

        // ── Public properties ─────────────────────────────────────────────────

        /// <summary>Tint colour to apply to all child Renderers on hit.</summary>
        public Color FlashColor => _flashColor;

        /// <summary>Duration in seconds the flash colour persists. Always ≥ 0.05 s.</summary>
        public float FlashDuration => _flashDuration;

        // ── Editor validation ─────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_flashDuration < 0.05f)
            {
                _flashDuration = 0.05f;
                Debug.LogWarning(
                    "[DamageFlashConfig] FlashDuration clamped to minimum 0.05 s.", this);
            }
        }
    }
}
