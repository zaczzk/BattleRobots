using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data SO that configures the announcer message strings and display duration for
    /// prominent in-match moments (critical hit, maximum momentum, sudden death, etc.).
    ///
    /// ── Integration ─────────────────────────────────────────────────────────────
    ///   Assign to <c>MatchAnnouncerController._config</c>. The controller selects the
    ///   appropriate string when a game event fires and pushes it to an on-screen label.
    ///   All fields are optional empty strings — leave blank to suppress that callout.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime; MatchAnnouncerController reads properties only.
    ///   - OnValidate clamps MessageDuration to a minimum so the banner is always readable.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ UI ▶ MatchAnnouncerConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/UI/MatchAnnouncerConfig")]
    public sealed class MatchAnnouncerConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Combat Callouts")]
        [Tooltip("Text displayed when a critical hit lands. Leave blank to suppress.")]
        [SerializeField] private string _critHitMessage = "CRITICAL HIT!";

        [Tooltip("Text displayed when the momentum bar reaches maximum. Leave blank to suppress.")]
        [SerializeField] private string _momentumFullMessage = "MAXIMUM MOMENTUM!";

        [Tooltip("Text displayed when sudden-death mode activates. Leave blank to suppress.")]
        [SerializeField] private string _suddenDeathMessage = "SUDDEN DEATH!";

        [Header("Match Flow Callouts")]
        [Tooltip("Text displayed at match start (after countdown). Leave blank to suppress.")]
        [SerializeField] private string _matchStartMessage = "FIGHT!";

        [Tooltip("Text displayed when the player wins. Leave blank to suppress.")]
        [SerializeField] private string _playerWinMessage = "VICTORY!";

        [Tooltip("Text displayed when the player loses. Leave blank to suppress.")]
        [SerializeField] private string _playerLossMessage = "DEFEATED!";

        [Header("Timing")]
        [Tooltip("Seconds the announcer banner stays visible before fading out. " +
                 "Minimum 0.5 to ensure readability.")]
        [SerializeField, Min(0.5f)] private float _messageDuration = 2.5f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Text for the critical-hit callout. May be empty.</summary>
        public string CritHitMessage => _critHitMessage;

        /// <summary>Text for the full-momentum callout. May be empty.</summary>
        public string MomentumFullMessage => _momentumFullMessage;

        /// <summary>Text for the sudden-death callout. May be empty.</summary>
        public string SuddenDeathMessage => _suddenDeathMessage;

        /// <summary>Text for the match-start callout. May be empty.</summary>
        public string MatchStartMessage => _matchStartMessage;

        /// <summary>Text for the player-win callout. May be empty.</summary>
        public string PlayerWinMessage => _playerWinMessage;

        /// <summary>Text for the player-loss callout. May be empty.</summary>
        public string PlayerLossMessage => _playerLossMessage;

        /// <summary>Seconds the banner stays visible. Always >= 0.5.</summary>
        public float MessageDuration => _messageDuration;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _messageDuration = Mathf.Max(0.5f, _messageDuration);
        }
#endif
    }
}
