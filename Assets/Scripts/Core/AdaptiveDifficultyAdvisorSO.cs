using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Reads the player's recent score trend (<see cref="ScoreHistorySO.TrendDelta"/>) and
    /// current win streak (<see cref="WinStreakSO.CurrentStreak"/>) to recommend one of the
    /// available difficulty presets from a <see cref="DifficultyPresetsConfig"/>.
    ///
    /// ── Suggestion logic ──────────────────────────────────────────────────────
    ///   • positive trend AND streak ≥ 3  → hardest preset  (last index in Presets)
    ///   • positive trend AND streak ≥ 1  → next-up preset  (second-to-last index)
    ///   • negative trend                 → easiest preset  (index 0)
    ///   • all other cases (no data)      → null (no suggestion)
    ///
    /// "No data" covers: null/empty Presets list, zero trend with zero streak, or
    /// positive trend with no win streak (streak == 0).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Both data sources are optional; null sources are treated as zero data.
    ///   - <see cref="GetSuggestion"/> and <see cref="GetSuggestionReason"/> are
    ///     pure read methods — they never mutate any SO at runtime.
    ///   - Preset entries with a null <c>.config</c> are forwarded as-is; the caller
    ///     should treat a null return as "no suggestion".
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ AdaptiveDifficultyAdvisor.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/AdaptiveDifficultyAdvisor",
        fileName = "AdaptiveDifficultyAdvisorSO")]
    public sealed class AdaptiveDifficultyAdvisorSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data Sources (optional)")]
        [Tooltip("Rolling score history SO. Provides TrendDelta (positive = improving). " +
                 "Leave null to treat trend as 0.")]
        [SerializeField] private ScoreHistorySO _scoreHistory;

        [Tooltip("Win streak SO. Provides CurrentStreak. " +
                 "Leave null to treat streak as 0.")]
        [SerializeField] private WinStreakSO _winStreak;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the suggested <see cref="BotDifficultyConfig"/> based on the current
        /// score trend and win streak, chosen from the presets in
        /// <paramref name="config"/>.
        ///
        /// Returns <c>null</c> when:
        ///   • <paramref name="config"/> is null.
        ///   • <paramref name="config.Presets"/> is null or empty.
        ///   • There is no data signal (trend == 0 and streak == 0).
        ///   • Trend is positive but streak is 0 (no active win streak to confirm the trend).
        /// </summary>
        /// <param name="config">The difficulty presets catalogue.  Index 0 = easiest, last index = hardest.</param>
        public BotDifficultyConfig GetSuggestion(DifficultyPresetsConfig config)
        {
            if (config == null || config.Presets == null || config.Presets.Count == 0)
                return null;

            int trend  = _scoreHistory != null ? _scoreHistory.TrendDelta    : 0;
            int streak = _winStreak    != null ? _winStreak.CurrentStreak : 0;

            // Dominant positive signal: improving trend + strong win streak → hardest.
            if (trend > 0 && streak >= 3)
                return config.Presets[config.Presets.Count - 1].config;

            // Moderate positive signal: improving trend + any win streak → next-up.
            if (trend > 0 && streak >= 1)
            {
                int nextUpIndex = Mathf.Max(0, config.Presets.Count - 2);
                return config.Presets[nextUpIndex].config;
            }

            // Negative trend → recommend easiest preset.
            if (trend < 0)
                return config.Presets[0].config;

            // No actionable data.
            return null;
        }

        /// <summary>
        /// Returns a human-readable explanation for the current difficulty suggestion.
        ///
        /// Returns <c>"No data"</c> when no suggestion can be made (same conditions as
        /// <see cref="GetSuggestion"/> returning null).
        /// </summary>
        /// <param name="config">The difficulty presets catalogue used to determine the suggestion.</param>
        public string GetSuggestionReason(DifficultyPresetsConfig config)
        {
            if (config == null || config.Presets == null || config.Presets.Count == 0)
                return "No data";

            int trend  = _scoreHistory != null ? _scoreHistory.TrendDelta    : 0;
            int streak = _winStreak    != null ? _winStreak.CurrentStreak : 0;

            if (trend > 0 && streak >= 3)
                return string.Format(
                    "Score trending up with a {0}-match win streak \u2014 try the hardest challenge!",
                    streak);

            if (trend > 0 && streak >= 1)
                return string.Format(
                    "Score trending up with a {0}-match win streak \u2014 step up the difficulty!",
                    streak);

            if (trend < 0)
                return "Score is trending down \u2014 try an easier opponent to rebuild momentum.";

            return "No data";
        }
    }
}
