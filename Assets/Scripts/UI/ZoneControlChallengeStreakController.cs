using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlChallengeStreakSO"/>
    /// and displays streak progress and escalating rewards.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <c>_onChallengeComplete</c>: calls <c>RecordCompletion</c>, credits
    ///   the wallet with the earned reward, then refreshes.
    ///   <c>_onChallengeFailed</c>: calls <c>RecordFailure</c>, then refreshes.
    ///   <c>_onMatchStarted</c>: refreshes display (does not reset streak —
    ///   streak persists across matches by design).
    ///   <c>_onStreakIncreased/_onStreakBroken</c>: refreshes display.
    ///   <see cref="Refresh"/>: shows streak count, next reward, and total
    ///   rewards earned; hides panel when <c>_streakSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlChallengeStreakController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlChallengeStreakSO _streakSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onChallengeComplete;
        [SerializeField] private VoidGameEvent _onChallengeFailed;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onStreakIncreased;
        [SerializeField] private VoidGameEvent _onStreakBroken;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _rewardLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleCompleteDelegate;
        private Action _handleFailedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleCompleteDelegate = HandleChallengeComplete;
            _handleFailedDelegate   = HandleChallengeFailed;
            _refreshDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onChallengeComplete?.RegisterCallback(_handleCompleteDelegate);
            _onChallengeFailed?.RegisterCallback(_handleFailedDelegate);
            _onMatchStarted?.RegisterCallback(_refreshDelegate);
            _onStreakIncreased?.RegisterCallback(_refreshDelegate);
            _onStreakBroken?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onChallengeComplete?.UnregisterCallback(_handleCompleteDelegate);
            _onChallengeFailed?.UnregisterCallback(_handleFailedDelegate);
            _onMatchStarted?.UnregisterCallback(_refreshDelegate);
            _onStreakIncreased?.UnregisterCallback(_refreshDelegate);
            _onStreakBroken?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleChallengeComplete()
        {
            if (_streakSO == null) return;
            _streakSO.RecordCompletion();
            _wallet?.AddFunds(_streakSO.RewardBase + (_streakSO.StreakCount - 1) * _streakSO.RewardPerStreak);
            Refresh();
        }

        private void HandleChallengeFailed()
        {
            _streakSO?.RecordFailure();
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>Updates the challenge streak panel.</summary>
        public void Refresh()
        {
            if (_streakSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_streakSO.StreakCount}";

            if (_rewardLabel != null)
                _rewardLabel.text = $"Next: {_streakSO.GetCurrentReward()}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total: {_streakSO.TotalRewardsEarned}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        public ZoneControlChallengeStreakSO StreakSO => _streakSO;
        public PlayerWallet                 Wallet   => _wallet;
    }
}
