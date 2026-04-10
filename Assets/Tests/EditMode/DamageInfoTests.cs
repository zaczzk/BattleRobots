using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the <see cref="DamageInfo"/> struct.
    ///
    /// DamageInfo is a value type with no Unity dependencies beyond Vector3,
    /// so these tests run purely on struct semantics — no SO or scene setup required.
    /// </summary>
    public class DamageInfoTests
    {
        // ── Construction ──────────────────────────────────────────────────────

        [Test]
        public void Constructor_SetsAllFields()
        {
            var hitPoint = new Vector3(1f, 2f, 3f);
            var info = new DamageInfo(25f, "robot_enemy", hitPoint);

            Assert.AreEqual(25f,           info.amount,   0.001f);
            Assert.AreEqual("robot_enemy", info.sourceId);
            Assert.AreEqual(hitPoint,      info.hitPoint);
        }

        [Test]
        public void Constructor_DefaultSourceId_IsEmptyString()
        {
            var info = new DamageInfo(10f);
            Assert.AreEqual(string.Empty, info.sourceId);
        }

        [Test]
        public void Constructor_DefaultHitPoint_IsVectorZero()
        {
            var info = new DamageInfo(10f);
            Assert.AreEqual(Vector3.zero, info.hitPoint);
        }

        [Test]
        public void Constructor_NullSourceId_IsCoercedToEmptyString()
        {
            // Constructor replaces null with string.Empty to keep FixedUpdate allocation-free.
            var info = new DamageInfo(5f, null);
            Assert.AreEqual(string.Empty, info.sourceId);
        }

        // ── Value-type semantics ───────────────────────────────────────────────

        [Test]
        public void DamageInfo_IsStruct_CopiesByValue()
        {
            var original = new DamageInfo(20f, "src", Vector3.one);
            DamageInfo copy = original;
            copy.amount = 99f; // modifying the copy must not affect the original
            Assert.AreEqual(20f, original.amount, 0.001f);
        }

        [Test]
        public void DamageInfo_ZeroAmount_IsValid()
        {
            // The struct itself imposes no constraint on amount; guards live in HealthSO.
            var info = new DamageInfo(0f);
            Assert.AreEqual(0f, info.amount, 0.001f);
        }

        // ── ToString ──────────────────────────────────────────────────────────

        [Test]
        public void ToString_ContainsAmount()
        {
            var info = new DamageInfo(42f, "src", Vector3.zero);
            string str = info.ToString();
            Assert.IsTrue(str.Contains("42"), $"Expected '42' in: {str}");
        }

        [Test]
        public void ToString_ContainsSourceId()
        {
            var info = new DamageInfo(10f, "turret_01", Vector3.zero);
            string str = info.ToString();
            Assert.IsTrue(str.Contains("turret_01"), $"Expected 'turret_01' in: {str}");
        }
    }
}
