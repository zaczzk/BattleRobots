using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// A single row in the <see cref="SeasonalEventUI"/> tier list.
    /// Displays the tier name, score threshold, reward amount, and whether the
    /// tier has been reached or already claimed.
    ///
    /// Scene setup:
    ///   Attach to a prefab that has child Text components for name/score/reward
    ///   and optional GameObjects for reached/claimed state indicators.
    ///   SeasonalEventUI will instantiate and configure rows via <see cref="Setup"/>.
    ///
    /// Architecture: BattleRobots.UI — no Physics references.
    /// </summary>
    public sealed class SeasonalEventTierRowUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Labels")]
        [SerializeField] private Text _tierNameLabel;
        [SerializeField] private Text _requiredScoreLabel;
        [SerializeField] private Text _rewardLabel;

        [Header("State Indicators")]
        [Tooltip("Shown (enabled) when the player's score has reached this tier's threshold.")]
        [SerializeField] private GameObject _reachedIndicator;

        [Tooltip("Shown (enabled) once the player has claimed this tier's reward.")]
        [SerializeField] private GameObject _claimedIndicator;

        [Header("Claim Button")]
        [Tooltip("Button shown when tier is reached but not yet claimed. " +
                 "Wire onClick → SeasonalEventUI.OnClaimTierClicked(tierIndex).")]
        [SerializeField] private Button _claimButton;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Configures this row for display.
        /// </summary>
        /// <param name="tierName">Player-facing tier label (e.g. "Gold").</param>
        /// <param name="requiredScore">Score threshold required to reach this tier.</param>
        /// <param name="rewardCurrency">Currency awarded on claim.</param>
        /// <param name="isReached">Whether the player's current score meets the threshold.</param>
        /// <param name="isClaimed">Whether the reward has already been claimed.</param>
        public void Setup(
            string tierName,
            int    requiredScore,
            int    rewardCurrency,
            bool   isReached,
            bool   isClaimed)
        {
            if (_tierNameLabel      != null) _tierNameLabel.text      = tierName;
            if (_requiredScoreLabel != null) _requiredScoreLabel.text = requiredScore.ToString();
            if (_rewardLabel        != null) _rewardLabel.text        = rewardCurrency.ToString();

            if (_reachedIndicator != null) _reachedIndicator.SetActive(isReached);
            if (_claimedIndicator != null) _claimedIndicator.SetActive(isClaimed);

            // Claim button: visible when reached but not yet claimed.
            if (_claimButton != null)
                _claimButton.gameObject.SetActive(isReached && !isClaimed);
        }

        // ── Testable properties ───────────────────────────────────────────────

        /// <summary>Text on the tier name label (testable without MonoBehaviour instantiation).</summary>
        public string TierNameText      => _tierNameLabel      != null ? _tierNameLabel.text      : string.Empty;

        /// <summary>Text on the required score label.</summary>
        public string RequiredScoreText => _requiredScoreLabel != null ? _requiredScoreLabel.text : string.Empty;

        /// <summary>Text on the reward label.</summary>
        public string RewardText        => _rewardLabel        != null ? _rewardLabel.text        : string.Empty;

        /// <summary>Whether the reached indicator is active.</summary>
        public bool IsReachedVisible => _reachedIndicator != null && _reachedIndicator.activeSelf;

        /// <summary>Whether the claimed indicator is active.</summary>
        public bool IsClaimedVisible => _claimedIndicator != null && _claimedIndicator.activeSelf;

        /// <summary>Whether the claim button is visible.</summary>
        public bool IsClaimButtonVisible => _claimButton != null && _claimButton.gameObject.activeSelf;
    }
}
