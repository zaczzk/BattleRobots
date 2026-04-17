using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that accumulates per-match zone-capture and presence
    /// metrics for both the player and the bot in order to determine the match MVP.
    ///
    /// ── MVP Rule ───────────────────────────────────────────────────────────────
    ///   Primary criterion  — capture count: more captures → MVP.
    ///   Tiebreaker         — if capture counts are equal, more time in zones → MVP.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMVP.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMVP", order = 61)]
    public sealed class ZoneControlMVPSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after any metric is updated.")]
        [SerializeField] private VoidGameEvent _onMVPDataUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _playerCaptures;
        private int   _botCaptures;
        private float _playerPresenceTime;
        private float _botPresenceTime;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Zone captures recorded for the player this match.</summary>
        public int PlayerCaptures => _playerCaptures;

        /// <summary>Zone captures recorded for the bot this match.</summary>
        public int BotCaptures => _botCaptures;

        /// <summary>Total time (seconds) the player has spent in zones this match.</summary>
        public float PlayerPresenceTime => _playerPresenceTime;

        /// <summary>Total time (seconds) the bot has spent in zones this match.</summary>
        public float BotPresenceTime => _botPresenceTime;

        /// <summary>
        /// True when the player is assessed as the match MVP.
        /// Primary: most captures. Tiebreaker: most presence time.
        /// </summary>
        public bool IsPlayerMVP =>
            _playerCaptures != _botCaptures
                ? _playerCaptures > _botCaptures
                : _playerPresenceTime > _botPresenceTime;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Records one player zone capture and fires <c>_onMVPDataUpdated</c>.</summary>
        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            _onMVPDataUpdated?.Raise();
        }

        /// <summary>Records one bot zone capture and fires <c>_onMVPDataUpdated</c>.</summary>
        public void RecordBotCapture()
        {
            _botCaptures++;
            _onMVPDataUpdated?.Raise();
        }

        /// <summary>
        /// Adds <paramref name="dt"/> seconds to the player presence accumulator.
        /// No-op for values ≤ 0. Fires <c>_onMVPDataUpdated</c> on success.
        /// </summary>
        public void AddPlayerPresenceTime(float dt)
        {
            if (dt <= 0f) return;
            _playerPresenceTime += dt;
            _onMVPDataUpdated?.Raise();
        }

        /// <summary>
        /// Adds <paramref name="dt"/> seconds to the bot presence accumulator.
        /// No-op for values ≤ 0. Fires <c>_onMVPDataUpdated</c> on success.
        /// </summary>
        public void AddBotPresenceTime(float dt)
        {
            if (dt <= 0f) return;
            _botPresenceTime += dt;
            _onMVPDataUpdated?.Raise();
        }

        /// <summary>
        /// Clears all accumulators silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _playerCaptures    = 0;
            _botCaptures       = 0;
            _playerPresenceTime = 0f;
            _botPresenceTime    = 0f;
        }
    }
}
