using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Defines the win condition for a zone-control match.
    ///
    /// ── Victory Types ──────────────────────────────────────────────────────────
    ///   <c>FirstToCaptures</c>  — player wins when their capture count reaches
    ///     <see cref="CaptureTarget"/>.
    ///   <c>MostZonesHeld</c>    — match ends when <see cref="TimeLimitSeconds"/>
    ///     elapses; most captures wins (evaluation delegated to match manager).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — SO is immutable during play.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlVictoryCondition.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlVictoryCondition", order = 60)]
    public sealed class ZoneControlVictoryConditionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("How the match victory is determined.")]
        [SerializeField] private ZoneControlVictoryType _victoryType = ZoneControlVictoryType.FirstToCaptures;

        [Tooltip("Capture count the player must reach (FirstToCaptures mode).")]
        [Min(1)]
        [SerializeField] private int _captureTarget = 10;

        [Tooltip("Seconds until the match ends (MostZonesHeld mode).")]
        [Min(10f)]
        [SerializeField] private float _timeLimitSeconds = 120f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by RaiseVictory() when victory is achieved.")]
        [SerializeField] private VoidGameEvent _onVictoryAchieved;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The victory rule applied to this match.</summary>
        public ZoneControlVictoryType VictoryType => _victoryType;

        /// <summary>Capture count the player must reach in FirstToCaptures mode.</summary>
        public int CaptureTarget => _captureTarget;

        /// <summary>Time limit in seconds for MostZonesHeld mode.</summary>
        public float TimeLimitSeconds => _timeLimitSeconds;

        /// <summary>Human-readable description of the active victory condition.</summary>
        public string VictoryDescription =>
            _victoryType == ZoneControlVictoryType.FirstToCaptures
                ? $"First to {_captureTarget} captures"
                : $"Most zones in {_timeLimitSeconds:F0}s";

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the supplied state satisfies the victory condition.
        /// Zero allocation.
        /// </summary>
        /// <param name="playerCaptures">Player's current capture count.</param>
        /// <param name="timeElapsed">Seconds elapsed since match start.</param>
        public bool IsVictoryMet(int playerCaptures, float timeElapsed)
        {
            return _victoryType == ZoneControlVictoryType.FirstToCaptures
                ? playerCaptures >= _captureTarget
                : timeElapsed >= _timeLimitSeconds;
        }

        /// <summary>Fires <c>_onVictoryAchieved</c>. Null-safe.</summary>
        public void RaiseVictory() => _onVictoryAchieved?.Raise();

        /// <summary>No-op placeholder kept for API symmetry.</summary>
        public void Reset() { }

        private void OnValidate()
        {
            _captureTarget    = Mathf.Max(1, _captureTarget);
            _timeLimitSeconds = Mathf.Max(10f, _timeLimitSeconds);
        }
    }

    /// <summary>Determines how zone-control victory is evaluated.</summary>
    public enum ZoneControlVictoryType
    {
        /// <summary>Win when capture count reaches the target.</summary>
        FirstToCaptures = 0,
        /// <summary>Win by having the most captures when time expires.</summary>
        MostZonesHeld   = 1,
    }
}
