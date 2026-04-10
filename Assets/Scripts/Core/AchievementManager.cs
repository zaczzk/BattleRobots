using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that wires SO event channels to achievement evaluation and
    /// unlocks achievements when their trigger conditions are first met.
    ///
    /// ── Trigger evaluation ────────────────────────────────────────────────────
    ///   Each <see cref="AchievementTrigger"/> type maps to a runtime data source:
    ///   • <see cref="AchievementTrigger.MatchWon"/>     — <see cref="PlayerAchievementsSO.TotalMatchesWon"/>
    ///   • <see cref="AchievementTrigger.WinStreak"/>    — <see cref="WinStreakSO.BestStreak"/>
    ///   • <see cref="AchievementTrigger.ReachLevel"/>   — <see cref="PlayerProgressionSO.CurrentLevel"/>
    ///   • <see cref="AchievementTrigger.TotalMatches"/> — <see cref="PlayerAchievementsSO.TotalMatchesPlayed"/>
    ///   • <see cref="AchievementTrigger.PartUpgraded"/> — sum of all tiers in <see cref="PlayerPartUpgrades"/>
    ///
    /// ── Event-driven evaluation schedule ─────────────────────────────────────
    ///   • <c>_onMatchEnded</c> → <see cref="HandleMatchEnded"/>:
    ///       records match result in <see cref="PlayerAchievementsSO"/>, runs a full
    ///       evaluation pass, then persists to disk.
    ///   • <c>_onLevelUp</c>, <c>_onUpgradesChanged</c>, <c>_onStreakChanged</c>
    ///       → <see cref="EvaluateAll"/>: runs evaluation; no separate persist call
    ///       (the caller that raised the event is responsible for its own persistence).
    ///
    /// ── Persistence ───────────────────────────────────────────────────────────
    ///   <see cref="PersistAchievements"/> applies the load → mutate → save pattern
    ///   to <see cref="SaveData"/> so no other SaveData field is disturbed.
    ///   It is called automatically from <see cref="HandleMatchEnded"/> and is also
    ///   public for callers that need to force a flush.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace. No Physics / UI references.
    ///   - All <see cref="Action"/> delegates are cached in <c>Awake</c> — zero alloc
    ///     in event callbacks.
    ///   - <c>OnEnable</c> / <c>OnDisable</c> register / unregister to prevent leaks.
    ///   - Every injected SO field is optional and null-safe; the system degrades
    ///     gracefully when sources for trigger types are absent.
    ///
    /// ── Scene / SO wiring ─────────────────────────────────────────────────────
    ///   1. Add this MB to any persistent GameObject (e.g. the GameBootstrapper root).
    ///   2. Assign <c>_catalog</c> and <c>_playerAchievements</c> (required for function).
    ///   3. Assign optional sources: <c>_winStreak</c>, <c>_playerProgression</c>,
    ///      <c>_playerPartUpgrades</c>, <c>_matchResult</c>.
    ///   4. Wire the four event-channel fields to the matching SO assets.
    ///   5. Assign <c>_playerWallet</c> to enable credit rewards on unlock.
    /// </summary>
    public sealed class AchievementManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Achievement Data")]
        [Tooltip("Full catalog of achievement definitions. Required for any evaluation.")]
        [SerializeField] private AchievementCatalogSO _catalog;

        [Tooltip("Runtime SO tracking unlock state and match-play counters. " +
                 "Required for any evaluation.")]
        [SerializeField] private PlayerAchievementsSO _playerAchievements;

        [Header("Economy")]
        [Tooltip("Wallet credited when an achievement with RewardCredits > 0 unlocks. " +
                 "Leave null to skip credit rewards (prestige-only mode).")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Optional Trigger Sources")]
        [Tooltip("Provides BestStreak for the WinStreak trigger. Leave null to skip.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Tooltip("Provides CurrentLevel for the ReachLevel trigger. Leave null to skip.")]
        [SerializeField] private PlayerProgressionSO _playerProgression;

        [Tooltip("Provides per-part tier data for the PartUpgraded trigger. Leave null to skip.")]
        [SerializeField] private PlayerPartUpgrades _playerPartUpgrades;

        [Tooltip("Blackboard SO written by MatchManager before MatchEnded fires. " +
                 "Used to determine win / loss for RecordMatchResult(). " +
                 "Leave null to treat every match end as a loss (conservative fallback).")]
        [SerializeField] private MatchResultSO _matchResult;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised when a match ends (MatchManager._onMatchEnded). " +
                 "Triggers RecordMatchResult + full evaluation + persistence.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("VoidGameEvent raised when the player levels up. " +
                 "Leave null if ReachLevel achievements are not used.")]
        [SerializeField] private VoidGameEvent _onLevelUp;

        [Tooltip("VoidGameEvent raised when any upgrade tier changes. " +
                 "Leave null if PartUpgraded achievements are not used.")]
        [SerializeField] private VoidGameEvent _onUpgradesChanged;

        [Tooltip("VoidGameEvent raised when the win streak changes. " +
                 "Leave null if WinStreak achievements are not used.")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        // ── Cached delegates (allocated once in Awake — zero alloc in callbacks) ──

        private Action _handleMatchEndedDelegate;
        private Action _evaluateAllDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _evaluateAllDelegate      = EvaluateAll;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onLevelUp?.RegisterCallback(_evaluateAllDelegate);
            _onUpgradesChanged?.RegisterCallback(_evaluateAllDelegate);
            _onStreakChanged?.RegisterCallback(_evaluateAllDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onLevelUp?.UnregisterCallback(_evaluateAllDelegate);
            _onUpgradesChanged?.UnregisterCallback(_evaluateAllDelegate);
            _onStreakChanged?.UnregisterCallback(_evaluateAllDelegate);
        }

        // ── Internal event handler ────────────────────────────────────────────

        private void HandleMatchEnded()
        {
            if (_playerAchievements == null) return;

            // MatchResultSO is written by MatchManager before MatchEnded fires (T022),
            // so PlayerWon reflects the outcome of the match that just ended.
            bool playerWon = _matchResult != null && _matchResult.PlayerWon;
            _playerAchievements.RecordMatchResult(playerWon);
            EvaluateAll();
            PersistAchievements();
        }

        // ── Public evaluation API ─────────────────────────────────────────────

        /// <summary>
        /// Iterates every achievement in the catalog and unlocks those whose trigger
        /// condition is now satisfied and that have not already been unlocked.
        /// <para>
        /// Also called from level-up, upgrade-changed, and streak-changed event
        /// callbacks.  Safe to call manually (e.g. from tests or after loading a save).
        /// </para>
        /// </summary>
        public void EvaluateAll()
        {
            if (_catalog == null || _playerAchievements == null) return;

            IReadOnlyList<AchievementDefinitionSO> all = _catalog.Achievements;
            for (int i = 0; i < all.Count; i++)
            {
                AchievementDefinitionSO def = all[i];
                if (def == null) continue;
                if (_playerAchievements.HasUnlocked(def.Id)) continue;
                if (EvaluateTrigger(def))
                    UnlockAchievement(def);
            }
        }

        /// <summary>
        /// Persists the current achievement state (unlocked IDs and match counters)
        /// to the save file without disturbing any other <see cref="SaveData"/> field.
        /// Uses the load → mutate → save pattern.
        /// <para>
        /// Called automatically from <see cref="HandleMatchEnded"/>.
        /// Also public so external callers can force a flush if needed.
        /// </para>
        /// </summary>
        public void PersistAchievements()
        {
            if (_playerAchievements == null) return;

            SaveData save = SaveSystem.Load();
            save.totalMatchesPlayed = _playerAchievements.TotalMatchesPlayed;
            save.totalMatchesWon    = _playerAchievements.TotalMatchesWon;

            save.unlockedAchievementIds.Clear();
            IReadOnlyList<string> unlocked = _playerAchievements.UnlockedIds;
            for (int i = 0; i < unlocked.Count; i++)
                save.unlockedAchievementIds.Add(unlocked[i]);

            SaveSystem.Save(save);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private bool EvaluateTrigger(AchievementDefinitionSO def)
        {
            switch (def.TriggerType)
            {
                case AchievementTrigger.MatchWon:
                    return _playerAchievements.TotalMatchesWon >= def.TargetCount;

                case AchievementTrigger.WinStreak:
                    return _winStreak != null &&
                           _winStreak.BestStreak >= def.TargetCount;

                case AchievementTrigger.ReachLevel:
                    return _playerProgression != null &&
                           _playerProgression.CurrentLevel >= def.TargetCount;

                case AchievementTrigger.TotalMatches:
                    return _playerAchievements.TotalMatchesPlayed >= def.TargetCount;

                case AchievementTrigger.PartUpgraded:
                    return GetTotalUpgradeTiers() >= def.TargetCount;

                default:
                    return false;
            }
        }

        private int GetTotalUpgradeTiers()
        {
            if (_playerPartUpgrades == null) return 0;

            _playerPartUpgrades.TakeSnapshot(out List<string> _, out List<int> values);
            int total = 0;
            for (int i = 0; i < values.Count; i++)
                total += values[i];
            return total;
        }

        private void UnlockAchievement(AchievementDefinitionSO def)
        {
            _playerAchievements.Unlock(def.Id);

            if (def.RewardCredits > 0)
                _playerWallet?.AddFunds(def.RewardCredits);

            Debug.Log($"[AchievementManager] Achievement unlocked: '{def.DisplayName}'. " +
                      $"Reward: {def.RewardCredits} credits.");
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_catalog == null)
                Debug.LogWarning("[AchievementManager] _catalog not assigned — " +
                                 "no achievements will be evaluated.", this);
            if (_playerAchievements == null)
                Debug.LogWarning("[AchievementManager] _playerAchievements not assigned — " +
                                 "no achievements will be evaluated.", this);
        }
#endif
    }
}
