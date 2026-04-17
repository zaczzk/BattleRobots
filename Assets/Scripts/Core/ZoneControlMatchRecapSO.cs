using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that builds a single-sentence match recap from the
    /// post-match SOs and exposes it as a formatted string.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="BuildRecap"/> at match end to populate the runtime fields
    ///   from <c>ZoneControlVictoryConditionSO</c>, <c>ZoneControlMVPSO</c>, and
    ///   <c>ZoneControlScoreboardSO</c>.  <see cref="GetRecapSummary"/> returns a
    ///   formatted summary string once the recap is built; empty string otherwise.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchRecap.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchRecap", order = 64)]
    public sealed class ZoneControlMatchRecapSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when BuildRecap completes successfully.")]
        [SerializeField] private VoidGameEvent _onRecapBuilt;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int                    _captureCount;
        private bool                   _mvpIsPlayer;
        private ZoneControlVictoryType _victoryType;
        private int                    _finalPlayerScore;
        private int                    _finalBotScore;
        private bool                   _isBuilt;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True after <see cref="BuildRecap"/> has been called successfully.</summary>
        public bool IsBuilt => _isBuilt;

        /// <summary>Zone captures recorded in the last built recap.</summary>
        public int CaptureCount => _captureCount;

        /// <summary>True when the player was MVP in the last built recap.</summary>
        public bool MvpIsPlayer => _mvpIsPlayer;

        /// <summary>Victory type recorded in the last built recap.</summary>
        public ZoneControlVictoryType VictoryType => _victoryType;

        /// <summary>Final player score recorded in the last built recap.</summary>
        public int FinalPlayerScore => _finalPlayerScore;

        /// <summary>Final bot score recorded in the last built recap.</summary>
        public int FinalBotScore => _finalBotScore;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the recap from <paramref name="victorySO"/>, <paramref name="mvpSO"/>,
        /// and <paramref name="scoreboardSO"/>.  Null SOs contribute zero/default values.
        /// Fires <c>_onRecapBuilt</c> on completion.
        /// </summary>
        public void BuildRecap(
            ZoneControlVictoryConditionSO victorySO,
            ZoneControlMVPSO             mvpSO,
            ZoneControlScoreboardSO      scoreboardSO)
        {
            _victoryType       = victorySO   != null ? victorySO.VictoryType    : ZoneControlVictoryType.FirstToCaptures;
            _captureCount      = mvpSO       != null ? mvpSO.PlayerCaptures     : 0;
            _mvpIsPlayer       = mvpSO       != null ? mvpSO.IsPlayerMVP        : false;
            _finalPlayerScore  = scoreboardSO != null ? scoreboardSO.PlayerScore : 0;
            _finalBotScore     = scoreboardSO != null ? scoreboardSO.GetBotScore(0) : 0;
            _isBuilt           = true;

            _onRecapBuilt?.Raise();
        }

        /// <summary>
        /// Returns a human-readable one-line recap string, or an empty string when
        /// the recap has not yet been built.  Zero allocation.
        /// </summary>
        public string GetRecapSummary()
        {
            if (!_isBuilt) return string.Empty;

            string mvpLabel     = _mvpIsPlayer ? "Player" : "Bot";
            string winTypeLabel = _victoryType == ZoneControlVictoryType.FirstToCaptures
                ? "First to Captures"
                : "Most Zones Held";

            return $"Captures: {_captureCount} | MVP: {mvpLabel} | Win: {winTypeLabel} | Score: {_finalPlayerScore}\u2013{_finalBotScore}";
        }

        /// <summary>
        /// Clears all recap state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _captureCount     = 0;
            _mvpIsPlayer      = false;
            _victoryType      = ZoneControlVictoryType.FirstToCaptures;
            _finalPlayerScore = 0;
            _finalBotScore    = 0;
            _isBuilt          = false;
        }
    }
}
