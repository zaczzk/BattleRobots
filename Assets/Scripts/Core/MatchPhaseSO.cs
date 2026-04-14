using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Enum representing the phases of a match lifecycle.
    /// </summary>
    public enum MatchPhase
    {
        PreMatch    = 0,
        Active      = 1,
        SuddenDeath = 2,
        PostMatch   = 3
    }

    /// <summary>
    /// Runtime SO that tracks the current match phase and broadcasts phase transitions
    /// via a VoidGameEvent channel.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   MatchManager (or other coordinator) calls SetPhase() on state transitions.
    ///   SetPhase raises _onPhaseChanged → MatchPhaseHUDController.Refresh().
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SetPhase is idempotent — setting the same phase twice fires no event.
    ///   - Zero allocation on the hot path (enum comparison + optional Raise).
    ///   - Resets to PreMatch on OnEnable so Play-mode enter and domain-reload both
    ///     produce a clean initial state.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ MatchPhase.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/MatchPhase")]
    public sealed class MatchPhaseSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel (optional)")]
        [Tooltip("Raised inside SetPhase() when the phase actually changes. " +
                 "Subscribe MatchPhaseHUDController to this event.")]
        [SerializeField] private VoidGameEvent _onPhaseChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private MatchPhase _currentPhase;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            // Always start in PreMatch so the HUD shows the correct initial state.
            _currentPhase = MatchPhase.PreMatch;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The current match phase.</summary>
        public MatchPhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Human-readable label for the current phase, suitable for HUD display.
        /// </summary>
        public string PhaseLabel
        {
            get
            {
                switch (_currentPhase)
                {
                    case MatchPhase.Active:      return "Active";
                    case MatchPhase.SuddenDeath: return "Sudden Death";
                    case MatchPhase.PostMatch:   return "Post-Match";
                    default:                     return "Pre-Match";
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Transitions to <paramref name="phase"/> and raises <c>_onPhaseChanged</c>.
        /// No-op and no event when <paramref name="phase"/> equals <see cref="CurrentPhase"/>.
        /// Zero allocation — enum comparison only.
        /// </summary>
        public void SetPhase(MatchPhase phase)
        {
            if (_currentPhase == phase) return;
            _currentPhase = phase;
            _onPhaseChanged?.Raise();
        }
    }
}
