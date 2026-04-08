using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the current daily challenge: description, progress bar, progress
    /// fraction text, reward amount, and a claimable reward button.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Assign <c>_progress</c> and <c>_wallet</c> SO references in the Inspector.
    ///   2. Wire child UI elements (_descriptionLabel, _progressLabel, _progressBar,
    ///      _rewardLabel, _claimButton) to their respective Text/Slider/Button GameObjects.
    ///   3. Connect <c>_claimButton.onClick</c> to <b>DailyChallengeUI.OnClaimClicked</b>.
    ///   4. (Optional) Add a <c>FloatGameEventListener</c> on the same GO wired to
    ///      <c>DailyChallengeProgressSO._onProgressChanged</c> →
    ///      <b>DailyChallengeUI.OnProgressChanged(float)</b> for real-time bar updates
    ///      after each match without opening the panel.
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────
    ///   • <c>BattleRobots.UI</c> namespace — no Physics references.
    ///   • No Update/FixedUpdate — panel rebuilds on <c>OnEnable</c>.
    ///   • <c>OnProgressChanged</c> is a public callback for FloatGameEventListener.
    ///
    /// </summary>
    [AddComponentMenu("BattleRobots/UI/Daily Challenge UI")]
    public sealed class DailyChallengeUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("DailyChallengeProgressSO runtime asset tracking today's challenge.")]
        [SerializeField] private DailyChallengeProgressSO _progress;

        [Tooltip("PlayerWallet SO — used by ClaimReward to credit the reward currency.")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("UI Elements")]
        [Tooltip("Text label showing the challenge description.")]
        [SerializeField] private Text _descriptionLabel;

        [Tooltip("Text label showing 'currentProgress / targetValue'.")]
        [SerializeField] private Text _progressLabel;

        [Tooltip("Slider representing progress fraction [0..1].")]
        [SerializeField] private Slider _progressBar;

        [Tooltip("Text label showing the reward amount, e.g. '+200'.")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("Button the player taps to claim a completed challenge reward.")]
        [SerializeField] private Button _claimButton;

        [Tooltip("Optional panel shown only when the challenge has been completed " +
                 "and the reward is still unclaimed (e.g. a 'Reward ready!' banner).")]
        [SerializeField] private GameObject _rewardReadyPanel;

        // ── Unity messages ────────────────────────────────────────────────────

        private void OnEnable() => Refresh();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all UI elements from the current <see cref="DailyChallengeProgressSO"/>
        /// state. Called automatically on <c>OnEnable</c>. May also be called directly
        /// from a FloatGameEventListener or VoidGameEventListener response.
        /// </summary>
        public void Refresh()
        {
            if (_progress == null)
            {
                SetPanelActive(false);
                return;
            }

            DailyChallengeDefinitionSO def = _progress.ActiveChallenge;

            if (def == null)
            {
                SetPanelActive(false);
                return;
            }

            SetPanelActive(true);

            if (_descriptionLabel != null)
                _descriptionLabel.text = def.Description;

            if (_rewardLabel != null)
                _rewardLabel.text = $"+{def.RewardCurrency}";

            float fraction = _progress.ProgressFraction;

            if (_progressBar != null)
                _progressBar.value = fraction;

            if (_progressLabel != null)
                _progressLabel.text = $"{_progress.Progress:F0} / {def.TargetValue:F0}";

            bool rewardReady = _progress.IsCompleted && !_progress.IsRewardClaimed;

            if (_claimButton != null)
                _claimButton.interactable = rewardReady;

            if (_rewardReadyPanel != null)
                _rewardReadyPanel.SetActive(rewardReady);
        }

        /// <summary>
        /// FloatGameEventListener callback — updates the progress bar and label
        /// in real time when a match finishes.
        /// <paramref name="fraction"/> is the normalised progress value [0..1].
        /// </summary>
        public void OnProgressChanged(float fraction)
        {
            if (_progressBar != null)
                _progressBar.value = fraction;

            if (_progressLabel != null && _progress != null && _progress.ActiveChallenge != null)
                _progressLabel.text =
                    $"{_progress.Progress:F0} / {_progress.ActiveChallenge.TargetValue:F0}";

            // Check if the challenge just completed so the claim button activates.
            if (_claimButton != null)
                _claimButton.interactable = _progress != null
                                            && _progress.IsCompleted
                                            && !_progress.IsRewardClaimed;

            if (_rewardReadyPanel != null && _progress != null)
                _rewardReadyPanel.SetActive(_progress.IsCompleted && !_progress.IsRewardClaimed);
        }

        /// <summary>
        /// Connected to <c>_claimButton.onClick</c>.
        /// Calls <see cref="DailyChallengeProgressSO.ClaimReward"/> then refreshes the panel.
        /// </summary>
        public void OnClaimClicked()
        {
            _progress?.ClaimReward(_wallet);
            Refresh();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetPanelActive(bool active)
        {
            // If this component's GO is inside a panel root, the caller can hide
            // the whole panel; we fall back to disabling child Text elements.
            if (_descriptionLabel != null) _descriptionLabel.gameObject.SetActive(active);
            if (_progressLabel    != null) _progressLabel.gameObject.SetActive(active);
            if (_progressBar      != null) _progressBar.gameObject.SetActive(active);
            if (_rewardLabel      != null) _rewardLabel.gameObject.SetActive(active);
            if (_claimButton      != null) _claimButton.gameObject.SetActive(active);
        }
    }
}
