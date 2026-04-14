using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that holds a score multiplier applied when computing
    /// a match score after a prestige reward has been activated.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   • <see cref="OnEnable"/> resets <see cref="Multiplier"/> to
    ///     <see cref="_defaultMultiplier"/> (domain-reload safe).
    ///   • <see cref="PrestigeRewardBonusApplier"/> calls <see cref="SetMultiplier"/>
    ///     at match start and <see cref="ResetToDefault"/> at match end.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime multiplier is NOT serialised to the SO asset.
    ///   - SetMultiplier clamps to [0.01, 10] to prevent degenerate values.
    ///   - Zero alloc on all hot paths.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ ScoreMultiplier.
    /// Assign to <see cref="BattleRobots.Physics.PrestigeRewardBonusApplier"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/ScoreMultiplier",
        fileName = "ScoreMultiplierSO")]
    public sealed class ScoreMultiplierSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Default multiplier restored between matches. Must be ≥ 0.01.")]
        [SerializeField, Min(0.01f)] private float _defaultMultiplier = 1f;

        // ── Runtime state ─────────────────────────────────────────────────────

        private float _multiplier = 1f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _multiplier = _defaultMultiplier;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Current score multiplier. Initialised from <see cref="_defaultMultiplier"/>
        /// on OnEnable. Modified by <see cref="SetMultiplier"/> and restored by
        /// <see cref="ResetToDefault"/>.
        /// </summary>
        public float Multiplier => _multiplier;

        /// <summary>The default multiplier as configured in the Inspector.</summary>
        public float DefaultMultiplier => _defaultMultiplier;

        /// <summary>
        /// Sets <see cref="Multiplier"/> to <paramref name="value"/>,
        /// clamped to the range [0.01, 10].
        /// </summary>
        /// <param name="value">Desired multiplier value.</param>
        public void SetMultiplier(float value)
        {
            _multiplier = Mathf.Clamp(value, 0.01f, 10f);
        }

        /// <summary>
        /// Resets <see cref="Multiplier"/> back to <see cref="DefaultMultiplier"/>.
        /// Called at match end by <see cref="BattleRobots.Physics.PrestigeRewardBonusApplier"/>.
        /// </summary>
        public void ResetToDefault()
        {
            _multiplier = _defaultMultiplier;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_defaultMultiplier < 0.01f)
            {
                Debug.LogWarning(
                    $"[ScoreMultiplierSO] _defaultMultiplier ({_defaultMultiplier}) " +
                    $"is below 0.01 — clamped to 0.01.", this);
                _defaultMultiplier = 0.01f;
            }
        }
#endif
    }
}
