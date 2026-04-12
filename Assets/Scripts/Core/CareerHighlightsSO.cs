using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks the player's all-time single-match bests across four
    /// performance dimensions:
    ///   • Best damage dealt in a single match
    ///   • Fastest win (shortest winning match duration)
    ///   • Highest currency earned in a single match
    ///   • Longest match duration ever played
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   • <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> at startup
    ///     to restore persisted records from <see cref="SaveData.careerHighlights"/>.
    ///   • <see cref="MatchManager"/> calls <see cref="Update(MatchResultSO)"/> in EndMatch
    ///     after <see cref="MatchResultSO"/> is written and before _onMatchEnded fires,
    ///     so that UI subscribers already see updated IsNew* flags when the event arrives.
    ///   • <see cref="MatchManager"/> calls <see cref="TakeSnapshot"/> and stores the
    ///     result in <see cref="SaveData.careerHighlights"/> before SaveSystem.Save().
    ///   • <see cref="Reset"/> is silent — intended for testing; not called by game flow.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime fields are NOT serialized (SO asset stays immutable on disk).
    ///   - All mutators are null-safe and clamp negative inputs to zero.
    ///   - Transient IsNew* flags are reset at the start of each Update() call and
    ///     remain set until the next Update() invocation.
    ///   - <c>_onHighlightsUpdated</c> fires only when at least one record is broken.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ CareerHighlights.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/CareerHighlights",
        fileName = "CareerHighlightsSO")]
    public sealed class CareerHighlightsSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel — Out")]
        [Tooltip("Raised after Update() when at least one new category best is set. " +
                 "Use this to refresh UI panels or trigger a fanfare effect. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private VoidGameEvent _onHighlightsUpdated;

        // ── Runtime state (not serialized — reset each domain reload) ─────────

        private float _bestSingleMatchDamage;
        private float _fastestWinSeconds;       // 0 = never won
        private int   _bestSingleMatchCurrency;
        private float _longestMatchSeconds;

        // Transient flags — cleared at the start of Update(), set when a record is broken.
        private bool _isNewBestDamage;
        private bool _isNewFastestWin;
        private bool _isNewBestCurrency;
        private bool _isNewLongestMatch;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Highest damage dealt to the enemy in a single match. 0 = no match played.</summary>
        public float BestSingleMatchDamage   => _bestSingleMatchDamage;

        /// <summary>
        /// Fastest winning match duration in seconds.
        /// 0 means the player has never won a match.
        /// </summary>
        public float FastestWinSeconds       => _fastestWinSeconds;

        /// <summary>Highest currency earned in a single match. 0 = no match played.</summary>
        public int   BestSingleMatchCurrency => _bestSingleMatchCurrency;

        /// <summary>Longest match duration ever played in seconds. 0 = no match played.</summary>
        public float LongestMatchSeconds     => _longestMatchSeconds;

        /// <summary>True if the most recent <see cref="Update"/> set a new damage best.</summary>
        public bool IsNewBestDamage   => _isNewBestDamage;

        /// <summary>True if the most recent <see cref="Update"/> set a new fastest-win record.</summary>
        public bool IsNewFastestWin   => _isNewFastestWin;

        /// <summary>True if the most recent <see cref="Update"/> set a new currency best.</summary>
        public bool IsNewBestCurrency => _isNewBestCurrency;

        /// <summary>True if the most recent <see cref="Update"/> set a new longest-match record.</summary>
        public bool IsNewLongestMatch => _isNewLongestMatch;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Compares the completed match's stats against stored career bests and updates
        /// any category where a new record is set.
        ///
        /// Transient IsNew* flags are cleared at the start of this call, then set for each
        /// category that produces a new career best.  If at least one flag ends up true,
        /// <c>_onHighlightsUpdated</c> fires.
        ///
        /// Null <paramref name="result"/> is a safe no-op.
        /// </summary>
        public void Update(MatchResultSO result)
        {
            if (result == null) return;

            // Reset transient flags before evaluating this match.
            _isNewBestDamage   = false;
            _isNewFastestWin   = false;
            _isNewBestCurrency = false;
            _isNewLongestMatch = false;

            // Damage dealt — any match (win or loss).
            if (result.DamageDone > _bestSingleMatchDamage)
            {
                _bestSingleMatchDamage = result.DamageDone;
                _isNewBestDamage       = true;
            }

            // Fastest win — only on player victories with a positive duration.
            // First win always sets the record (_fastestWinSeconds == 0 acts as "no record").
            if (result.PlayerWon && result.DurationSeconds > 0f
                && (_fastestWinSeconds <= 0f || result.DurationSeconds < _fastestWinSeconds))
            {
                _fastestWinSeconds = result.DurationSeconds;
                _isNewFastestWin   = true;
            }

            // Highest currency earned — any match.
            if (result.CurrencyEarned > _bestSingleMatchCurrency)
            {
                _bestSingleMatchCurrency = result.CurrencyEarned;
                _isNewBestCurrency       = true;
            }

            // Longest match duration — any match.
            if (result.DurationSeconds > _longestMatchSeconds)
            {
                _longestMatchSeconds = result.DurationSeconds;
                _isNewLongestMatch   = true;
            }

            // Notify subscribers only when at least one record was broken.
            if (_isNewBestDamage || _isNewFastestWin || _isNewBestCurrency || _isNewLongestMatch)
                _onHighlightsUpdated?.Raise();
        }

        /// <summary>
        /// Rehydrates runtime fields from a persisted <see cref="CareerHighlightsSnapshot"/>.
        /// Does NOT fire any event — safe to call from <see cref="GameBootstrapper"/>.
        /// Null snapshot is a safe no-op (all fields remain at zero).
        /// Negative values are clamped to zero.
        /// </summary>
        public void LoadSnapshot(CareerHighlightsSnapshot snapshot)
        {
            if (snapshot == null) return;

            _bestSingleMatchDamage   = Mathf.Max(0f, snapshot.bestSingleMatchDamage);
            _fastestWinSeconds       = Mathf.Max(0f, snapshot.fastestWinSeconds);
            _bestSingleMatchCurrency = Mathf.Max(0,  snapshot.bestSingleMatchCurrency);
            _longestMatchSeconds     = Mathf.Max(0f, snapshot.longestMatchSeconds);

            // Transient flags are always false after a load — no match just completed.
            _isNewBestDamage   = false;
            _isNewFastestWin   = false;
            _isNewBestCurrency = false;
            _isNewLongestMatch = false;
        }

        /// <summary>
        /// Returns a snapshot of the current career bests for persistence.
        /// Call this just before <see cref="SaveSystem.Save"/> in the match-end flow.
        /// </summary>
        public CareerHighlightsSnapshot TakeSnapshot() =>
            new CareerHighlightsSnapshot
            {
                bestSingleMatchDamage   = _bestSingleMatchDamage,
                fastestWinSeconds       = _fastestWinSeconds,
                bestSingleMatchCurrency = _bestSingleMatchCurrency,
                longestMatchSeconds     = _longestMatchSeconds,
            };

        /// <summary>
        /// Clears all accumulated records and transient flags.
        /// Silent — does NOT fire <c>_onHighlightsUpdated</c>.
        /// Intended for testing; not called by normal game flow.
        /// </summary>
        public void Reset()
        {
            _bestSingleMatchDamage   = 0f;
            _fastestWinSeconds       = 0f;
            _bestSingleMatchCurrency = 0;
            _longestMatchSeconds     = 0f;
            _isNewBestDamage         = false;
            _isNewFastestWin         = false;
            _isNewBestCurrency       = false;
            _isNewLongestMatch       = false;
        }
    }
}
