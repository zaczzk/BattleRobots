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

        /// <summary>
        /// Total bonus currency earned from <see cref="BonusConditionSO"/> conditions
        /// evaluated at match end.  This amount is already included in
        /// <see cref="CurrencyEarned"/>; it is stored separately so the post-match UI
        /// can display "Bonus: +N" without re-evaluating conditions.
        /// Zero when no <see cref="MatchBonusCatalogSO"/> is assigned to MatchManager,
        /// or when no conditions were satisfied.
        /// </summary>
        public int BonusEarned { get; private set; }

        // ── M7 Progression fields (T114) ─────────────────────────────────────

        /// <summary>
        /// XP awarded to the player this match, as computed by
        /// <see cref="XPRewardCalculator.CalculateMatchXP"/>.
        /// Zero when <see cref="MatchManager"/> has no <c>_playerProgression</c> assigned,
        /// or when using a caller that does not pass the optional parameter.
        /// </summary>
        public int XPEarned { get; private set; }

        /// <summary>
        /// True when the player levelled up at least once during this match's XP award.
        /// False when no <c>_playerProgression</c> is assigned, or when the XP earned
        /// was not enough to cross the next level threshold.
        /// </summary>
        public bool LeveledUp { get; private set; }

        /// <summary>
        /// The player's level after XP was applied.
        /// Defaults to 1 when no <c>_playerProgression</c> is assigned.
        /// </summary>
        public int NewLevel { get; private set; }

        // ── Mutator — called by MatchManager only ─────────────────────────────

        /// <summary>
        /// Overwrite all fields with fresh match data.
        /// Must be called <b>before</b> raising the MatchEnded VoidGameEvent
        /// so that subscribers read up-to-date values.
        ///
        /// The <paramref name="damageDone"/>, <paramref name="damageTaken"/>,
        /// <paramref name="bonusEarned"/>, <paramref name="xpEarned"/>,
        /// <paramref name="leveledUp"/>, and <paramref name="newLevel"/> parameters
        /// are optional (default 0/false/1) for backwards compatibility with callers
        /// that do not yet track those values.
        /// </summary>
        public void Write(bool playerWon, float durationSeconds, int currencyEarned, int newWalletBalance,
                          float damageDone = 0f, float damageTaken = 0f, int bonusEarned = 0,
                          int xpEarned = 0, bool leveledUp = false, int newLevel = 1)
        {
            PlayerWon        = playerWon;
            DurationSeconds  = durationSeconds;
            CurrencyEarned   = currencyEarned;
            NewWalletBalance = newWalletBalance;
            DamageDone       = damageDone;
            DamageTaken      = damageTaken;
            BonusEarned      = bonusEarned;
            XPEarned         = xpEarned;
            LeveledUp        = leveledUp;
            NewLevel         = newLevel;
        }
    }
}
