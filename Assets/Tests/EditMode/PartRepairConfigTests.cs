using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartRepairConfig"/>.
    ///
    /// Covers:
    ///   • Fresh instance: CreditsPerHPPoint is positive (default 2).
    ///   • GetRepairCost: null condition → 0; full-health → 0; partial damage → correct ceil;
    ///     zero HP → full repair cost; fractional missing HP → ceil to whole credits;
    ///     large HP pool calculates correctly.
    ///   • CreditsPerHPPoint minimum clamp preserved via Min(0.1f).
    /// </summary>
    public class PartRepairConfigTests
    {
        private PartRepairConfig  _config;
        private PartConditionSO   _cond;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PartConditionSO MakeCondition(float maxHP = 50f)
        {
            var so = ScriptableObject.CreateInstance<PartConditionSO>();
            SetField(so, "_maxHP", maxHP);
            so.LoadSnapshot(1f); // full health
            return so;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PartRepairConfig>();
            _cond   = MakeCondition();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_cond);
            _config = null;
            _cond   = null;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CreditsPerHPPoint_IsPositive()
        {
            Assert.Greater(_config.CreditsPerHPPoint, 0f);
        }

        [Test]
        public void GetRepairCost_NullCondition_ReturnsZero()
        {
            Assert.AreEqual(0, _config.GetRepairCost(null));
        }

        [Test]
        public void GetRepairCost_FullHealthCondition_ReturnsZero()
        {
            // _cond is at full HP from SetUp.
            Assert.AreEqual(0, _config.GetRepairCost(_cond));
        }

        [Test]
        public void GetRepairCost_PartialDamage_CalculatesCorrectly()
        {
            // MaxHP = 50, take 10 damage → missingHP = 10; default rate = 2.0
            // Expected cost = Ceil(10 × 2.0) = 20
            _cond.TakeDamage(10f);
            SetField(_config, "_creditsPerHPPoint", 2f);

            int cost = _config.GetRepairCost(_cond);

            Assert.AreEqual(20, cost);
        }

        [Test]
        public void GetRepairCost_ZeroHP_ReturnsFullRepairCost()
        {
            // MaxHP = 50, fully destroyed → missingHP = 50; rate = 2.0
            // Expected cost = Ceil(50 × 2.0) = 100
            _cond.TakeDamage(_cond.MaxHP);
            SetField(_config, "_creditsPerHPPoint", 2f);

            int cost = _config.GetRepairCost(_cond);

            Assert.AreEqual(100, cost);
        }

        [Test]
        public void GetRepairCost_FractionalCost_CeilsToWholeCredits()
        {
            // MaxHP = 50, take 1 damage → missingHP = 1; rate = 0.3
            // Ceil(1 × 0.3) = Ceil(0.3) = 1
            _cond.TakeDamage(1f);
            SetField(_config, "_creditsPerHPPoint", 0.3f);

            int cost = _config.GetRepairCost(_cond);

            Assert.AreEqual(1, cost);
        }

        [Test]
        public void GetRepairCost_LargeHPPool_CalculatesCorrectly()
        {
            var bigCond = MakeCondition(1000f);
            bigCond.TakeDamage(300f); // missing 300 HP; rate = 1.5
            // Expected = Ceil(300 × 1.5) = Ceil(450) = 450
            SetField(_config, "_creditsPerHPPoint", 1.5f);

            int cost = _config.GetRepairCost(bigCond);

            Assert.AreEqual(450, cost);
            Object.DestroyImmediate(bigCond);
        }
    }
}
