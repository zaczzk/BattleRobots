using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchStatisticsSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (all zeros).
    ///   • RecordDamageDealt: accumulation, hit-count increment, zero/negative guard.
    ///   • RecordDamageTaken: accumulation, hits-received increment, zero/negative guard.
    ///   • DamageInfo overloads: correct extraction of <see cref="DamageInfo.amount"/>.
    ///   • DamageEfficiency: equal damage (0.5), only dealt (1.0), only taken (0.0),
    ///     fresh instance (0.0 safe division).
    ///   • Reset: clears all four accumulators; idempotent on fresh instance.
    ///
    /// All tests use <c>ScriptableObject.CreateInstance</c> — no scene or AssetDatabase
    /// required.  The SO is destroyed in TearDown to avoid asset leaks.
    /// </summary>
    public class MatchStatisticsSOTests
    {
        private MatchStatisticsSO _stats;

        [SetUp]
        public void SetUp()
        {
            _stats = ScriptableObject.CreateInstance<MatchStatisticsSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stats);
            _stats = null;
        }

        // ── Fresh-instance defaults ────────────────────────────────────────────

        [Test]
        public void FreshInstance_TotalDamageDealt_IsZero()
        {
            Assert.AreEqual(0f, _stats.TotalDamageDealt, 0.0001f);
        }

        [Test]
        public void FreshInstance_TotalDamageTaken_IsZero()
        {
            Assert.AreEqual(0f, _stats.TotalDamageTaken, 0.0001f);
        }

        [Test]
        public void FreshInstance_HitCount_IsZero()
        {
            Assert.AreEqual(0, _stats.HitCount);
        }

        [Test]
        public void FreshInstance_HitsReceived_IsZero()
        {
            Assert.AreEqual(0, _stats.HitsReceived);
        }

        [Test]
        public void FreshInstance_DamageEfficiency_IsZero()
        {
            // Safe division: no hits means 0, not NaN.
            Assert.AreEqual(0f, _stats.DamageEfficiency, 0.0001f);
        }

        // ── RecordDamageDealt ──────────────────────────────────────────────────

        [Test]
        public void RecordDamageDealt_Positive_AccumulatesTotalAndIncreasesHitCount()
        {
            _stats.RecordDamageDealt(50f);

            Assert.AreEqual(50f, _stats.TotalDamageDealt, 0.0001f);
            Assert.AreEqual(1,   _stats.HitCount);
        }

        [Test]
        public void RecordDamageDealt_CalledTwice_AccumulatesBothAmounts()
        {
            _stats.RecordDamageDealt(30f);
            _stats.RecordDamageDealt(20f);

            Assert.AreEqual(50f, _stats.TotalDamageDealt, 0.0001f);
            Assert.AreEqual(2,   _stats.HitCount);
        }

        [Test]
        public void RecordDamageDealt_ZeroAmount_DoesNotChangeTotalOrHitCount()
        {
            _stats.RecordDamageDealt(0f);

            Assert.AreEqual(0f, _stats.TotalDamageDealt, 0.0001f);
            Assert.AreEqual(0,  _stats.HitCount);
        }

        [Test]
        public void RecordDamageDealt_NegativeAmount_DoesNotChangeTotalOrHitCount()
        {
            _stats.RecordDamageDealt(-10f);

            Assert.AreEqual(0f, _stats.TotalDamageDealt, 0.0001f);
            Assert.AreEqual(0,  _stats.HitCount);
        }

        // ── RecordDamageTaken ──────────────────────────────────────────────────

        [Test]
        public void RecordDamageTaken_Positive_AccumulatesTotalAndIncreasesHitsReceived()
        {
            _stats.RecordDamageTaken(40f);

            Assert.AreEqual(40f, _stats.TotalDamageTaken, 0.0001f);
            Assert.AreEqual(1,   _stats.HitsReceived);
        }

        [Test]
        public void RecordDamageTaken_CalledTwice_AccumulatesBothAmounts()
        {
            _stats.RecordDamageTaken(25f);
            _stats.RecordDamageTaken(15f);

            Assert.AreEqual(40f, _stats.TotalDamageTaken, 0.0001f);
            Assert.AreEqual(2,   _stats.HitsReceived);
        }

        [Test]
        public void RecordDamageTaken_ZeroAmount_DoesNotChangeTotalOrHitsReceived()
        {
            _stats.RecordDamageTaken(0f);

            Assert.AreEqual(0f, _stats.TotalDamageTaken, 0.0001f);
            Assert.AreEqual(0,  _stats.HitsReceived);
        }

        // ── DamageInfo overloads ───────────────────────────────────────────────

        [Test]
        public void RecordDamageDealt_DamageInfo_ExtractsAmountCorrectly()
        {
            var info = new DamageInfo(75f, "player");
            _stats.RecordDamageDealt(info);

            Assert.AreEqual(75f, _stats.TotalDamageDealt, 0.0001f);
            Assert.AreEqual(1,   _stats.HitCount);
        }

        [Test]
        public void RecordDamageTaken_DamageInfo_ExtractsAmountCorrectly()
        {
            var info = new DamageInfo(35f, "enemy");
            _stats.RecordDamageTaken(info);

            Assert.AreEqual(35f, _stats.TotalDamageTaken, 0.0001f);
            Assert.AreEqual(1,   _stats.HitsReceived);
        }

        // ── DamageEfficiency ───────────────────────────────────────────────────

        [Test]
        public void DamageEfficiency_EqualDamage_IsPointFive()
        {
            _stats.RecordDamageDealt(50f);
            _stats.RecordDamageTaken(50f);

            Assert.AreEqual(0.5f, _stats.DamageEfficiency, 0.0001f);
        }

        [Test]
        public void DamageEfficiency_OnlyDamageDealt_IsOne()
        {
            _stats.RecordDamageDealt(100f);

            Assert.AreEqual(1f, _stats.DamageEfficiency, 0.0001f);
        }

        [Test]
        public void DamageEfficiency_OnlyDamageTaken_IsZero()
        {
            _stats.RecordDamageTaken(100f);

            Assert.AreEqual(0f, _stats.DamageEfficiency, 0.0001f);
        }

        // ── Reset ──────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllAccumulators()
        {
            _stats.RecordDamageDealt(80f);
            _stats.RecordDamageTaken(60f);

            _stats.Reset();

            Assert.AreEqual(0f, _stats.TotalDamageDealt, 0.0001f);
            Assert.AreEqual(0f, _stats.TotalDamageTaken, 0.0001f);
            Assert.AreEqual(0,  _stats.HitCount);
            Assert.AreEqual(0,  _stats.HitsReceived);
        }

        [Test]
        public void Reset_OnFreshInstance_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _stats.Reset());
        }
    }
}
