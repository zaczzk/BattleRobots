using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DailyChallengeManager"/>.
    ///
    /// Covers:
    ///   • Awake with null _dailyChallenge / null _config → DoesNotThrow.
    ///   • OnEnable / OnDisable with null _onMatchEnded → DoesNotThrow.
    ///   • HandleMatchEnded guard paths:
    ///       – null DailyChallengeSO → DoesNotThrow (no effect).
    ///       – already-completed challenge → wallet unchanged.
    ///       – null MatchResultSO → DoesNotThrow.
    ///       – null CurrentChallenge → DoesNotThrow.
    ///       – condition not satisfied → wallet unchanged, IsCompleted stays false.
    ///   • HandleMatchEnded success path:
    ///       – satisfied condition → wallet credited by BonusAmount × RewardMultiplier.
    ///       – satisfied condition → IsCompleted set to true.
    ///   • OnDisable unregisters: after disable, raising _onMatchEnded does NOT complete
    ///     the challenge.
    ///
    /// Uses the inactive-GO pattern so Awake fires only after all fields are wired.
    /// SaveSystem.Delete() is called in SetUp and TearDown to prevent test pollution.
    /// </summary>
    public class DailyChallengeManagerTests
    {
        private GameObject           _go;
        private DailyChallengeManager _manager;
        private DailyChallengeSO      _dailyChallenge;
        private DailyChallengeConfig  _config;
        private MatchResultSO         _matchResult;
        private PlayerWallet          _playerWallet;
        private VoidGameEvent         _onMatchEnded;
        private BonusConditionSO      _condition;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();

            _dailyChallenge = ScriptableObject.CreateInstance<DailyChallengeSO>();
            _config         = ScriptableObject.CreateInstance<DailyChallengeConfig>();
            _matchResult    = ScriptableObject.CreateInstance<MatchResultSO>();
            _playerWallet   = ScriptableObject.CreateInstance<PlayerWallet>();
            _onMatchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            _condition      = ScriptableObject.CreateInstance<BonusConditionSO>();

            // NoDamageTaken condition: satisfied when damageTaken ≤ 100, bonus = 100.
            SetField(_condition, "_conditionType", BonusConditionType.NoDamageTaken);
            SetField(_condition, "_threshold",     100f);
            SetField(_condition, "_bonusAmount",   100);
            SetField(_condition, "_displayName",   "Test Condition");

            // Wire condition into config with ×2 multiplier (expected reward = 200).
            SetField(_config, "_challengePool",
                new List<BonusConditionSO> { _condition });
            SetField(_config, "_rewardMultiplier", 2f);

            _go = new GameObject("DailyChallengeManager");
            _go.SetActive(false); // inactive until fields are injected
            _manager = _go.AddComponent<DailyChallengeManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_dailyChallenge);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_matchResult);
            Object.DestroyImmediate(_playerWallet);
            Object.DestroyImmediate(_onMatchEnded);
            Object.DestroyImmediate(_condition);
            SaveSystem.Delete();
        }

        private void Activate() => _go.SetActive(true);

        // ── Awake / OnEnable / OnDisable guards ───────────────────────────────

        [Test]
        public void Awake_NullDailyChallenge_DoesNotThrow()
        {
            // All fields null — Awake must not throw.
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void Awake_NullConfig_DoesNotThrow()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            // _config intentionally null
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnEnable_NullOnMatchEnded_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_NullOnMatchEnded_DoesNotThrow()
        {
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── HandleMatchEnded — guard paths ────────────────────────────────────

        [Test]
        public void HandleMatchEnded_NullDailyChallenge_DoesNotThrow()
        {
            // _dailyChallenge left null; only _onMatchEnded wired.
            SetField(_manager, "_onMatchEnded", _onMatchEnded);
            Activate();
            Assert.DoesNotThrow(() => _onMatchEnded.Raise());
        }

        [Test]
        public void HandleMatchEnded_AlreadyCompleted_DoesNotReward()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            SetField(_manager, "_config",         _config);
            SetField(_manager, "_matchResult",    _matchResult);
            SetField(_manager, "_playerWallet",   _playerWallet);
            SetField(_manager, "_onMatchEnded",   _onMatchEnded);

            // Prime the challenge then mark it completed before the match ends.
            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);
            _dailyChallenge.MarkCompleted();

            _matchResult.Write(playerWon: true, durationSeconds: 30f,
                               currencyEarned: 200, newWalletBalance: 700,
                               damageDone: 50f, damageTaken: 0f);
            _playerWallet.Reset(); // sets Balance = 500
            int balanceBefore = _playerWallet.Balance;

            Activate();
            _onMatchEnded.Raise();

            Assert.AreEqual(balanceBefore, _playerWallet.Balance,
                "Wallet must not change when the challenge is already completed.");
        }

        [Test]
        public void HandleMatchEnded_NullMatchResult_DoesNotThrow()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            SetField(_manager, "_config",         _config);
            SetField(_manager, "_onMatchEnded",   _onMatchEnded);
            // _matchResult intentionally null

            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);

            Activate();
            Assert.DoesNotThrow(() => _onMatchEnded.Raise());
        }

        [Test]
        public void HandleMatchEnded_NullCurrentChallenge_DoesNotThrow()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            SetField(_manager, "_matchResult",    _matchResult);
            SetField(_manager, "_onMatchEnded",   _onMatchEnded);
            // _config null → RefreshIfNeeded is a no-op → CurrentChallenge stays null

            Activate();
            Assert.DoesNotThrow(() => _onMatchEnded.Raise());
        }

        [Test]
        public void HandleMatchEnded_ConditionNotMet_DoesNotReward()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            SetField(_manager, "_config",         _config);
            SetField(_manager, "_matchResult",    _matchResult);
            SetField(_manager, "_playerWallet",   _playerWallet);
            SetField(_manager, "_onMatchEnded",   _onMatchEnded);

            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);

            // Condition: NoDamageTaken threshold = 100; player took 150 → NOT satisfied.
            _matchResult.Write(playerWon: true, durationSeconds: 30f,
                               currencyEarned: 200, newWalletBalance: 700,
                               damageDone: 50f, damageTaken: 150f);
            _playerWallet.Reset();
            int balanceBefore = _playerWallet.Balance;

            Activate();
            _onMatchEnded.Raise();

            Assert.AreEqual(balanceBefore, _playerWallet.Balance,
                "Wallet must not change when the condition is not satisfied.");
            Assert.IsFalse(_dailyChallenge.IsCompleted);
        }

        // ── HandleMatchEnded — success path ───────────────────────────────────

        [Test]
        public void HandleMatchEnded_ConditionMet_RewardsWallet()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            SetField(_manager, "_config",         _config);
            SetField(_manager, "_matchResult",    _matchResult);
            SetField(_manager, "_playerWallet",   _playerWallet);
            SetField(_manager, "_onMatchEnded",   _onMatchEnded);

            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);

            // NoDamageTaken threshold = 100, damageTaken = 0 → satisfied.
            _matchResult.Write(playerWon: true, durationSeconds: 30f,
                               currencyEarned: 200, newWalletBalance: 700,
                               damageDone: 50f, damageTaken: 0f);
            _playerWallet.Reset(); // Balance = 500
            int balanceBefore = _playerWallet.Balance;

            Activate();
            _onMatchEnded.Raise();

            // BonusAmount(100) × RewardMultiplier(2.0) = 200
            Assert.AreEqual(balanceBefore + 200, _playerWallet.Balance,
                "Wallet must be credited by BonusAmount × RewardMultiplier on success.");
        }

        [Test]
        public void HandleMatchEnded_ConditionMet_MarksCompleted()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            SetField(_manager, "_config",         _config);
            SetField(_manager, "_matchResult",    _matchResult);
            SetField(_manager, "_playerWallet",   _playerWallet);
            SetField(_manager, "_onMatchEnded",   _onMatchEnded);

            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);

            _matchResult.Write(playerWon: true, durationSeconds: 30f,
                               currencyEarned: 200, newWalletBalance: 700,
                               damageDone: 50f, damageTaken: 0f);
            _playerWallet.Reset();

            Activate();
            _onMatchEnded.Raise();

            Assert.IsTrue(_dailyChallenge.IsCompleted,
                "DailyChallengeSO.IsCompleted must be true after a successful completion.");
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromMatchEnded()
        {
            SetField(_manager, "_dailyChallenge", _dailyChallenge);
            SetField(_manager, "_config",         _config);
            SetField(_manager, "_matchResult",    _matchResult);
            SetField(_manager, "_playerWallet",   _playerWallet);
            SetField(_manager, "_onMatchEnded",   _onMatchEnded);

            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);

            _matchResult.Write(playerWon: true, durationSeconds: 30f,
                               currencyEarned: 200, newWalletBalance: 700,
                               damageDone: 50f, damageTaken: 0f);
            _playerWallet.Reset();

            Activate();
            _go.SetActive(false); // OnDisable — unregisters _handleMatchEndedDelegate

            // Raising the event after disable must NOT complete the challenge.
            _onMatchEnded.Raise();

            Assert.IsFalse(_dailyChallenge.IsCompleted,
                "Challenge must not be completed after manager is disabled.");
        }
    }
}
