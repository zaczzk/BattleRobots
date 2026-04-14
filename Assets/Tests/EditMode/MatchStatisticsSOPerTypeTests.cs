using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the M25 per-type damage tracking added to
    /// <see cref="MatchStatisticsSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance per-type defaults (all zero).
    ///   • RecordDamageDealt(DamageInfo) routes amount to the correct type bucket.
    ///   • RecordDamageDealt(float) does NOT populate any type bucket.
    ///   • GetDealtByType returns 0 for unknown (out-of-range) DamageType values.
    ///   • DamageTypeRatio returns 0 when no damage dealt (safe division).
    ///   • DamageTypeRatio returns 1 when all damage is of one type.
    ///   • Reset clears all four type totals.
    ///   • Multiple RecordDamageDealt(DamageInfo) calls accumulate per bucket
    ///     and also add to TotalDamageDealt correctly.
    ///
    /// All tests use <c>ScriptableObject.CreateInstance</c> — no scene required.
    /// </summary>
    public class MatchStatisticsSOPerTypeTests
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

        // ── FreshInstance defaults ─────────────────────────────────────────────

        [Test]
        public void FreshInstance_Physical_IsZero()
        {
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Physical), 0.0001f);
        }

        [Test]
        public void FreshInstance_Energy_IsZero()
        {
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Energy), 0.0001f);
        }

        [Test]
        public void FreshInstance_Thermal_IsZero()
        {
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Thermal), 0.0001f);
        }

        [Test]
        public void FreshInstance_Shock_IsZero()
        {
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Shock), 0.0001f);
        }

        // ── RecordDamageDealt(DamageInfo) type routing ────────────────────────

        [Test]
        public void RecordDamageDealt_DamageInfo_Physical_RoutesToPhysicalBucket()
        {
            _stats.RecordDamageDealt(new DamageInfo(30f, "p", default, null, DamageType.Physical));

            Assert.AreEqual(30f, _stats.GetDealtByType(DamageType.Physical), 0.0001f);
            Assert.AreEqual(0f,  _stats.GetDealtByType(DamageType.Energy),   0.0001f);
        }

        [Test]
        public void RecordDamageDealt_DamageInfo_Energy_RoutesToEnergyBucket()
        {
            _stats.RecordDamageDealt(new DamageInfo(20f, "p", default, null, DamageType.Energy));

            Assert.AreEqual(20f, _stats.GetDealtByType(DamageType.Energy),   0.0001f);
            Assert.AreEqual(0f,  _stats.GetDealtByType(DamageType.Physical), 0.0001f);
        }

        [Test]
        public void RecordDamageDealt_DamageInfo_Thermal_RoutesToThermalBucket()
        {
            _stats.RecordDamageDealt(new DamageInfo(15f, "p", default, null, DamageType.Thermal));

            Assert.AreEqual(15f, _stats.GetDealtByType(DamageType.Thermal), 0.0001f);
        }

        [Test]
        public void RecordDamageDealt_DamageInfo_Shock_RoutesToShockBucket()
        {
            _stats.RecordDamageDealt(new DamageInfo(25f, "p", default, null, DamageType.Shock));

            Assert.AreEqual(25f, _stats.GetDealtByType(DamageType.Shock), 0.0001f);
        }

        // ── Float overload does NOT route to type buckets ─────────────────────

        [Test]
        public void RecordDamageDealt_FloatAmount_TypeBucketsRemainZero()
        {
            _stats.RecordDamageDealt(50f);

            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Physical), 0.0001f,
                "Float overload must not route to Physical bucket.");
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Energy),   0.0001f);
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Thermal),  0.0001f);
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Shock),    0.0001f);
        }

        // ── GetDealtByType unknown type ────────────────────────────────────────

        [Test]
        public void GetDealtByType_UnknownType_ReturnsZero()
        {
            _stats.RecordDamageDealt(new DamageInfo(100f, "p", default, null, DamageType.Physical));

            // Cast an out-of-range value — must not throw and must return 0.
            Assert.AreEqual(0f, _stats.GetDealtByType((DamageType)99), 0.0001f);
        }

        // ── DamageTypeRatio ────────────────────────────────────────────────────

        [Test]
        public void DamageTypeRatio_NoDamageDealt_IsZero()
        {
            // Safe division: zero total means zero ratio for every type.
            Assert.AreEqual(0f, _stats.DamageTypeRatio(DamageType.Physical), 0.0001f);
        }

        [Test]
        public void DamageTypeRatio_AllPhysical_PhysicalIsOne_OthersAreZero()
        {
            _stats.RecordDamageDealt(new DamageInfo(100f, "p", default, null, DamageType.Physical));

            Assert.AreEqual(1f, _stats.DamageTypeRatio(DamageType.Physical), 0.0001f);
            Assert.AreEqual(0f, _stats.DamageTypeRatio(DamageType.Energy),   0.0001f);
        }

        // ── Reset clears type totals ───────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllTypeTotals()
        {
            _stats.RecordDamageDealt(new DamageInfo(40f, "p", default, null, DamageType.Energy));
            _stats.RecordDamageDealt(new DamageInfo(60f, "p", default, null, DamageType.Shock));

            _stats.Reset();

            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Energy),   0.0001f);
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Shock),    0.0001f);
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Physical), 0.0001f);
            Assert.AreEqual(0f, _stats.GetDealtByType(DamageType.Thermal),  0.0001f);
        }

        // ── Multiple-type accumulation ─────────────────────────────────────────

        [Test]
        public void MultipleRecords_AccumulatePerBucketAndTotal()
        {
            _stats.RecordDamageDealt(new DamageInfo(30f, "p", default, null, DamageType.Physical));
            _stats.RecordDamageDealt(new DamageInfo(20f, "p", default, null, DamageType.Energy));
            _stats.RecordDamageDealt(new DamageInfo(10f, "p", default, null, DamageType.Physical));

            Assert.AreEqual(40f, _stats.GetDealtByType(DamageType.Physical), 0.0001f);
            Assert.AreEqual(20f, _stats.GetDealtByType(DamageType.Energy),   0.0001f);
            Assert.AreEqual(60f, _stats.TotalDamageDealt,                    0.0001f,
                "TotalDamageDealt must sum all DamageInfo calls.");
        }
    }
}
