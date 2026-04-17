using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that detects a tied score at match end and declares
    /// the active tie-break rule.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="EvaluateTie"/> with the player and bot scores.
    ///   A tie is declared (and <c>_onTieBreakTriggered</c> fired) only on the
    ///   first transition from not-tied to tied.  Resolving the tie (unequal scores)
    ///   clears <see cref="IsActive"/> silently.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlTieBreak.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlTieBreak", order = 63)]
    public sealed class ZoneControlTieBreakSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Rule applied when scores are equal.")]
        [SerializeField] private ZoneControlTieBreakType _tieBreakType = ZoneControlTieBreakType.SuddenDeath;

        [Tooltip("Extra captures required to win in CaptureTarget tie-break mode.")]
        [Min(1)]
        [SerializeField] private int _captureTargetBonus = 3;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when a tie is first detected (false→true transition only).")]
        [SerializeField] private VoidGameEvent _onTieBreakTriggered;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool _isActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True when a tie has been detected and not yet resolved.</summary>
        public bool IsActive => _isActive;

        /// <summary>The tie-break rule in use.</summary>
        public ZoneControlTieBreakType TieBreakType => _tieBreakType;

        /// <summary>Extra captures required for the CaptureTarget tie-break.</summary>
        public int CaptureTargetBonus => _captureTargetBonus;

        /// <summary>Human-readable description of the active tie-break rule.</summary>
        public string TieBreakDescription =>
            _tieBreakType switch
            {
                ZoneControlTieBreakType.CaptureTarget => $"Capture Target: +{_captureTargetBonus}",
                ZoneControlTieBreakType.TimeAdvantage => "Time Advantage",
                _                                     => "Sudden Death",
            };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Compares <paramref name="playerScore"/> and <paramref name="botScore"/>.
        /// When equal and not already active, sets <see cref="IsActive"/> to true and
        /// fires <c>_onTieBreakTriggered</c>.
        /// When unequal and currently active, clears <see cref="IsActive"/> silently.
        /// No-op when the state would not change.
        /// Zero allocation.
        /// </summary>
        public void EvaluateTie(int playerScore, int botScore)
        {
            bool tied = playerScore == botScore;

            if (tied && !_isActive)
            {
                _isActive = true;
                _onTieBreakTriggered?.Raise();
            }
            else if (!tied && _isActive)
            {
                _isActive = false;
            }
        }

        /// <summary>
        /// Clears tie-break state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _isActive = false;
        }

        private void OnValidate()
        {
            _captureTargetBonus = Mathf.Max(1, _captureTargetBonus);
        }
    }

    /// <summary>Determines how a tied zone-control match is resolved.</summary>
    public enum ZoneControlTieBreakType
    {
        /// <summary>Play continues until one player captures a zone (first blood wins).</summary>
        SuddenDeath   = 0,
        /// <summary>First player to reach the original target plus a bonus wins.</summary>
        CaptureTarget = 1,
        /// <summary>Player who held zones for the most cumulative time wins.</summary>
        TimeAdvantage = 2,
    }
}
