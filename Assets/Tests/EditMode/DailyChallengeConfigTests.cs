using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DailyChallengeConfig"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (ChallengePool not-null/empty, RewardMultiplier = 2).
    ///   • IReadOnlyList contract on ChallengePool.
    ///   • Count reflects pool contents (1 and 2 entries).
    ///   • RewardMultiplier can be set via reflection.
    ///   • Pool preserves insertion order.
    /// </summary>
    public class DailyChallengeConfigTests
    {
        private DailyChallengeConfig _config;
        private BonusConditionSO     _conditionA;
        private BonusConditionSO     _conditionB;

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
            _config     = ScriptableObject.CreateInstance<DailyChallengeConfig>();
            _conditionA = ScriptableObject.CreateInstance<BonusConditionSO>();
            _conditionB = ScriptableObject.CreateInstance<BonusConditionSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_conditionA);
            Object.DestroyImmediate(_conditionB);
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ChallengePool_NotNull()
        {
            Assert.IsNotNull(_config.ChallengePool);
        }

        [Test]
        public void FreshInstance_ChallengePool_IsEmpty()
        {
            Assert.AreEqual(0, _config.ChallengePool.Count);
        }

        [Test]
        public void FreshInstance_RewardMultiplier_IsTwo()
        {
            Assert.AreEqual(2f, _config.RewardMultiplier);
        }

        [Test]
        public void FreshInstance_ChallengePool_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<BonusConditionSO>>(_config.ChallengePool);
        }

        // ── Pool contents ─────────────────────────────────────────────────────

        [Test]
        public void WithOneCondition_Count_IsOne()
        {
            SetField(_config, "_challengePool",
                new List<BonusConditionSO> { _conditionA });
            Assert.AreEqual(1, _config.ChallengePool.Count);
        }

        [Test]
        public void WithTwoConditions_Count_IsTwo()
        {
            SetField(_config, "_challengePool",
                new List<BonusConditionSO> { _conditionA, _conditionB });
            Assert.AreEqual(2, _config.ChallengePool.Count);
        }

        // ── RewardMultiplier ──────────────────────────────────────────────────

        [Test]
        public void RewardMultiplier_CanBeSetViaReflection()
        {
            SetField(_config, "_rewardMultiplier", 3.5f);
            Assert.AreEqual(3.5f, _config.RewardMultiplier);
        }

        // ── Insertion order ───────────────────────────────────────────────────

        [Test]
        public void ChallengePool_PreservesInsertionOrder()
        {
            SetField(_config, "_challengePool",
                new List<BonusConditionSO> { _conditionA, _conditionB });
            Assert.AreEqual(_conditionA, _config.ChallengePool[0]);
            Assert.AreEqual(_conditionB, _config.ChallengePool[1]);
        }
    }
}
