using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives the daily-challenge display panel in any scene.
    ///
    /// Reads <see cref="BattleRobots.Core.DailyChallengeSO"/> on <see cref="OnEnable"/>
    /// and again whenever <c>_onChallengeCompleted</c> fires, so the "Completed" badge
    /// activates without a scene reload.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace; no Physics namespace references.
    ///   • All UI fields are optional — null references are skipped silently.
    ///   • No Update; no heap allocations after Awake.
    ///   • Subscribes to a <see cref="BattleRobots.Core.VoidGameEvent"/> channel
    ///     (the same SO wired inside DailyChallengeSO._onChallengeCompleted) rather
    ///     than polling the SO directly.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to the Canvas panel that shows the daily challenge.
    ///   2. Assign _dailyChallenge (required for anything to display).
    ///   3. Assign _onChallengeCompleted — the same VoidGameEvent SO that is referenced
    ///      inside DailyChallengeSO._onChallengeCompleted.
    ///   4. Optionally assign _config, _titleText, _descriptionText, _rewardText,
    ///      _completedBadge.
    /// </summary>
    public sealed class DailyChallengeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO that holds the current challenge and completion state.")]
        [SerializeField] private BattleRobots.Core.DailyChallengeSO _dailyChallenge;

        [Tooltip("Config SO used to compute the display reward amount " +
                 "(BonusAmount × RewardMultiplier). Leave null to use multiplier 2.")]
        [SerializeField] private BattleRobots.Core.DailyChallengeConfig _config;

        [Header("Event Channel")]
        [Tooltip("VoidGameEvent fired by DailyChallengeSO.MarkCompleted(). " +
                 "Assign the same SO that is wired into DailyChallengeSO._onChallengeCompleted " +
                 "so this controller refreshes when the player completes the challenge.")]
        [SerializeField] private BattleRobots.Core.VoidGameEvent _onChallengeCompleted;

        [Header("UI (optional)")]
        [Tooltip("Text showing the challenge name (DisplayName or 'Daily Challenge' fallback).")]
        [SerializeField] private Text _titleText;

        [Tooltip("Text showing the challenge description.")]
        [SerializeField] private Text _descriptionText;

        [Tooltip("Text showing the reward amount, e.g. 'Reward: +200 credits'.")]
        [SerializeField] private Text _rewardText;

        [Tooltip("GameObject shown (SetActive true) when the challenge has been completed today.")]
        [SerializeField] private GameObject _completedBadge;

        // ── Cached delegate ───────────────────────────────────────────────────

        private System.Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onChallengeCompleted?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onChallengeCompleted?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads <see cref="BattleRobots.Core.DailyChallengeSO"/> and updates all
        /// optional UI fields.  Called automatically on <see cref="OnEnable"/> and
        /// whenever <c>_onChallengeCompleted</c> fires.
        /// </summary>
        public void Refresh()
        {
            if (_dailyChallenge == null) return;

            var challenge = _dailyChallenge.CurrentChallenge;
            if (challenge == null) return;

            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrEmpty(challenge.DisplayName)
                    ? "Daily Challenge"
                    : challenge.DisplayName;
            }

            if (_descriptionText != null)
                _descriptionText.text = challenge.DisplayDescription;

            if (_rewardText != null)
            {
                float multiplier = _config != null ? _config.RewardMultiplier : 2f;
                int   reward     = Mathf.RoundToInt(challenge.BonusAmount * multiplier);
                _rewardText.text = $"Reward: +{reward} credits";
            }

            if (_completedBadge != null)
                _completedBadge.SetActive(_dailyChallenge.IsCompleted);
        }
    }
}
