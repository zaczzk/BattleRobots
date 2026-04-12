using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays a line-by-line breakdown of how the match score was calculated,
    /// giving players clear feedback on which factors contributed to their final score.
    ///
    /// ── Score formula displayed ────────────────────────────────────────────────
    ///   Base:           1 000 (win) or 100 (loss)
    ///   Time Bonus:     max(0, 600 − DurationSeconds × 5)  [wins only]
    ///   Damage Dealt:   floor(DamageDone × 2)
    ///   Damage Taken:   −floor(DamageTaken)
    ///   Bonus Credits:  BonusEarned × 3
    ///   ──────────────────────────────────────────────────────────
    ///   Total:          max(0, sum of above)
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add to any post-match panel alongside PostMatchController.
    ///   2. Assign _matchResult and _onMatchEnded (same SOs as PostMatchController).
    ///   3. Optionally assign _personalBest for authoritative total + new-best banner.
    ///   4. Assign individual Text fields for the rows you want to display.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace. References BattleRobots.Core only.
    ///   - No Physics references. No Update / FixedUpdate.
    ///   - All inspector fields are optional and null-safe.
    ///   - Delegate cached in Awake — zero alloc on subscribe / unsubscribe.
    ///   - String allocations in Refresh() run at most once per match (cold path).
    ///
    /// Create via GameObject ▶ Add Component ▶ MatchScoreBreakdownController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchScoreBreakdownController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Blackboard SO written by MatchManager before MatchEnded fires. " +
                 "Leave null to show fallback '—' text on all rows.")]
        [SerializeField] private MatchResultSO _matchResult;

        [Tooltip("Tracks the player's current and all-time best score. " +
                 "When assigned, _totalScoreText shows PersonalBestSO.CurrentScore " +
                 "(the authoritative value MatchManager submitted) rather than a local " +
                 "recalculation. The new-best banner and best-score label are also driven " +
                 "from this SO. Leave null to use the locally recalculated total.")]
        [SerializeField] private PersonalBestSO _personalBest;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by MatchManager when the round ends. " +
                 "Leave null if the breakdown panel is opened on demand rather than reacting to match end.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Score Components (optional)")]
        [Tooltip("Displays the base score: 'Base: 1000' (win) or 'Base: 100' (loss).")]
        [SerializeField] private Text _baseScoreText;

        [Tooltip("Displays the time bonus: 'Time Bonus: +300' for wins, 'Time Bonus: \u2014' for losses.")]
        [SerializeField] private Text _timeBonusText;

        [Tooltip("Displays the damage dealt contribution: 'Damage Dealt: +460'.")]
        [SerializeField] private Text _damageDealtText;

        [Tooltip("Displays the damage taken penalty: 'Damage Taken: -120'.")]
        [SerializeField] private Text _damageTakenText;

        [Tooltip("Displays performance-condition bonus credits: 'Bonus Credits: +525' or 'Bonus Credits: \u2014'.")]
        [SerializeField] private Text _bonusCreditsText;

        [Tooltip("Displays the final match score total: 'Score: 2165'.")]
        [SerializeField] private Text _totalScoreText;

        [Header("New Best (optional)")]
        [Tooltip("Activated when PersonalBestSO.IsNewBest is true after the match. " +
                 "Requires _personalBest to be assigned; otherwise always hidden.")]
        [SerializeField] private GameObject _newBestBanner;

        [Tooltip("Displays the all-time best score: 'Best: 2165'. " +
                 "Requires _personalBest to be assigned; skipped otherwise.")]
        [SerializeField] private Text _bestScoreText;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Recomputes all score-breakdown labels from <see cref="_matchResult"/> and
        /// <see cref="_personalBest"/>.
        /// Safe to call when any optional field is null (silently skipped).
        /// Allocates strings — call from event handlers only, not from Update.
        /// </summary>
        public void Refresh()
        {
            if (_matchResult == null)
            {
                ApplyFallback();
                return;
            }

            // ── Compute each score component (mirrors MatchScoreCalculator logic) ─────

            int baseScore = _matchResult.PlayerWon ? 1000 : 100;

            int timeBonus = 0;
            if (_matchResult.PlayerWon)
                timeBonus = Mathf.Max(0, Mathf.FloorToInt(600f - _matchResult.DurationSeconds * 5f));

            int damageDealt        = Mathf.FloorToInt(_matchResult.DamageDone  * 2f);
            int damageTakenPenalty = Mathf.FloorToInt(_matchResult.DamageTaken);
            int bonusCredits       = _matchResult.BonusEarned * 3;

            // Prefer PersonalBestSO.CurrentScore when available — it is the authoritative
            // total that MatchManager submitted just before MatchEnded fired.
            int total = _personalBest != null
                ? _personalBest.CurrentScore
                : Mathf.Max(0, baseScore + timeBonus + damageDealt - damageTakenPenalty + bonusCredits);

            // ── Populate text labels ──────────────────────────────────────────

            if (_baseScoreText != null)
                _baseScoreText.text = string.Format("Base: {0}", baseScore);

            if (_timeBonusText != null)
                _timeBonusText.text = timeBonus > 0
                    ? string.Format("Time Bonus: +{0}", timeBonus)
                    : "Time Bonus: \u2014";           // em dash — not applicable on a loss

            if (_damageDealtText != null)
                _damageDealtText.text = string.Format("Damage Dealt: +{0}", damageDealt);

            if (_damageTakenText != null)
                _damageTakenText.text = string.Format("Damage Taken: -{0}", damageTakenPenalty);

            if (_bonusCreditsText != null)
                _bonusCreditsText.text = bonusCredits > 0
                    ? string.Format("Bonus Credits: +{0}", bonusCredits)
                    : "Bonus Credits: \u2014";        // em dash — no conditions satisfied

            if (_totalScoreText != null)
                _totalScoreText.text = string.Format("Score: {0}", total);

            // ── New-best banner ───────────────────────────────────────────────

            bool isNewBest = _personalBest != null && _personalBest.IsNewBest;
            _newBestBanner?.SetActive(isNewBest);

            if (_bestScoreText != null && _personalBest != null)
                _bestScoreText.text = string.Format("Best: {0}", _personalBest.BestScore);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplyFallback()
        {
            const string dash = "\u2014";   // em dash

            if (_baseScoreText    != null) _baseScoreText.text    = "Base: "          + dash;
            if (_timeBonusText    != null) _timeBonusText.text    = "Time Bonus: "    + dash;
            if (_damageDealtText  != null) _damageDealtText.text  = "Damage Dealt: "  + dash;
            if (_damageTakenText  != null) _damageTakenText.text  = "Damage Taken: "  + dash;
            if (_bonusCreditsText != null) _bonusCreditsText.text = "Bonus Credits: " + dash;
            if (_totalScoreText   != null) _totalScoreText.text   = "Score: "         + dash;
            _newBestBanner?.SetActive(false);
        }
    }
}
