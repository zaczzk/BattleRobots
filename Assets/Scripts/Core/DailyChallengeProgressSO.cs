using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that manages the player's daily-challenge state.
    ///
    /// ── How daily selection works ─────────────────────────────────────────────
    ///   On startup <see cref="RefreshForToday"/> is called with the persisted
    ///   <see cref="DailyChallengeData"/>. If the stored UTC date matches today the
    ///   previous session's progress is restored; otherwise a new challenge is
    ///   chosen deterministically from <c>_catalog</c> using the UTC date as a
    ///   <see cref="System.Random"/> seed, guaranteeing all players worldwide get
    ///   the same challenge each day.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper.LoadAndApplySaveData"/> calls
    ///      <see cref="RefreshForToday"/> with the stored <c>DailyChallengeData</c>.
    ///   2. After every match <see cref="GameBootstrapper.RecordMatchAndSave"/> calls
    ///      <see cref="RecordMatch"/>; then saves via <see cref="BuildData"/>.
    ///   3. <see cref="ClaimReward"/> is called from <see cref="BattleRobots.UI.DailyChallengeUI"/>
    ///      when the player taps the "Claim" button.
    ///
    /// ── Event channels ────────────────────────────────────────────────────────
    ///   <list type="bullet">
    ///     <item><c>_onChallengeCompleted</c> (VoidGameEvent) — raised the first
    ///       time <see cref="Progress"/> reaches <see cref="TargetValue"/>.</item>
    ///     <item><c>_onRewardClaimed</c> (VoidGameEvent) — raised once when the
    ///       player successfully claims the reward.</item>
    ///     <item><c>_onProgressChanged</c> (FloatGameEvent) — raised after each
    ///       <see cref="RecordMatch"/> call with the new progress fraction [0..1].
    ///       Wire a FloatGameEventListener → DailyChallengeUI.OnProgressChanged or
    ///       Slider.value.</item>
    ///   </list>
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Core</c> only — no Physics or UI references.
    ///   • SO assets are immutable at runtime except for designated mutators
    ///     (<see cref="RefreshForToday"/>, <see cref="RecordMatch"/>,
    ///     <see cref="ClaimReward"/>).
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ DailyChallenge ▶ Progress
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/DailyChallenge/Progress",
        order    = 2)]
    public sealed class DailyChallengeProgressSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Catalog")]
        [Tooltip("All available daily-challenge definitions. One is chosen per UTC day.")]
        [SerializeField] private List<DailyChallengeDefinitionSO> _catalog =
            new List<DailyChallengeDefinitionSO>();

        [Header("Event Channels — Out")]
        [Tooltip("Raised once when the player's accumulated progress first meets the target.")]
        [SerializeField] private VoidGameEvent _onChallengeCompleted;

        [Tooltip("Raised once when the player claims the completed challenge reward.")]
        [SerializeField] private VoidGameEvent _onRewardClaimed;

        [Tooltip("Raised after RecordMatch with the current progress fraction [0..1]. " +
                 "Wire a FloatGameEventListener → Slider.value for a progress bar.")]
        [SerializeField] private FloatGameEvent _onProgressChanged;

        // ── Runtime state ─────────────────────────────────────────────────────

        private DailyChallengeDefinitionSO _activeChallenge;
        private string _todayKey;   // "YYYY-MM-DD" UTC
        private float  _progress;
        private bool   _isCompleted;
        private bool   _rewardClaimed;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The challenge definition selected for today, or <c>null</c> if the catalog is empty.</summary>
        public DailyChallengeDefinitionSO ActiveChallenge => _activeChallenge;

        /// <summary>Accumulated raw progress units toward <see cref="TargetValue"/>.</summary>
        public float Progress => _progress;

        /// <summary>Goal threshold copied from the active challenge for convenience.</summary>
        public float TargetValue => _activeChallenge != null ? _activeChallenge.TargetValue : 0f;

        /// <summary>Progress as a normalised fraction [0..1] clamped to 1 on completion.</summary>
        public float ProgressFraction =>
            (_activeChallenge != null && _activeChallenge.TargetValue > 0f)
                ? Mathf.Clamp01(_progress / _activeChallenge.TargetValue)
                : 0f;

        /// <summary>True once accumulated progress has reached the target for today's challenge.</summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>True once <see cref="ClaimReward"/> has been called successfully.</summary>
        public bool IsRewardClaimed => _rewardClaimed;

        // ── Lifecycle mutators ────────────────────────────────────────────────

        /// <summary>
        /// Restores today's challenge state from <paramref name="data"/>, or selects
        /// a new challenge when the stored date differs from today (UTC).
        ///
        /// <para>Call once during <see cref="GameBootstrapper"/> startup.</para>
        /// </summary>
        public void RefreshForToday(DailyChallengeData data)
        {
            string today = GetTodayKey();

            if (data != null
                && data.lastDateUtc == today
                && !string.IsNullOrEmpty(data.challengeId))
            {
                // Same day — restore persisted state.
                _todayKey       = today;
                _activeChallenge = FindById(data.challengeId);
                _progress       = data.progress;
                _isCompleted    = _activeChallenge != null
                                  && _progress >= _activeChallenge.TargetValue;
                _rewardClaimed  = data.rewardClaimed;
            }
            else
            {
                // New day (or first launch) — reset and select fresh challenge.
                _todayKey       = today;
                _activeChallenge = SelectChallengeForDate(today);
                _progress       = 0f;
                _isCompleted    = false;
                _rewardClaimed  = false;
            }
        }

        /// <summary>
        /// Processes a completed match and updates accumulated progress.
        /// Fires <c>_onProgressChanged</c> (FloatGameEvent, fraction [0..1]) and,
        /// if newly completed, <c>_onChallengeCompleted</c> (VoidGameEvent).
        ///
        /// <para>No-ops when: already completed, no active challenge, or record is null.</para>
        /// </summary>
        public void RecordMatch(MatchRecord record)
        {
            if (_activeChallenge == null || _isCompleted || record == null) return;

            float delta = _activeChallenge.ComputeProgress(record);
            if (delta <= 0f) return;

            _progress = Mathf.Min(_progress + delta, _activeChallenge.TargetValue);
            _onProgressChanged?.Raise(ProgressFraction);

            if (!_isCompleted && _progress >= _activeChallenge.TargetValue)
            {
                _isCompleted = true;
                _onChallengeCompleted?.Raise();
            }
        }

        /// <summary>
        /// Grants the challenge reward to <paramref name="wallet"/> and marks the
        /// reward as claimed. Raises <c>_onRewardClaimed</c>.
        ///
        /// <para>No-ops when: not completed, already claimed, or no active challenge.</para>
        /// </summary>
        public void ClaimReward(PlayerWallet wallet)
        {
            if (!_isCompleted || _rewardClaimed || _activeChallenge == null) return;

            wallet?.AddFunds(_activeChallenge.RewardCurrency);
            _rewardClaimed = true;
            _onRewardClaimed?.Raise();
        }

        // ── Persistence ───────────────────────────────────────────────────────

        /// <summary>
        /// Snapshots current daily-challenge state into a <see cref="DailyChallengeData"/>
        /// POCO for XOR-SaveSystem persistence.
        /// Called by <see cref="GameBootstrapper.RecordMatchAndSave"/>.
        /// </summary>
        public DailyChallengeData BuildData()
        {
            return new DailyChallengeData
            {
                lastDateUtc   = _todayKey   ?? string.Empty,
                challengeId   = _activeChallenge?.ChallengeId ?? string.Empty,
                progress      = _progress,
                rewardClaimed = _rewardClaimed,
            };
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Returns today's UTC date as a "YYYY-MM-DD" string.
        /// Internal and virtual for unit-test override via subclass (kept internal
        /// so tests in the same assembly can access it via reflection if needed).
        /// </summary>
        internal static string GetTodayKey()
        {
            DateTime utc = DateTime.UtcNow;
            return $"{utc.Year:D4}-{utc.Month:D2}-{utc.Day:D2}";
        }

        /// <summary>
        /// Picks a challenge from <c>_catalog</c> deterministically using the
        /// date string's hash as a <see cref="System.Random"/> seed.
        /// All players sharing the same clock date will get the same challenge.
        /// Returns <c>null</c> if the catalog is empty or all entries are null.
        /// </summary>
        private DailyChallengeDefinitionSO SelectChallengeForDate(string dateKey)
        {
            if (_catalog == null || _catalog.Count == 0) return null;

            // Seed from date string — stable across devices and sessions.
            int seed = 0;
            for (int i = 0; i < dateKey.Length; i++)
                seed = seed * 31 + dateKey[i];

            var rng   = new System.Random(seed);
            int index = rng.Next(0, _catalog.Count);
            return _catalog[index];
        }

        /// <summary>
        /// Finds a <see cref="DailyChallengeDefinitionSO"/> in the catalog by
        /// <see cref="DailyChallengeDefinitionSO.ChallengeId"/>. Linear scan.
        /// Returns <c>null</c> if not found.
        /// </summary>
        private DailyChallengeDefinitionSO FindById(string id)
        {
            if (string.IsNullOrEmpty(id) || _catalog == null) return null;

            for (int i = 0; i < _catalog.Count; i++)
            {
                if (_catalog[i] != null && _catalog[i].ChallengeId == id)
                    return _catalog[i];
            }
            return null;
        }
    }
}
