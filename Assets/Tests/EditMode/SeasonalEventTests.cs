using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T070 — Seasonal Event system
    /// (SeasonalEventDefinitionSO, SeasonalEventProgressSO, SeasonalEventData).
    ///
    /// Coverage (20 cases):
    ///
    /// SeasonalEventProgressSO — RecordMatch
    ///   [01] RecordMatch_NullRecord_NoScoreChange
    ///   [02] RecordMatch_NullDefinition_NoScoreChange
    ///   [03] RecordMatch_SeasonInactive_NoScoreChange
    ///   [04] RecordMatch_Win_AwardsPointsPerWin
    ///   [05] RecordMatch_Loss_AwardsPointsPerMatch
    ///   [06] RecordMatch_MultipleMatches_AccumulatesScore
    ///   [07] RecordMatch_FiresOnScoreChangedEvent
    ///
    /// SeasonalEventProgressSO — IsTierReached / TryClaimTier
    ///   [08] IsTierReached_ScoreBelowThreshold_ReturnsFalse
    ///   [09] IsTierReached_ScoreAtThreshold_ReturnsTrue
    ///   [10] IsTierReached_InvalidIndex_ReturnsFalse
    ///   [11] TryClaimTier_TierNotReached_ReturnsFalse_WalletUnchanged
    ///   [12] TryClaimTier_TierReached_ReturnsTrueCreditsWallet
    ///   [13] TryClaimTier_AlreadyClaimed_ReturnsFalse
    ///   [14] TryClaimTier_NullWallet_ReturnsTrueMarksClaimed
    ///   [15] TryClaimTier_FiresOnTierUnlockedEvent
    ///
    /// SeasonalEventProgressSO — LoadFromData / BuildData
    ///   [16] LoadFromData_NullData_StartsClean
    ///   [17] LoadFromData_MatchingSeasonId_RestoresScore
    ///   [18] LoadFromData_DifferentSeasonId_ResetsProgress
    ///   [19] BuildData_RoundTrip_AllFieldsPersisted
    ///
    /// SeasonalEventDefinitionSO — helpers
    ///   [20] GetHighestReachedTierIndex_NoTiersReached_ReturnsMinusOne
    /// </summary>
    [TestFixture]
    public sealed class SeasonalEventTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private SeasonalEventProgressSO  _progress;
        private SeasonalEventDefinitionSO _activeDef;
        private SeasonalEventDefinitionSO _inactiveDef;
        private PlayerWallet             _wallet;

        [SetUp]
        public void SetUp()
        {
            _progress    = ScriptableObject.CreateInstance<SeasonalEventProgressSO>();
            _activeDef   = MakeDefinition("season_active",   active: true,  pWin: 100, pMatch: 10,
                new SeasonalEventRewardTier { tierName = "Bronze", requiredScore = 200, rewardCurrency = 50  },
                new SeasonalEventRewardTier { tierName = "Silver", requiredScore = 500, rewardCurrency = 150 },
                new SeasonalEventRewardTier { tierName = "Gold",   requiredScore = 1000, rewardCurrency = 500 });
            _inactiveDef = MakeDefinition("season_inactive", active: false, pWin: 100, pMatch: 10);
            _wallet      = ScriptableObject.CreateInstance<PlayerWallet>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_progress);
            UnityEngine.Object.DestroyImmediate(_activeDef);
            UnityEngine.Object.DestroyImmediate(_inactiveDef);
            UnityEngine.Object.DestroyImmediate(_wallet);
        }

        // ── [01] RecordMatch — null record ────────────────────────────────────

        [Test]
        public void RecordMatch_NullRecord_NoScoreChange()
        {
            SetDefinition(_progress, _activeDef);
            _progress.RecordMatch(null);
            Assert.AreEqual(0, _progress.Score);
        }

        // ── [02] RecordMatch — null definition ────────────────────────────────

        [Test]
        public void RecordMatch_NullDefinition_NoScoreChange()
        {
            // _definition is null by default
            _progress.RecordMatch(MakeRecord(playerWon: true));
            Assert.AreEqual(0, _progress.Score);
        }

        // ── [03] RecordMatch — inactive season ───────────────────────────────

        [Test]
        public void RecordMatch_SeasonInactive_NoScoreChange()
        {
            SetDefinition(_progress, _inactiveDef);
            _progress.RecordMatch(MakeRecord(playerWon: true));
            Assert.AreEqual(0, _progress.Score);
        }

        // ── [04] RecordMatch — win awards PointsPerWin ───────────────────────

        [Test]
        public void RecordMatch_Win_AwardsPointsPerWin()
        {
            SetDefinition(_progress, _activeDef);
            _progress.RecordMatch(MakeRecord(playerWon: true));
            Assert.AreEqual(100, _progress.Score);
        }

        // ── [05] RecordMatch — loss awards PointsPerMatch ────────────────────

        [Test]
        public void RecordMatch_Loss_AwardsPointsPerMatch()
        {
            SetDefinition(_progress, _activeDef);
            _progress.RecordMatch(MakeRecord(playerWon: false));
            Assert.AreEqual(10, _progress.Score);
        }

        // ── [06] RecordMatch — multiple matches accumulate ────────────────────

        [Test]
        public void RecordMatch_MultipleMatches_AccumulatesScore()
        {
            SetDefinition(_progress, _activeDef);
            _progress.RecordMatch(MakeRecord(playerWon: true));   // +100
            _progress.RecordMatch(MakeRecord(playerWon: false));  // +10
            _progress.RecordMatch(MakeRecord(playerWon: true));   // +100
            Assert.AreEqual(210, _progress.Score);
        }

        // ── [07] RecordMatch — fires OnScoreChanged ───────────────────────────

        [Test]
        public void RecordMatch_FiresOnScoreChangedEvent()
        {
            SetDefinition(_progress, _activeDef);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_progress, "_onScoreChanged", channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            _progress.RecordMatch(MakeRecord(playerWon: true));

            Assert.AreEqual(1, fired);
            UnityEngine.Object.DestroyImmediate(channel);
        }

        // ── [08] IsTierReached — below threshold ──────────────────────────────

        [Test]
        public void IsTierReached_ScoreBelowThreshold_ReturnsFalse()
        {
            SetDefinition(_progress, _activeDef);
            // Score 0, Bronze requires 200
            Assert.IsFalse(_progress.IsTierReached(0));
        }

        // ── [09] IsTierReached — at threshold ────────────────────────────────

        [Test]
        public void IsTierReached_ScoreAtThreshold_ReturnsTrue()
        {
            SetDefinition(_progress, _activeDef);
            // Win twice (200 pts) to reach Bronze threshold (200)
            _progress.RecordMatch(MakeRecord(playerWon: true)); // 100
            _progress.RecordMatch(MakeRecord(playerWon: true)); // 200
            Assert.IsTrue(_progress.IsTierReached(0));
        }

        // ── [10] IsTierReached — invalid index ───────────────────────────────

        [Test]
        public void IsTierReached_InvalidIndex_ReturnsFalse()
        {
            SetDefinition(_progress, _activeDef);
            Assert.IsFalse(_progress.IsTierReached(-1));
            Assert.IsFalse(_progress.IsTierReached(99));
        }

        // ── [11] TryClaimTier — not reached ──────────────────────────────────

        [Test]
        public void TryClaimTier_TierNotReached_ReturnsFalse_WalletUnchanged()
        {
            SetDefinition(_progress, _activeDef);
            int balanceBefore = _wallet.Balance;
            bool result = _progress.TryClaimTier(0, _wallet);
            Assert.IsFalse(result);
            Assert.AreEqual(balanceBefore, _wallet.Balance);
        }

        // ── [12] TryClaimTier — reached, credits wallet ───────────────────────

        [Test]
        public void TryClaimTier_TierReached_ReturnsTrueCreditsWallet()
        {
            SetDefinition(_progress, _activeDef);
            // Reach Bronze (200 pts)
            _progress.RecordMatch(MakeRecord(playerWon: true));
            _progress.RecordMatch(MakeRecord(playerWon: true));

            int balanceBefore = _wallet.Balance;
            bool result = _progress.TryClaimTier(0, _wallet);

            Assert.IsTrue(result);
            Assert.IsTrue(_progress.IsTierClaimed(0));
            Assert.AreEqual(balanceBefore + 50, _wallet.Balance); // Bronze reward = 50
        }

        // ── [13] TryClaimTier — already claimed ───────────────────────────────

        [Test]
        public void TryClaimTier_AlreadyClaimed_ReturnsFalse()
        {
            SetDefinition(_progress, _activeDef);
            _progress.RecordMatch(MakeRecord(playerWon: true));
            _progress.RecordMatch(MakeRecord(playerWon: true));

            _progress.TryClaimTier(0, _wallet);   // first claim
            bool result = _progress.TryClaimTier(0, _wallet);  // second attempt

            Assert.IsFalse(result);
        }

        // ── [14] TryClaimTier — null wallet still marks claimed ───────────────

        [Test]
        public void TryClaimTier_NullWallet_ReturnsTrueMarksClaimed()
        {
            SetDefinition(_progress, _activeDef);
            _progress.RecordMatch(MakeRecord(playerWon: true));
            _progress.RecordMatch(MakeRecord(playerWon: true));

            bool result = _progress.TryClaimTier(0, null);

            Assert.IsTrue(result);
            Assert.IsTrue(_progress.IsTierClaimed(0));
        }

        // ── [15] TryClaimTier — fires OnTierUnlocked ─────────────────────────

        [Test]
        public void TryClaimTier_FiresOnTierUnlockedEvent()
        {
            SetDefinition(_progress, _activeDef);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_progress, "_onTierUnlocked", channel);

            // Reach Bronze
            _progress.RecordMatch(MakeRecord(playerWon: true));
            _progress.RecordMatch(MakeRecord(playerWon: true));

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            _progress.TryClaimTier(0, _wallet);

            Assert.AreEqual(1, fired);
            UnityEngine.Object.DestroyImmediate(channel);
        }

        // ── [16] LoadFromData — null data starts clean ────────────────────────

        [Test]
        public void LoadFromData_NullData_StartsClean()
        {
            SetDefinition(_progress, _activeDef);
            _progress.LoadFromData(null);
            Assert.AreEqual(0, _progress.Score);
            Assert.IsFalse(_progress.IsTierClaimed(0));
        }

        // ── [17] LoadFromData — matching season ID restores state ─────────────

        [Test]
        public void LoadFromData_MatchingSeasonId_RestoresScore()
        {
            SetDefinition(_progress, _activeDef);
            var data = new SeasonalEventData
            {
                seasonId        = "season_active",
                cumulativeScore = 350,
                claimedTierIndices = new List<int> { 0 }, // Bronze claimed
            };

            _progress.LoadFromData(data);

            Assert.AreEqual(350, _progress.Score);
            Assert.IsTrue(_progress.IsTierClaimed(0));
            Assert.IsFalse(_progress.IsTierClaimed(1));
        }

        // ── [18] LoadFromData — different season ID resets ───────────────────

        [Test]
        public void LoadFromData_DifferentSeasonId_ResetsProgress()
        {
            SetDefinition(_progress, _activeDef);
            var staleData = new SeasonalEventData
            {
                seasonId        = "season_old",  // does not match "season_active"
                cumulativeScore = 9999,
                claimedTierIndices = new List<int> { 0, 1, 2 },
            };

            _progress.LoadFromData(staleData);

            Assert.AreEqual(0, _progress.Score);
            Assert.IsFalse(_progress.IsTierClaimed(0));
        }

        // ── [19] BuildData — round-trip ───────────────────────────────────────

        [Test]
        public void BuildData_RoundTrip_AllFieldsPersisted()
        {
            SetDefinition(_progress, _activeDef);

            // Earn Bronze + claim it, earn some more
            for (int i = 0; i < 2; i++) _progress.RecordMatch(MakeRecord(playerWon: true)); // 200
            _progress.TryClaimTier(0, _wallet);  // claim Bronze
            _progress.RecordMatch(MakeRecord(playerWon: true)); // 300

            SeasonalEventData data = _progress.BuildData();

            // Restore into a fresh SO
            var fresh = ScriptableObject.CreateInstance<SeasonalEventProgressSO>();
            SetDefinition(fresh, _activeDef);
            fresh.LoadFromData(data);

            Assert.AreEqual(300, fresh.Score);
            Assert.IsTrue(fresh.IsTierClaimed(0));
            Assert.IsFalse(fresh.IsTierClaimed(1));
            Assert.AreEqual("season_active", data.seasonId);

            UnityEngine.Object.DestroyImmediate(fresh);
        }

        // ── [20] GetHighestReachedTierIndex — none reached ────────────────────

        [Test]
        public void GetHighestReachedTierIndex_NoTiersReached_ReturnsMinusOne()
        {
            // Score 0 against Bronze threshold of 200
            Assert.AreEqual(-1, _activeDef.GetHighestReachedTierIndex(0));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static MatchRecord MakeRecord(bool playerWon) =>
            new MatchRecord { playerWon = playerWon };

        private static SeasonalEventDefinitionSO MakeDefinition(
            string id,
            bool   active,
            int    pWin   = 100,
            int    pMatch = 10,
            params SeasonalEventRewardTier[] tiers)
        {
            var def = ScriptableObject.CreateInstance<SeasonalEventDefinitionSO>();

            SetField(def, "_eventId",       id);
            SetField(def, "_eventName",     id);
            SetField(def, "_pointsPerWin",  pWin);
            SetField(def, "_pointsPerMatch", pMatch);
            SetField(def, "_rewardTiers",   tiers);

            long now = DateTime.UtcNow.ToBinary();
            if (active)
            {
                SetField(def, "_startUtcBinary", DateTime.UtcNow.AddDays(-1).ToBinary());
                SetField(def, "_endUtcBinary",   DateTime.UtcNow.AddDays(30).ToBinary());
            }
            else
            {
                // Already ended
                SetField(def, "_startUtcBinary", DateTime.UtcNow.AddDays(-10).ToBinary());
                SetField(def, "_endUtcBinary",   DateTime.UtcNow.AddDays(-1).ToBinary());
            }

            return def;
        }

        private static void SetDefinition(
            SeasonalEventProgressSO progress,
            SeasonalEventDefinitionSO def)
        {
            SetField(progress, "_definition", def);
        }

        private static void SetField(UnityEngine.Object obj, string fieldName, object value)
        {
            FieldInfo fi = obj.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {obj.GetType().Name}.");
            fi.SetValue(obj, value);
        }
    }
}
