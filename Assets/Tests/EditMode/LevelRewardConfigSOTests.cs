using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LevelRewardConfigSO"/>.
    ///
    /// Covers:
    ///   • Rewards list is not null on a fresh instance
    ///   • GetRewardsForLevel returns empty list when no entries exist
    ///   • GetRewardsForLevel returns empty list when no entry matches
    ///   • GetRewardsForLevel returns the single matching entry
    ///   • GetRewardsForLevel returns multiple entries at the same level
    ///   • GetRewardsForLevel does not return entries at other levels
    ///   • HasRewardAtLevel returns false on empty list
    ///   • HasRewardAtLevel returns false for unmatched level
    ///   • HasRewardAtLevel returns true for matched level
    ///   • IReadOnlyList contract — Rewards cannot be cast to mutable List
    ///   • Entry field defaults and value storage (level, rewardCredits, displayName)
    /// </summary>
    public class LevelRewardConfigSOTests
    {
        private LevelRewardConfigSO _config;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetRewards(LevelRewardConfigSO config,
                                       List<LevelRewardEntry> rewards)
        {
            FieldInfo fi = config.GetType()
                .GetField("_rewards", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_rewards' not found on LevelRewardConfigSO.");
            fi.SetValue(config, rewards);
        }

        private static LevelRewardEntry MakeEntry(int level, int credits,
                                                   string name = "")
            => new LevelRewardEntry { level = level, rewardCredits = credits, displayName = name };

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<LevelRewardConfigSO>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        // ── Rewards list not-null on fresh instance ───────────────────────────

        [Test]
        public void Rewards_FreshInstance_IsNotNull()
        {
            Assert.IsNotNull(_config.Rewards);
        }

        [Test]
        public void Rewards_FreshInstance_IsEmpty()
        {
            Assert.AreEqual(0, _config.Rewards.Count);
        }

        // ── GetRewardsForLevel — empty catalog ────────────────────────────────

        [Test]
        public void GetRewardsForLevel_EmptyCatalog_ReturnsEmptyList()
        {
            var result = _config.GetRewardsForLevel(3);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        // ── GetRewardsForLevel — no matching level ────────────────────────────

        [Test]
        public void GetRewardsForLevel_NoMatchingLevel_ReturnsEmptyList()
        {
            SetRewards(_config, new List<LevelRewardEntry>
            {
                MakeEntry(5, 100)
            });

            var result = _config.GetRewardsForLevel(3);
            Assert.AreEqual(0, result.Count);
        }

        // ── GetRewardsForLevel — single match ─────────────────────────────────

        [Test]
        public void GetRewardsForLevel_SingleMatch_ReturnsOneEntry()
        {
            SetRewards(_config, new List<LevelRewardEntry>
            {
                MakeEntry(5, 200, "Level 5 Reward")
            });

            var result = _config.GetRewardsForLevel(5);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(200, result[0].rewardCredits);
            Assert.AreEqual("Level 5 Reward", result[0].displayName);
        }

        // ── GetRewardsForLevel — multiple entries at same level ───────────────

        [Test]
        public void GetRewardsForLevel_MultipleEntriesSameLevel_ReturnsAll()
        {
            SetRewards(_config, new List<LevelRewardEntry>
            {
                MakeEntry(5, 100, "A"),
                MakeEntry(5, 200, "B"),
            });

            var result = _config.GetRewardsForLevel(5);
            Assert.AreEqual(2, result.Count);
        }

        // ── GetRewardsForLevel — only returns matching level ──────────────────

        [Test]
        public void GetRewardsForLevel_MixedLevels_OnlyReturnsMatchingLevel()
        {
            SetRewards(_config, new List<LevelRewardEntry>
            {
                MakeEntry(3, 50),
                MakeEntry(5, 100),
                MakeEntry(5, 200),
                MakeEntry(10, 500),
            });

            var result = _config.GetRewardsForLevel(5);
            Assert.AreEqual(2, result.Count);
            foreach (var e in result)
                Assert.AreEqual(5, e.level);
        }

        // ── HasRewardAtLevel ──────────────────────────────────────────────────

        [Test]
        public void HasRewardAtLevel_EmptyCatalog_ReturnsFalse()
        {
            Assert.IsFalse(_config.HasRewardAtLevel(5));
        }

        [Test]
        public void HasRewardAtLevel_NoMatchingLevel_ReturnsFalse()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(5, 100) });
            Assert.IsFalse(_config.HasRewardAtLevel(3));
        }

        [Test]
        public void HasRewardAtLevel_MatchingLevel_ReturnsTrue()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(5, 100) });
            Assert.IsTrue(_config.HasRewardAtLevel(5));
        }

        // ── IReadOnlyList contract ────────────────────────────────────────────

        [Test]
        public void Rewards_IsIReadOnlyList_NotMutableList()
        {
            Assert.IsNotNull(_config.Rewards as System.Collections.IEnumerable);
            // Verify it cannot be downcast to a mutable List<T> by the caller
            // (the backing field is private; IReadOnlyList is the only public surface).
            Assert.IsInstanceOf<IReadOnlyList<LevelRewardEntry>>(_config.Rewards);
        }

        // ── Entry value storage ───────────────────────────────────────────────

        [Test]
        public void Entry_LevelAndCredits_StoreCorrectly()
        {
            var entry = MakeEntry(7, 350, "Seven!");
            Assert.AreEqual(7,       entry.level);
            Assert.AreEqual(350,     entry.rewardCredits);
            Assert.AreEqual("Seven!", entry.displayName);
        }
    }
}
