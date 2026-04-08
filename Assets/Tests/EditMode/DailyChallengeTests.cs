using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T069 — Daily Challenge system
    /// (DailyChallengeDefinitionSO, DailyChallengeProgressSO, DailyChallengeData).
    ///
    /// Coverage (20 cases):
    ///
    /// DailyChallengeProgressSO — RefreshForToday
    ///   [01] RefreshForToday_NullData_SelectsChallengeFromCatalog
    ///   [02] RefreshForToday_TodayData_RestoresProgress
    ///   [03] RefreshForToday_YesterdayData_ResetsForNewDay
    ///   [04] RefreshForToday_TodayData_CompletedState_Restored
    ///   [05] RefreshForToday_EmptyCatalog_ActiveChallengeIsNull
    ///   [06] RefreshForToday_UnknownChallengeId_ActiveChallengeIsNull
    ///
    /// DailyChallengeProgressSO — RecordMatch
    ///   [07] RecordMatch_NullRecord_NoChange
    ///   [08] RecordMatch_NullActiveChallenge_NoChange
    ///   [09] RecordMatch_AlreadyCompleted_NoFurtherChange
    ///   [10] RecordMatch_DamageTotal_AccumulatesCorrectly
    ///   [11] RecordMatch_DamageTotal_CapsAtTarget
    ///   [12] RecordMatch_WinCount_PlayerWon_Increments
    ///   [13] RecordMatch_WinCount_PlayerLost_NoIncrement
    ///   [14] RecordMatch_PlayCount_AlwaysIncrements
    ///   [15] RecordMatch_CurrencyTotal_AccumulatesCorrectly
    ///   [16] RecordMatch_CompletesChallenge_FiresCompletedEvent
    ///
    /// DailyChallengeProgressSO — ClaimReward
    ///   [17] ClaimReward_NotCompleted_NoOp
    ///   [18] ClaimReward_AlreadyClaimed_NoOp
    ///   [19] ClaimReward_Completed_AddsRewardToWallet
    ///
    /// DailyChallengeProgressSO — BuildData persistence
    ///   [20] BuildData_RoundTrip_AllFieldsPersisted
    /// </summary>
    [TestFixture]
    public sealed class DailyChallengeTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private DailyChallengeProgressSO _challengeProgress;
        private DailyChallengeDefinitionSO _defA;
        private DailyChallengeDefinitionSO _defB;
        private PlayerWallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _challengeProgress = ScriptableObject.CreateInstance<DailyChallengeProgressSO>();
            _defA              = MakeDef("daily_dmg_500",  DailyChallengeGoalType.DamageTotal,   500f, 200);
            _defB              = MakeDef("daily_win_1",    DailyChallengeGoalType.WinCount,         1f, 150);
            _wallet            = ScriptableObject.CreateInstance<PlayerWallet>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_challengeProgress);
            Object.DestroyImmediate(_defA);
            Object.DestroyImmediate(_defB);
            Object.DestroyImmediate(_wallet);
        }

        // ── [01] RefreshForToday — null data selects from catalog ─────────────

        [Test]
        public void RefreshForToday_NullData_SelectsChallengeFromCatalog()
        {
            SetCatalog(_challengeProgress, new[] { _defA, _defB });

            _challengeProgress.RefreshForToday(null);

            Assert.IsNotNull(_challengeProgress.ActiveChallenge,
                "A challenge should be selected from the catalog.");
            Assert.AreEqual(0f, _challengeProgress.Progress);
            Assert.IsFalse(_challengeProgress.IsCompleted);
            Assert.IsFalse(_challengeProgress.IsRewardClaimed);
        }

        // ── [02] RefreshForToday — today's data restores progress ─────────────

        [Test]
        public void RefreshForToday_TodayData_RestoresProgress()
        {
            SetCatalog(_challengeProgress, new[] { _defA });
            string today = DailyChallengeProgressSO.GetTodayKey();

            var data = new DailyChallengeData
            {
                lastDateUtc   = today,
                challengeId   = _defA.ChallengeId,
                progress      = 250f,
                rewardClaimed = false,
            };

            _challengeProgress.RefreshForToday(data);

            Assert.AreEqual(_defA, _challengeProgress.ActiveChallenge);
            Assert.AreEqual(250f, _challengeProgress.Progress, 0.001f);
            Assert.IsFalse(_challengeProgress.IsCompleted,
                "250 / 500 should not be completed.");
        }

        // ── [03] RefreshForToday — yesterday's data resets for new day ────────

        [Test]
        public void RefreshForToday_YesterdayData_ResetsForNewDay()
        {
            SetCatalog(_challengeProgress, new[] { _defA });

            var data = new DailyChallengeData
            {
                lastDateUtc   = "2000-01-01",   // clearly in the past
                challengeId   = _defA.ChallengeId,
                progress      = 499f,
                rewardClaimed = true,
            };

            _challengeProgress.RefreshForToday(data);

            Assert.AreEqual(0f, _challengeProgress.Progress,
                "Progress should reset for a new day.");
            Assert.IsFalse(_challengeProgress.IsRewardClaimed,
                "RewardClaimed should reset for a new day.");
            Assert.IsFalse(_challengeProgress.IsCompleted);
        }

        // ── [04] RefreshForToday — today's completed state restored ───────────

        [Test]
        public void RefreshForToday_TodayData_CompletedState_Restored()
        {
            SetCatalog(_challengeProgress, new[] { _defA });
            string today = DailyChallengeProgressSO.GetTodayKey();

            var data = new DailyChallengeData
            {
                lastDateUtc   = today,
                challengeId   = _defA.ChallengeId,
                progress      = 500f,   // exactly at target
                rewardClaimed = true,
            };

            _challengeProgress.RefreshForToday(data);

            Assert.IsTrue(_challengeProgress.IsCompleted,
                "Should be completed when restored progress == target.");
            Assert.IsTrue(_challengeProgress.IsRewardClaimed);
        }

        // ── [05] RefreshForToday — empty catalog → ActiveChallenge null ────────

        [Test]
        public void RefreshForToday_EmptyCatalog_ActiveChallengeIsNull()
        {
            SetCatalog(_challengeProgress, System.Array.Empty<DailyChallengeDefinitionSO>());

            _challengeProgress.RefreshForToday(null);

            Assert.IsNull(_challengeProgress.ActiveChallenge);
        }

        // ── [06] RefreshForToday — unknown ID in today data → null challenge ───

        [Test]
        public void RefreshForToday_UnknownChallengeId_ActiveChallengeIsNull()
        {
            SetCatalog(_challengeProgress, new[] { _defA });
            string today = DailyChallengeProgressSO.GetTodayKey();

            var data = new DailyChallengeData
            {
                lastDateUtc = today,
                challengeId = "nonexistent_id",
                progress    = 100f,
            };

            _challengeProgress.RefreshForToday(data);

            Assert.IsNull(_challengeProgress.ActiveChallenge,
                "Unknown challengeId should yield null ActiveChallenge.");
        }

        // ── [07] RecordMatch — null record no-ops ─────────────────────────────

        [Test]
        public void RecordMatch_NullRecord_NoChange()
        {
            SetCatalog(_challengeProgress, new[] { _defA });
            _challengeProgress.RefreshForToday(null);

            _challengeProgress.RecordMatch(null);

            Assert.AreEqual(0f, _challengeProgress.Progress);
        }

        // ── [08] RecordMatch — null active challenge no-ops ───────────────────

        [Test]
        public void RecordMatch_NullActiveChallenge_NoChange()
        {
            // Empty catalog → no challenge selected.
            SetCatalog(_challengeProgress, System.Array.Empty<DailyChallengeDefinitionSO>());
            _challengeProgress.RefreshForToday(null);

            Assert.DoesNotThrow(() =>
                _challengeProgress.RecordMatch(new MatchRecord { damageDone = 999f }));

            Assert.AreEqual(0f, _challengeProgress.Progress);
        }

        // ── [09] RecordMatch — already completed → no further change ──────────

        [Test]
        public void RecordMatch_AlreadyCompleted_NoFurtherChange()
        {
            SetCatalog(_challengeProgress, new[] { _defA }); // DamageTotal, target 500
            string today = DailyChallengeProgressSO.GetTodayKey();
            var data = new DailyChallengeData
            {
                lastDateUtc = today,
                challengeId = _defA.ChallengeId,
                progress    = 500f,
            };
            _challengeProgress.RefreshForToday(data);
            Assert.IsTrue(_challengeProgress.IsCompleted);

            _challengeProgress.RecordMatch(new MatchRecord { damageDone = 300f });

            Assert.AreEqual(500f, _challengeProgress.Progress,
                "Progress should not exceed target once completed.");
        }

        // ── [10] RecordMatch — DamageTotal accumulates ────────────────────────

        [Test]
        public void RecordMatch_DamageTotal_AccumulatesCorrectly()
        {
            SetCatalog(_challengeProgress, new[] { _defA }); // DamageTotal 500
            _challengeProgress.RefreshForToday(null);

            _challengeProgress.RecordMatch(new MatchRecord { damageDone = 200f });
            _challengeProgress.RecordMatch(new MatchRecord { damageDone = 150f });

            Assert.AreEqual(350f, _challengeProgress.Progress, 0.001f);
            Assert.IsFalse(_challengeProgress.IsCompleted);
        }

        // ── [11] RecordMatch — DamageTotal caps at target ─────────────────────

        [Test]
        public void RecordMatch_DamageTotal_CapsAtTarget()
        {
            SetCatalog(_challengeProgress, new[] { _defA }); // DamageTotal 500
            _challengeProgress.RefreshForToday(null);

            _challengeProgress.RecordMatch(new MatchRecord { damageDone = 999f });

            Assert.AreEqual(500f, _challengeProgress.Progress, 0.001f,
                "Progress should be capped at TargetValue.");
            Assert.IsTrue(_challengeProgress.IsCompleted);
        }

        // ── [12] RecordMatch — WinCount increments on win ─────────────────────

        [Test]
        public void RecordMatch_WinCount_PlayerWon_Increments()
        {
            SetCatalog(_challengeProgress, new[] { _defB }); // WinCount 1
            _challengeProgress.RefreshForToday(null);

            _challengeProgress.RecordMatch(new MatchRecord { playerWon = true });

            Assert.AreEqual(1f, _challengeProgress.Progress, 0.001f);
            Assert.IsTrue(_challengeProgress.IsCompleted);
        }

        // ── [13] RecordMatch — WinCount no-ops on loss ────────────────────────

        [Test]
        public void RecordMatch_WinCount_PlayerLost_NoIncrement()
        {
            SetCatalog(_challengeProgress, new[] { _defB }); // WinCount 1
            _challengeProgress.RefreshForToday(null);

            _challengeProgress.RecordMatch(new MatchRecord { playerWon = false });

            Assert.AreEqual(0f, _challengeProgress.Progress);
            Assert.IsFalse(_challengeProgress.IsCompleted);
        }

        // ── [14] RecordMatch — PlayCount always increments ────────────────────

        [Test]
        public void RecordMatch_PlayCount_AlwaysIncrements()
        {
            var defPlay = MakeDef("daily_play_3", DailyChallengeGoalType.PlayCount, 3f, 100);
            SetCatalog(_challengeProgress, new[] { defPlay });
            _challengeProgress.RefreshForToday(null);

            _challengeProgress.RecordMatch(new MatchRecord { playerWon = false });
            _challengeProgress.RecordMatch(new MatchRecord { playerWon = true  });

            Assert.AreEqual(2f, _challengeProgress.Progress, 0.001f);
            Assert.IsFalse(_challengeProgress.IsCompleted, "3 plays required; only 2 done.");

            Object.DestroyImmediate(defPlay);
        }

        // ── [15] RecordMatch — CurrencyTotal accumulates ──────────────────────

        [Test]
        public void RecordMatch_CurrencyTotal_AccumulatesCorrectly()
        {
            var defCur = MakeDef("daily_cur_300", DailyChallengeGoalType.CurrencyTotal, 300f, 100);
            SetCatalog(_challengeProgress, new[] { defCur });
            _challengeProgress.RefreshForToday(null);

            _challengeProgress.RecordMatch(new MatchRecord { currencyEarned = 150 });
            _challengeProgress.RecordMatch(new MatchRecord { currencyEarned =  80 });

            Assert.AreEqual(230f, _challengeProgress.Progress, 0.001f);
            Assert.IsFalse(_challengeProgress.IsCompleted, "230 / 300 → not done yet.");

            Object.DestroyImmediate(defCur);
        }

        // ── [16] RecordMatch — completes challenge and fires event ────────────

        [Test]
        public void RecordMatch_CompletesChallenge_FiresCompletedEvent()
        {
            SetCatalog(_challengeProgress, new[] { _defB }); // WinCount 1

            // Wire a VoidGameEvent to track fires.
            var completedEvt = ScriptableObject.CreateInstance<VoidGameEvent>();
            int fireCount    = 0;
            var listener     = new GameObject().AddComponent<VoidGameEventListener>();
            SetField(listener, "_event", completedEvt);
            var response = new UnityEngine.Events.UnityEvent();
            response.AddListener(() => fireCount++);
            SetField(listener, "_response", response);
            listener.SendMessage("OnEnable"); // register listener

            SetField(_challengeProgress, "_onChallengeCompleted", completedEvt);

            _challengeProgress.RefreshForToday(null);
            _challengeProgress.RecordMatch(new MatchRecord { playerWon = true });

            Assert.AreEqual(1, fireCount, "_onChallengeCompleted should fire exactly once.");
            Assert.IsTrue(_challengeProgress.IsCompleted);

            Object.DestroyImmediate(completedEvt);
            Object.DestroyImmediate(listener.gameObject);
        }

        // ── [17] ClaimReward — not completed → no-op ─────────────────────────

        [Test]
        public void ClaimReward_NotCompleted_NoOp()
        {
            SetCatalog(_challengeProgress, new[] { _defA });
            _challengeProgress.RefreshForToday(null);
            _wallet.Reset();

            _challengeProgress.ClaimReward(_wallet);

            Assert.AreEqual(0, _wallet.Balance,
                "Wallet should not change when challenge is not completed.");
            Assert.IsFalse(_challengeProgress.IsRewardClaimed);
        }

        // ── [18] ClaimReward — already claimed → no-op ───────────────────────

        [Test]
        public void ClaimReward_AlreadyClaimed_NoOp()
        {
            SetCatalog(_challengeProgress, new[] { _defB }); // WinCount 1, reward 150
            _challengeProgress.RefreshForToday(null);
            _wallet.Reset();

            // Complete and claim.
            _challengeProgress.RecordMatch(new MatchRecord { playerWon = true });
            _challengeProgress.ClaimReward(_wallet);

            int balanceAfterFirstClaim = _wallet.Balance;

            // Second claim attempt.
            _challengeProgress.ClaimReward(_wallet);

            Assert.AreEqual(balanceAfterFirstClaim, _wallet.Balance,
                "Second claim should not add more currency.");
        }

        // ── [19] ClaimReward — completed → credits wallet ─────────────────────

        [Test]
        public void ClaimReward_Completed_AddsRewardToWallet()
        {
            SetCatalog(_challengeProgress, new[] { _defB }); // WinCount 1, reward 150
            _challengeProgress.RefreshForToday(null);
            _wallet.Reset();

            _challengeProgress.RecordMatch(new MatchRecord { playerWon = true });
            _challengeProgress.ClaimReward(_wallet);

            Assert.AreEqual(150, _wallet.Balance,
                "PlayerWallet should receive the challenge reward currency.");
            Assert.IsTrue(_challengeProgress.IsRewardClaimed);
        }

        // ── [20] BuildData — round-trip persists all fields ───────────────────

        [Test]
        public void BuildData_RoundTrip_AllFieldsPersisted()
        {
            SetCatalog(_challengeProgress, new[] { _defA }); // DamageTotal 500
            _challengeProgress.RefreshForToday(null);

            // Partial progress — not yet complete.
            _challengeProgress.RecordMatch(new MatchRecord { damageDone = 300f });

            DailyChallengeData snapshot = _challengeProgress.BuildData();

            // Validate snapshot fields.
            Assert.IsFalse(string.IsNullOrEmpty(snapshot.lastDateUtc),
                "lastDateUtc must be set.");
            Assert.AreEqual(DailyChallengeProgressSO.GetTodayKey(), snapshot.lastDateUtc);
            Assert.AreEqual(_defA.ChallengeId, snapshot.challengeId);
            Assert.AreEqual(300f, snapshot.progress, 0.001f);
            Assert.IsFalse(snapshot.rewardClaimed);

            // Hydrate a fresh SO and verify state matches.
            var restored = ScriptableObject.CreateInstance<DailyChallengeProgressSO>();
            SetCatalog(restored, new[] { _defA });
            restored.RefreshForToday(snapshot);

            Assert.AreEqual(_defA, restored.ActiveChallenge);
            Assert.AreEqual(300f, restored.Progress, 0.001f);
            Assert.IsFalse(restored.IsCompleted);

            Object.DestroyImmediate(restored);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static DailyChallengeDefinitionSO MakeDef(
            string id,
            DailyChallengeGoalType goalType,
            float targetValue,
            int rewardCurrency)
        {
            var def = ScriptableObject.CreateInstance<DailyChallengeDefinitionSO>();
            SetField(def, "_challengeId",    id);
            SetField(def, "_description",    id + "_desc");
            SetField(def, "_goalType",       goalType);
            SetField(def, "_targetValue",    targetValue);
            SetField(def, "_rewardCurrency", rewardCurrency);
            return def;
        }

        private static void SetCatalog(
            DailyChallengeProgressSO progress,
            DailyChallengeDefinitionSO[] defs)
        {
            var list = new List<DailyChallengeDefinitionSO>(defs);
            typeof(DailyChallengeProgressSO)
                .GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(progress, list);
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(target, value);
        }
    }
}
