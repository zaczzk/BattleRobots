using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's current robot build tier as a coloured badge in the
    /// pre-match UI, refreshing automatically whenever the build rating changes.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────────
    ///   1. OnEnable subscribes to <c>_onRatingChanged</c> (IntGameEvent) and
    ///      calls Refresh().
    ///   2. Refresh() calls <see cref="RobotTierEvaluator.EvaluateTier"/> with the
    ///      current <see cref="BuildRatingSO.CurrentRating"/>.
    ///   3. <c>_tierLabel</c> is set to the tier's display name.
    ///   4. <c>_tierBadgeImage</c> and <c>_tierBackground</c> are tinted with the
    ///      tier's configured colour (white fallback when no config assigned).
    ///   5. OnDisable unsubscribes so no callbacks leak when the panel is hidden.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All inspector fields are optional; null dependencies are handled safely.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Refresh delegate cached in Awake; zero heap alloc after Awake.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────────
    ///   • _buildRating      → BuildRatingSO asset (same as BuildRatingController)
    ///   • _tierConfig       → RobotTierConfig SO asset
    ///   • _onRatingChanged  → BuildRatingSO._onRatingChanged IntGameEvent channel
    ///   • _tierLabel        → Text label (optional, shows tier name)
    ///   • _tierBadgeImage   → Image tinted by tier colour (optional)
    ///   • _tierBackground   → secondary Image tinted by tier colour (optional)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TierBadgeController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO that stores the current build-power rating. "
               + "Required — leave null to suppress badge updates entirely.")]
        [SerializeField] private BuildRatingSO _buildRating;

        [Tooltip("Tier-threshold config that maps rating values to tier levels. "
               + "Optional — when null the raw enum name is used as the label "
               + "and the badge tint defaults to white.")]
        [SerializeField] private RobotTierConfig _tierConfig;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same IntGameEvent as BuildRatingSO._onRatingChanged.  "
               + "Triggers Refresh() on every rating update.")]
        [SerializeField] private IntGameEvent _onRatingChanged;

        // ── Inspector — UI Refs (all optional) ────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Text label updated to the tier display name (e.g. 'GOLD') on each refresh.")]
        [SerializeField] private Text _tierLabel;

        [Tooltip("Primary badge Image whose colour is set to the tier tint colour.")]
        [SerializeField] private Image _tierBadgeImage;

        [Tooltip("Secondary background Image also tinted by the tier colour.")]
        [SerializeField] private Image _tierBackground;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action<int> _refreshDelegate;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = _ => Refresh();
        }

        private void OnEnable()
        {
            _onRatingChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onRatingChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_buildRating == null)
                return;

            RobotTierLevel tier = RobotTierEvaluator.EvaluateTier(_buildRating, _tierConfig);

            string displayName = _tierConfig != null
                ? _tierConfig.GetDisplayName(tier)
                : tier.ToString();

            Color color = _tierConfig != null
                ? _tierConfig.GetTierColor(tier)
                : Color.white;

            if (_tierLabel != null)
                _tierLabel.text = displayName;

            if (_tierBadgeImage != null)
                _tierBadgeImage.color = color;

            if (_tierBackground != null)
                _tierBackground.color = color;
        }
    }
}
