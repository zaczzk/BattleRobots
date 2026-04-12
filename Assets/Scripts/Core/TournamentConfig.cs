using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable balance configuration for the multi-round tournament mode.
    ///
    /// ── Fields ────────────────────────────────────────────────────────────────
    ///   <see cref="RoundCount"/>      — number of rounds needed to win the tournament (default 3).
    ///   <see cref="EntryFee"/>        — credits deducted when a tournament is entered (default 100).
    ///   <see cref="RoundWinBonus"/>   — credits awarded after each individual round win (default 50).
    ///   <see cref="GrandPrize"/>      — credits awarded on full tournament victory (default 500).
    ///   <see cref="ConsolationPrize"/>— credits awarded on elimination, so the player is never
    ///                                    left worse off than they started (default 0).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO asset is treated as immutable at runtime — never mutated after load.
    ///   - <see cref="OnValidate"/> clamps negative values and emits designer warnings.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TournamentConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/TournamentConfig", fileName = "TournamentConfig", order = 20)]
    public sealed class TournamentConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Number of rounds the player must win to claim the grand prize. Minimum 1.")]
        [SerializeField, Min(1)] private int _roundCount = 3;

        [Tooltip("Credits deducted from the player's wallet when they enter a tournament. " +
                 "Set to 0 for free entry. Must be less than GrandPrize to ensure a net gain on victory.")]
        [SerializeField, Min(0)] private int _entryFee = 100;

        [Tooltip("Credits awarded to the player after each individual round win, " +
                 "before the grand prize is evaluated. Cumulative across all rounds.")]
        [SerializeField, Min(0)] private int _roundWinBonus = 50;

        [Tooltip("One-time bonus credited when the player wins all tournament rounds.")]
        [SerializeField, Min(0)] private int _grandPrize = 500;

        [Tooltip("Credits returned to the player upon elimination, so they always receive " +
                 "at least some compensation. Set to 0 for no consolation (default).")]
        [SerializeField, Min(0)] private int _consolationPrize = 0;

        [Header("Tier Gating (optional)")]
        [Tooltip("Minimum tier the player's current build must achieve before entry is allowed. " +
                 "Unranked (0) disables tier gating — any tier may enter.")]
        [SerializeField] private RobotTierLevel _requiredTier = RobotTierLevel.Unranked;

        [Tooltip("Minimum build-power rating required to enter this tournament. " +
                 "Set to 0 to disable rating gating.")]
        [SerializeField, Min(0)] private int _minRating = 0;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>Number of rounds the player must win (≥ 1).</summary>
        public int RoundCount => _roundCount;

        /// <summary>Credits deducted on tournament entry (≥ 0).</summary>
        public int EntryFee => _entryFee;

        /// <summary>Credits awarded per round win (≥ 0).</summary>
        public int RoundWinBonus => _roundWinBonus;

        /// <summary>One-time bonus on full tournament victory (≥ 0).</summary>
        public int GrandPrize => _grandPrize;

        /// <summary>Credits given on elimination (≥ 0, default 0).</summary>
        public int ConsolationPrize => _consolationPrize;

        /// <summary>
        /// Minimum <see cref="RobotTierLevel"/> the player's build must reach to enter.
        /// <see cref="RobotTierLevel.Unranked"/> (the default) disables tier gating.
        /// </summary>
        public RobotTierLevel RequiredTier => _requiredTier;

        /// <summary>
        /// Minimum build-power rating required to enter (≥ 0, default 0).
        /// 0 disables rating gating.
        /// </summary>
        public int MinRating => _minRating;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_roundCount < 1)
            {
                _roundCount = 1;
                Debug.LogWarning("[TournamentConfig] RoundCount clamped to minimum 1.");
            }

            if (_entryFee < 0)
            {
                _entryFee = 0;
                Debug.LogWarning("[TournamentConfig] EntryFee clamped to 0.");
            }

            if (_roundWinBonus < 0)
            {
                _roundWinBonus = 0;
                Debug.LogWarning("[TournamentConfig] RoundWinBonus clamped to 0.");
            }

            if (_grandPrize < 0)
            {
                _grandPrize = 0;
                Debug.LogWarning("[TournamentConfig] GrandPrize clamped to 0.");
            }

            if (_consolationPrize < 0)
            {
                _consolationPrize = 0;
                Debug.LogWarning("[TournamentConfig] ConsolationPrize clamped to 0.");
            }

            if (_entryFee > _grandPrize && _grandPrize > 0)
                Debug.LogWarning("[TournamentConfig] EntryFee exceeds GrandPrize — players lose " +
                                 "more entering than they can win. Consider raising GrandPrize.");
        }
#endif
    }
}
