using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks head-to-head win/loss records between the player
    /// and the bot across all recorded matches, providing a running rivalry score.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="RecordResult"/> increments the appropriate win counter and
    ///   fires <c>_onRivalryUpdated</c>.
    ///   <see cref="RivalryDescription"/> returns a human-readable standing such
    ///   as "You lead 5-3", "Tied", or "Bot leads 4-2".
    ///   <see cref="WinsDelta"/> is positive when the player leads.
    ///   <see cref="Reset"/> clears state silently; called from <c>OnEnable</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on play-mode entry.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlRivalry.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRivalry", order = 73)]
    public sealed class ZoneControlRivalrySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each RecordResult call.")]
        [SerializeField] private VoidGameEvent _onRivalryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _playerWins;
        private int _botWins;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total matches the player has won.</summary>
        public int PlayerWins => _playerWins;

        /// <summary>Total matches the bot has won.</summary>
        public int BotWins    => _botWins;

        /// <summary>
        /// Signed win delta: positive when the player leads, negative when the bot leads.
        /// </summary>
        public int WinsDelta  => _playerWins - _botWins;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records the outcome of one match and fires <c>_onRivalryUpdated</c>.
        /// </summary>
        /// <param name="playerWon">True when the player won the match.</param>
        public void RecordResult(bool playerWon)
        {
            if (playerWon) _playerWins++;
            else           _botWins++;
            _onRivalryUpdated?.Raise();
        }

        /// <summary>
        /// Returns a human-readable rivalry standing:
        /// "You lead N-M", "Tied", or "Bot leads N-M".
        /// </summary>
        public string RivalryDescription()
        {
            int delta = WinsDelta;
            if (delta > 0) return $"You lead {_playerWins}-{_botWins}";
            if (delta < 0) return $"Bot leads {_botWins}-{_playerWins}";
            return "Tied";
        }

        /// <summary>
        /// Clears all runtime state silently.  Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _playerWins = 0;
            _botWins    = 0;
        }
    }
}
