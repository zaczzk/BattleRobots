using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Three categories of status effect that can be applied to a robot mid-match.
    /// </summary>
    public enum StatusEffectType
    {
        /// <summary>Deals damage over time via periodic FixedUpdate ticks.</summary>
        Burn = 0,

        /// <summary>Freezes locomotion inputs (zero move + turn) for the effect duration.</summary>
        Stun = 1,

        /// <summary>Multiplies the robot's effective move speed by <see cref="StatusEffectSO.SlowFactor"/>.</summary>
        Slow = 2,
    }

    /// <summary>
    /// Immutable data asset that defines one status-effect archetype (Burn / Stun / Slow).
    ///
    /// ── Effect types ──────────────────────────────────────────────────────────
    ///   • Burn — deals <see cref="DamagePerSecond"/> every FixedUpdate tick for
    ///            <see cref="DurationSeconds"/> seconds via the target's DamageReceiver.
    ///   • Stun — tells RobotLocomotionController to zero velocity for the duration.
    ///   • Slow — multiplies the robot's effective speed by <see cref="SlowFactor"/> for the duration.
    ///
    /// ── Stacking rule ─────────────────────────────────────────────────────────
    ///   Only one instance of each <see cref="StatusEffectType"/> can be active on a robot
    ///   at a time. A second application of the same type replaces the current one only
    ///   when <see cref="DurationSeconds"/> exceeds the time already remaining
    ///   (take-maximum rule). This prevents spamming short effects to refresh duration.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core only — no Physics / UI namespace references.
    ///   • Immutable at runtime — all fields are accessed through read-only properties.
    ///   • Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ StatusEffect.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/StatusEffect", fileName = "New StatusEffect")]
    public sealed class StatusEffectSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Effect Type")]
        [SerializeField]
        [Tooltip("Which category of status effect this asset represents.")]
        private StatusEffectType _type = StatusEffectType.Burn;

        [Header("Duration")]
        [SerializeField, Min(0.1f)]
        [Tooltip("How long the effect persists, in seconds. Minimum 0.1 s.")]
        private float _durationSeconds = 3f;

        [Header("Burn Settings (Burn type only)")]
        [SerializeField, Min(0f)]
        [Tooltip("Damage applied to the target per second while this Burn effect is active. " +
                 "Zero-damage is valid (pure visual / status-only Burn). " +
                 "Ignored for Stun and Slow types.")]
        private float _damagePerSecond = 5f;

        [Header("Slow Settings (Slow type only)")]
        [SerializeField, Range(0.01f, 1f)]
        [Tooltip("Speed multiplier applied to the robot while this Slow effect is active. " +
                 "1.0 = no slowdown, 0.5 = half speed, 0.01 = nearly frozen. " +
                 "Ignored for Burn and Stun types.")]
        private float _slowFactor = 0.5f;

        [Header("Display")]
        [SerializeField]
        [Tooltip("Human-readable name shown in HUD status icons and notifications.")]
        private string _displayName = string.Empty;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Which category of status effect this asset represents.</summary>
        public StatusEffectType Type => _type;

        /// <summary>How long the effect persists, in seconds (minimum 0.1 s).</summary>
        public float DurationSeconds => _durationSeconds;

        /// <summary>Damage per second applied each FixedUpdate tick for Burn-type effects.</summary>
        public float DamagePerSecond => _damagePerSecond;

        /// <summary>
        /// Speed multiplier applied for Slow-type effects (range [0.01, 1]).
        /// Values below 1 reduce locomotion speed proportionally.
        /// </summary>
        public float SlowFactor => _slowFactor;

        /// <summary>Human-readable effect name used in HUD icons and notifications.</summary>
        public string DisplayName => _displayName;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _durationSeconds = Mathf.Max(0.1f, _durationSeconds);
            _damagePerSecond = Mathf.Max(0f,   _damagePerSecond);
            _slowFactor      = Mathf.Clamp(_slowFactor, 0.01f, 1f);

            if (string.IsNullOrEmpty(_displayName))
                Debug.LogWarning($"[StatusEffectSO] '{name}' has no DisplayName — " +
                                 "HUD status icons will be blank.", this);
        }
#endif
    }
}
