using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that manages the daily challenge lifecycle: refreshing the challenge
    /// at session start and evaluating it after every match.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────
    ///   1. Awake  : calls <see cref="DailyChallengeSO.RefreshIfNeeded"/> so the current
    ///              session always has a valid challenge (new-day selection or same-day
    ///              restore from the saved pool index).
    ///   2. OnEnable  : subscribes HandleMatchEnded to <c>_onMatchEnded</c>.
    ///   3. HandleMatchEnded: reads <see cref="MatchResultSO"/> stats, evaluates the
    ///              challenge condition via
    ///              <see cref="MatchEndBonusEvaluator.EvaluateCondition"/>, and on
    ///              success credits the wallet, calls
    ///              <see cref="DailyChallengeSO.MarkCompleted"/>, optionally enqueues a
    ///              toast, and persists the completion flag.
    ///   4. OnDisable : unsubscribes HandleMatchEnded.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace; no Physics / UI namespace references.
    ///   • HandleMatchEnded is idempotent — once completed, further match-end events
    ///     have no effect (guarded by <see cref="DailyChallengeSO.IsCompleted"/>).
    ///   • All optional fields are null-safe; leave unassigned to skip that behaviour.
    ///   • Cold path only: no Update, no FixedUpdate, no heap allocations at runtime
    ///     (the notification string allocates, but only on the cold success path).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Create DailyChallengeSO and DailyChallengeConfig SO assets.
    ///   2. Add this MB to the Bootstrap or Arena scene.
    ///   3. Assign _dailyChallenge, _config, _matchResult, _onMatchEnded, _playerWallet.
    ///   4. Assign the same DailyChallengeSO to GameBootstrapper._dailyChallenge so it
    ///      is rehydrated at startup before this Awake runs.
    ///   5. Optionally assign _notificationQueue.
    /// </summary>
    public sealed class DailyChallengeManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Daily Challenge")]
        [Tooltip("Runtime SO storing the current challenge and completion state.")]
        [SerializeField] private DailyChallengeSO _dailyChallenge;

        [Tooltip("Config SO defining the challenge pool and reward multiplier.")]
        [SerializeField] private DailyChallengeConfig _config;

        [Header("Match")]
        [Tooltip("Blackboard SO written by MatchManager before MatchEnded fires.")]
        [SerializeField] private MatchResultSO _matchResult;

        [Tooltip("VoidGameEvent raised by MatchManager when a match ends.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Economy")]
        [Tooltip("PlayerWallet credited when the daily challenge is completed.")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Notifications (optional)")]
        [Tooltip("When assigned, a toast notification is enqueued on challenge completion. " +
                 "Leave null to skip.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        // ── Cached delegate ───────────────────────────────────────────────────

        private System.Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;

            // Resolve the challenge for this session: new-day → pick; same-day → restore.
            _dailyChallenge?.RefreshIfNeeded(_config);
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        // ── Match-end handler ─────────────────────────────────────────────────

        private void HandleMatchEnded()
        {
            // Guard: no challenge SO or already completed today.
            if (_dailyChallenge == null || _dailyChallenge.IsCompleted) return;

            var challenge = _dailyChallenge.CurrentChallenge;
            if (challenge == null) return;

            // Need match result data to evaluate the condition.
            if (_matchResult == null) return;

            bool satisfied = MatchEndBonusEvaluator.EvaluateCondition(
                challenge,
                _matchResult.PlayerWon,
                _matchResult.DurationSeconds,
                _matchResult.DamageDone,
                _matchResult.DamageTaken);

            if (!satisfied) return;

            // Condition met — award daily-challenge bonus.
            float multiplier = _config != null ? _config.RewardMultiplier : 2f;
            int reward = Mathf.RoundToInt(challenge.BonusAmount * multiplier);

            _playerWallet?.AddFunds(reward);

            // Mark completed (fires _onChallengeCompleted; idempotent internally).
            _dailyChallenge.MarkCompleted();

            // Optional toast — string alloc is acceptable on this cold path.
            _notificationQueue?.Enqueue(
                "Daily Challenge Complete!",
                $"+ {reward} bonus credits",
                4f);

            PersistChallenge();

            Debug.Log($"[DailyChallengeManager] Daily challenge completed! " +
                      $"Challenge='{challenge.DisplayName}', Reward={reward} credits.");
        }

        // ── Persistence ───────────────────────────────────────────────────────

        private void PersistChallenge()
        {
            if (_dailyChallenge == null) return;

            var (date, index, completed) = _dailyChallenge.TakeSnapshot();
            SaveData save = SaveSystem.Load();
            save.dailyChallengeDate      = date;
            save.dailyChallengeIndex     = index;
            save.dailyChallengeCompleted = completed;
            SaveSystem.Save(save);
        }
    }
}
