using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Modifier type taxonomy used by <see cref="MatchModifierSO"/>.
    /// Informational only — the actual gameplay effect is driven by the four
    /// multiplier fields on the SO, not this enum.
    ///
    /// Suggested presets:
    ///   Standard      — all multipliers 1.0  (no modifier).
    ///   DoubleRewards — RewardMultiplier 2.0.
    ///   ExtendedTime  — TimeMultiplier   2.0.
    ///   ShortTime     — TimeMultiplier   0.5.
    ///   FragileArmor  — ArmorMultiplier  0.0 (armor stripped entirely).
    ///   Overdrive     — SpeedMultiplier  1.5.
    /// </summary>
    public enum MatchModifierType
    {
        Standard,
        DoubleRewards,
        ExtendedTime,
        ShortTime,
        FragileArmor,
        Overdrive,
    }

    /// <summary>
    /// Immutable SO defining a single pre-match rule modifier that tweaks
    /// economy rewards, round duration, robot armor, and robot speed for
    /// a single match.
    ///
    /// ── How it flows ──────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.UI.MatchModifierSelectionController"/> writes the
    ///   chosen modifier to <see cref="SelectedModifierSO"/>.
    ///
    ///   <see cref="MatchManager"/> reads <see cref="RewardMultiplier"/> and
    ///   <see cref="TimeMultiplier"/> at match-start / match-end time.
    ///
    ///   <see cref="BattleRobots.Physics.CombatStatsApplicator"/> reads
    ///   <see cref="ArmorMultiplier"/> and <see cref="SpeedMultiplier"/> when
    ///   pushing computed stats to the robot's runtime components at match-start.
    ///
    /// ── Design constraints ────────────────────────────────────────────────────
    ///   All multipliers are floored by <c>OnValidate</c> to prevent degenerate
    ///   values (zero or negative) that would break physics or economy.
    ///   <see cref="ArmorMultiplier"/> is the one exception — it is allowed to
    ///   reach 0 so the "FragileArmor" preset can strip armor entirely.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Match ▶ MatchModifier.
    /// Assign instances to a <see cref="MatchModifierCatalogSO"/> so the
    /// selection controller can present them to the player.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Match/MatchModifier", order = 1)]
    public sealed class MatchModifierSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Informational category. Does not affect runtime behaviour; " +
                 "only the four multiplier fields below drive gameplay.")]
        [SerializeField] private MatchModifierType _modifierType = MatchModifierType.Standard;

        [Tooltip("Short name displayed in the pre-match selection UI.")]
        [SerializeField] private string _displayName = "Standard";

        [Tooltip("One-line description shown in the pre-match UI.")]
        [SerializeField, TextArea(1, 3)] private string _description = "";

        [Tooltip("Multiplier applied to the total match reward (win or loss) at the end of " +
                 "the match. 1.0 = no change. 2.0 = double rewards. Min 0.1.")]
        [SerializeField, Min(0.1f)] private float _rewardMultiplier = 1f;

        [Tooltip("Multiplier applied to the base round duration at match start. " +
                 "1.0 = no change. 2.0 = double time. 0.5 = half time. Min 0.1.")]
        [SerializeField, Min(0.1f)] private float _timeMultiplier = 1f;

        [Tooltip("Multiplier applied to each robot's computed armor rating when " +
                 "CombatStatsApplicator pushes stats at match start. " +
                 "0.0 = no armor (FragileArmor mode). 1.0 = no change. Min 0.")]
        [SerializeField, Min(0f)] private float _armorMultiplier = 1f;

        [Tooltip("Multiplier applied to each robot's base move speed when " +
                 "CombatStatsApplicator pushes stats at match start. " +
                 "1.5 = 50 % faster (Overdrive mode). Min 0.1.")]
        [SerializeField, Min(0.1f)] private float _speedMultiplier = 1f;

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>Informational type tag. Does not drive runtime behaviour.</summary>
        public MatchModifierType ModifierType => _modifierType;

        /// <summary>Short display name for the pre-match selection UI.</summary>
        public string DisplayName => _displayName;

        /// <summary>One-line description shown in the pre-match UI.</summary>
        public string Description => _description;

        /// <summary>
        /// Applied to the total match reward in <c>MatchManager.EndMatch()</c>.
        /// Guaranteed ≥ 0.1 by <c>OnValidate</c>.
        /// </summary>
        public float RewardMultiplier => _rewardMultiplier;

        /// <summary>
        /// Applied to the base round duration in <c>MatchManager.HandleMatchStarted()</c>.
        /// Guaranteed ≥ 0.1 by <c>OnValidate</c>.
        /// </summary>
        public float TimeMultiplier => _timeMultiplier;

        /// <summary>
        /// Applied to the computed armor rating in <c>CombatStatsApplicator.ApplyStats()</c>.
        /// Clamped to [0, 100] after multiplication. Guaranteed ≥ 0 by <c>OnValidate</c>.
        /// </summary>
        public float ArmorMultiplier => _armorMultiplier;

        /// <summary>
        /// Applied to the computed base speed in <c>CombatStatsApplicator.ApplyStats()</c>.
        /// Guaranteed ≥ 0.1 by <c>OnValidate</c>.
        /// </summary>
        public float SpeedMultiplier => _speedMultiplier;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _rewardMultiplier = Mathf.Max(0.1f, _rewardMultiplier);
            _timeMultiplier   = Mathf.Max(0.1f, _timeMultiplier);
            _armorMultiplier  = Mathf.Max(0f,   _armorMultiplier);
            _speedMultiplier  = Mathf.Max(0.1f, _speedMultiplier);

            if (string.IsNullOrWhiteSpace(_displayName))
                Debug.LogWarning($"[MatchModifierSO] '{name}': DisplayName is empty — " +
                                 "the selection UI will show a blank label.");
        }
#endif
    }
}
