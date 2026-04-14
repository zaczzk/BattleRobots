using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Computes and displays a 1–5 star rating at the end of each match.
    ///
    /// ── Rating algorithm ──────────────────────────────────────────────────────
    ///   Stars start at 1 (baseline — you showed up) and increase by:
    ///     +1  Player won.
    ///     +1  Damage efficiency ≥ <see cref="_efficiencyThreshold"/>
    ///         (DamageDone / (DamageDone + DamageTaken)).
    ///     +1  <see cref="PersonalBestSO.IsNewBest"/> — the match score beat the
    ///         all-time personal best.
    ///     +1  Player won AND IsNewBest (dominant winning run).
    ///   Final rating is clamped to [1, 5].
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   MatchManager writes <see cref="MatchResultSO"/> and PersonalBestSO then raises
    ///   <c>_onMatchEnded</c>.  This controller subscribes that event, computes the
    ///   rating, and drives <c>_ratingLabel</c> + optional star <c>Image</c> array.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one rating panel per canvas.
    ///   - All inspector fields optional — assign only those present in the scene.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _matchResult         → shared MatchResultSO blackboard.
    ///   _personalBest        → shared PersonalBestSO (optional — omit if PB not tracked).
    ///   _onMatchEnded        → same VoidGameEvent as MatchManager raises at match end.
    ///   _ratingPanel         → root panel (starts inactive; shown after each match).
    ///   _ratingLabel         → Text child receiving "N / 5".
    ///   _starImages          → array of up to 5 Image components for star sprites.
    ///   _filledStarSprite    → sprite assigned to earned stars.
    ///   _emptyStarSprite     → sprite assigned to unearned stars.
    ///   _efficiencyThreshold → damage-efficiency cut-off for the +1 star (default 0.5).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchRatingController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Blackboard SO written by MatchManager before _onMatchEnded fires. " +
                 "Read for PlayerWon / DamageDone / DamageTaken.")]
        [SerializeField] private MatchResultSO _matchResult;

        [Tooltip("Personal best SO (optional). When assigned, IsNewBest drives the +1 PB star.")]
        [SerializeField] private PersonalBestSO _personalBest;

        // ── Inspector — Event ─────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager at the end of each match.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root rating panel. Hidden on OnEnable; shown after each match.")]
        [SerializeField] private GameObject _ratingPanel;

        [Tooltip("Text label that receives 'N / 5'.")]
        [SerializeField] private Text _ratingLabel;

        [Tooltip("Array of up to 5 Image components representing star slots (index 0 = 1st star).")]
        [SerializeField] private Image[] _starImages;

        [Tooltip("Sprite applied to earned (filled) star images.")]
        [SerializeField] private Sprite _filledStarSprite;

        [Tooltip("Sprite applied to unearned (empty) star images.")]
        [SerializeField] private Sprite _emptyStarSprite;

        // ── Inspector — Thresholds ────────────────────────────────────────────

        [Header("Thresholds")]
        [Tooltip("Minimum damage efficiency ratio (DamageDone / total damage) required " +
                 "to earn the efficiency star. Range [0, 1]; default 0.5.")]
        [SerializeField, Range(0f, 1f)] private float _efficiencyThreshold = 0.5f;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _onMatchEndedDelegate;
        private int    _currentStars;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchEndedDelegate = OnMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_onMatchEndedDelegate);
            _ratingPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_onMatchEndedDelegate);
            _ratingPanel?.SetActive(false);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void OnMatchEnded()
        {
            _currentStars = ComputeStars();
            RefreshDisplay();
        }

        /// <summary>
        /// Computes the star rating (1–5) from the current <see cref="MatchResultSO"/>
        /// and optional <see cref="PersonalBestSO"/>.
        ///
        /// Exposed as <c>internal</c> so EditMode tests can invoke it directly
        /// without triggering the full event pipeline.
        /// </summary>
        internal int ComputeStars()
        {
            int stars = 1; // baseline: player showed up

            if (_matchResult == null) return stars;

            bool  won        = _matchResult.PlayerWon;
            float done       = _matchResult.DamageDone;
            float taken      = _matchResult.DamageTaken;
            float total      = done + taken;
            float efficiency = total > 0f ? done / total : 0f;
            bool  isNewBest  = _personalBest != null && _personalBest.IsNewBest;

            if (won)                    stars++;
            if (efficiency >= _efficiencyThreshold) stars++;
            if (isNewBest)              stars++;
            if (won && isNewBest)       stars++; // bonus: dominant winning run

            return Mathf.Clamp(stars, 1, 5);
        }

        private void RefreshDisplay()
        {
            _ratingPanel?.SetActive(true);

            if (_ratingLabel != null)
                _ratingLabel.text = string.Format("{0} / 5", _currentStars);

            if (_starImages == null) return;

            for (int i = 0; i < _starImages.Length && i < 5; i++)
            {
                Image img = _starImages[i];
                if (img == null) continue;

                bool filled = i < _currentStars;
                if      (filled  && _filledStarSprite != null) img.sprite = _filledStarSprite;
                else if (!filled && _emptyStarSprite  != null) img.sprite = _emptyStarSprite;
                img.enabled = true;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchResultSO"/>. May be null.</summary>
        public MatchResultSO MatchResult => _matchResult;

        /// <summary>The assigned <see cref="PersonalBestSO"/>. May be null.</summary>
        public PersonalBestSO PersonalBest => _personalBest;

        /// <summary>Damage-efficiency threshold for the efficiency star (default 0.5).</summary>
        public float EfficiencyThreshold => _efficiencyThreshold;

        /// <summary>Star rating computed after the most recent match (0 before any match).</summary>
        public int CurrentStars => _currentStars;
    }
}
