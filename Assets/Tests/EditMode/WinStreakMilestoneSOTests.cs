using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WinStreakMilestoneSO"/>.
    ///
    /// Covers:
    ///   • Milestones list is not null on a fresh instance.
    ///   • Milestones list is empty on a fresh instance.
    ///   • Milestones satisfies IReadOnlyList contract.
    ///   • GetRewardsForStreak returns empty list on empty catalog.
    ///   • GetRewardsForStreak returns empty list when no entry matches.
    ///   • GetRewardsForStreak returns the single matching entry.
    ///   • GetRewardsForStreak returns all entries at the same streakTarget.
    ///   • GetRewardsForStreak does not return entries at other targets.
    ///   • HasMilestoneAtStreak returns false on empty list.
    ///   • HasMilestoneAtStreak returns false for unmatched streak.
    ///   • HasMilestoneAtStreak returns true for matched streak.
    ///   • Entry field defaults and value storage (streakTarget, rewardCredits, displayName).
    /// </summary>
    public class WinStreakMilestoneSOTests
    {
        private WinStreakMilestoneSO _config;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetMilestones(WinStreakMilestoneSO config,
                                          List<WinStreakMilestoneEntry> milestones)
        {
            FieldInfo fi = config.GetType()
                .GetField("_milestones", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_milestones' not found on WinStreakMilestoneSO.");
            fi.SetValue(config, milestones);
        }

        private static WinStreakMilestoneEntry MakeEntry(int target, int credits,
                                                          string name = "")
            => new WinStreakMilestoneEntry
            {
                streakTarget  = target,
                rewardCredits = credits,
                displayName   = name,
            };

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<WinStreakMilestoneSO>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        // ── Milestones list — fresh instance ──────────────────────────────────

        [Test]
        public void Milestones_FreshInstance_IsNotNull()
        {
            Assert.IsNotNull(_config.Milestones);
        }

        [Test]
        public void Milestones_FreshInstance_IsEmpty()
        {
            Assert.AreEqual(0, _config.Milestones.Count);
        }

        [Test]
        public void Milestones_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<WinStreakMilestoneEntry>>(_config.Milestones);
        }

        // ── GetRewardsForStreak — empty catalog ───────────────────────────────

        [Test]
        public void GetRewardsForStreak_EmptyCatalog_ReturnsEmpty()
        {
            var result = _config.GetRewardsForStreak(3);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        // ── GetRewardsForStreak — no match ────────────────────────────────────

        [Test]
        public void GetRewardsForStreak_NoMatchingTarget_ReturnsEmpty()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry>
            {
                MakeEntry(5, 100)
            });

            var result = _config.GetRewardsForStreak(3);
            Assert.AreEqual(0, result.Count);
        }

        // ── GetRewardsForStreak — single match ────────────────────────────────

        [Test]
        public void GetRewardsForStreak_SingleMatch_ReturnsOneEntry()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry>
            {
                MakeEntry(3, 150, "3-Win Streak!")
            });

            var result = _config.GetRewardsForStreak(3);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(150,          result[0].rewardCredits);
            Assert.AreEqual("3-Win Streak!", result[0].displayName);
        }

        // ── GetRewardsForStreak — multiple entries at same target ─────────────

        [Test]
        public void GetRewardsForStreak_MultipleEntriesSameTarget_ReturnsAll()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry>
            {
                MakeEntry(5, 100, "A"),
                MakeEntry(5, 200, "B"),
            });

            var result = _config.GetRewardsForStreak(5);
            Assert.AreEqual(2, result.Count);
        }

        // ── GetRewardsForStreak — only matching target returned ───────────────

        [Test]
        public void GetRewardsForStreak_MixedTargets_OnlyReturnsMatchingTarget()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry>
            {
                MakeEntry(3,  50),
                MakeEntry(5, 100),
                MakeEntry(5, 200),
                MakeEntry(10, 500),
            });

            var result = _config.GetRewardsForStreak(5);
            Assert.AreEqual(2, result.Count);
            foreach (var e in result)
                Assert.AreEqual(5, e.streakTarget);
        }

        // ── HasMilestoneAtStreak ──────────────────────────────────────────────

        [Test]
        public void HasMilestoneAtStreak_EmptyCatalog_ReturnsFalse()
        {
            Assert.IsFalse(_config.HasMilestoneAtStreak(3));
        }

        [Test]
        public void HasMilestoneAtStreak_NoMatchingTarget_ReturnsFalse()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(5, 100) });
            Assert.IsFalse(_config.HasMilestoneAtStreak(3));
        }

        [Test]
        public void HasMilestoneAtStreak_MatchingTarget_ReturnsTrue()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(5, 100) });
            Assert.IsTrue(_config.HasMilestoneAtStreak(5));
        }

        // ── Entry value storage ───────────────────────────────────────────────

        [Test]
        public void Entry_FieldsStoreCorrectly()
        {
            var entry = MakeEntry(7, 350, "Lucky 7!");
            Assert.AreEqual(7,          entry.streakTarget);
            Assert.AreEqual(350,        entry.rewardCredits);
            Assert.AreEqual("Lucky 7!", entry.displayName);
        }
    }
}
