using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Blackboard ScriptableObject written by <see cref="MatchManager"/> at the end of
    /// every match (before <c>_onMatchEnded</c> fires) and read by
    /// <see cref="BattleRobots.UI.PostMatchController"/> to populate the results screen.
    ///
    /// ── Why a blackboard SO? ───────────────────────────────────────────────────
    ///   <c>MatchEnded</c> is a <see cref="VoidGameEvent"/> (no payload). Attaching result
    ///   data to a typed GameEvent would require a new concrete event type.
    ///   Writing to a SO blackboard keeps the event channel parameter-less while
    ///   still letting any subscriber read structured result data.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - <c>Write()</c> is the only mutation path; called exclusively by MatchManager.
    ///   - Treat all public fields as read-only from outside MatchManager.
    ///   - DO NOT persist this SO — it is reset each match.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Core ▶ MatchResultSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/MatchResultSO", order = 5)]
    public sealed class MatchResultSO : ScriptableObject
    {
        // ── Populated by MatchManager.EndMatch() ──────────────────────────────

        /// <summary>True when the local player was the winner.</summary>
        public bool PlayerWon { get; private set; }

        /// <summary>How long the round lasted in seconds.</summary>
        public float DurationSeconds { get; private set; }

        /// <summary>Currency awarded this match.</summary>
        public int CurrencyEarned { get; private set; }

        /// <summary>Wallet balance after rewards are applied.</summary>
        public int NewWalletBalance { get; private set; }

        /// <summary>
        /// Total damage the player dealt to the enemy this match.
        /// Populated from <see cref="MatchStatisticsSO.TotalDamageDealt"/> when a
        /// MatchStatisticsSO is assigned to MatchManager; otherwise approximated from
        /// the enemy's health-difference at match end.
        /// </summary>
        public float DamageDone { get; private set; }

        /// <summary>
        /// Total damage the player received from the enemy this match.
        /// Populated from <see cref="MatchStatisticsSO.TotalDamageTaken"/> when a
        /// MatchStatisticsSO is assigned to MatchManager; otherwise approximated from
        /// the player's health-difference at match end.
        /// </summary>
        public float DamageTaken { get; private set; }

        // ── Mutator — called by MatchManager only ─────────────────────────────

        /// <summary>
        /// Overwrite all fields with fresh match data.
        /// Must be called <b>before</b> raising the MatchEnded VoidGameEvent
        /// so that subscribers read up-to-date values.
        ///
        /// The <paramref name="damageDone"/> and <paramref name="damageTaken"/>
        /// parameters are optional (default 0) for backwards compatibility with
        /// callers that do not yet track damage statistics.
        /// </summary>
        public void Write(bool playerWon, float durationSeconds, int currencyEarned, int newWalletBalance,
                          float damageDone = 0f, float damageTaken = 0f)
        {
            PlayerWon        = playerWon;
            DurationSeconds  = durationSeconds;
            CurrencyEarned   = currencyEarned;
            NewWalletBalance = newWalletBalance;
            DamageDone       = damageDone;
            DamageTaken      = damageTaken;
        }
    }
}
