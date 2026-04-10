using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerPartUpgrades"/>.
    ///
    /// Covers:
    ///   • GetTier returns 0 for unknown / null / empty ids on a fresh instance
    ///   • SetTier stores tier and fires event
    ///   • SetTier with negative value clamps to 0
    ///   • SetTier called twice — last value wins
    ///   • SetTier with null/empty partId — no-op, event not fired
    ///   • LoadSnapshot rehydrates correctly (null lists, negative values, empties)
    ///   • TakeSnapshot returns a copy of current state
    ///   • Reset clears all data and Count returns 0
    ///   • Count reflects number of distinct upgraded parts
    /// </summary>
    public class PlayerPartUpgradesTests
    {
        private PlayerPartUpgrades _upgrades;
        private VoidGameEvent      _event;

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        [SetUp]
        public void SetUp()
        {
            _upgrades = ScriptableObject.CreateInstance<PlayerPartUpgrades>();
            _event    = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_upgrades, "_onUpgradesChanged", _event);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_upgrades);
            Object.DestroyImmediate(_event);
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_GetTier_UnknownId_ReturnsZero()
        {
            Assert.AreEqual(0, _upgrades.GetTier("arm_heavy"));
        }

        [Test]
        public void FreshInstance_GetTier_NullId_ReturnsZero()
        {
            Assert.AreEqual(0, _upgrades.GetTier(null));
        }

        [Test]
        public void FreshInstance_GetTier_EmptyId_ReturnsZero()
        {
            Assert.AreEqual(0, _upgrades.GetTier(""));
        }

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            Assert.AreEqual(0, _upgrades.Count);
        }

        // ── SetTier ───────────────────────────────────────────────────────────

        [Test]
        public void SetTier_StoresTier_GetTierReturnsIt()
        {
            _upgrades.SetTier("arm_heavy", 2);
            Assert.AreEqual(2, _upgrades.GetTier("arm_heavy"));
        }

        [Test]
        public void SetTier_NegativeTier_ClampedToZero()
        {
            _upgrades.SetTier("arm_heavy", -5);
            Assert.AreEqual(0, _upgrades.GetTier("arm_heavy"));
        }

        [Test]
        public void SetTier_FiresEvent()
        {
            bool fired = false;
            _event.RegisterCallback(() => fired = true);
            _upgrades.SetTier("arm_heavy", 1);
            Assert.IsTrue(fired);
        }

        [Test]
        public void SetTier_NullId_NoThrow_EventNotFired()
        {
            bool fired = false;
            _event.RegisterCallback(() => fired = true);
            Assert.DoesNotThrow(() => _upgrades.SetTier(null, 1));
            Assert.IsFalse(fired);
        }

        [Test]
        public void SetTier_EmptyId_NoThrow_EventNotFired()
        {
            bool fired = false;
            _event.RegisterCallback(() => fired = true);
            Assert.DoesNotThrow(() => _upgrades.SetTier("", 1));
            Assert.IsFalse(fired);
        }

        [Test]
        public void SetTier_CalledTwice_LastValueWins()
        {
            _upgrades.SetTier("wheel_fast", 1);
            _upgrades.SetTier("wheel_fast", 3);
            Assert.AreEqual(3, _upgrades.GetTier("wheel_fast"));
        }

        [Test]
        public void SetTier_MultipleParts_IndependentlyStored()
        {
            _upgrades.SetTier("arm_heavy", 2);
            _upgrades.SetTier("wheel_fast", 1);
            Assert.AreEqual(2, _upgrades.GetTier("arm_heavy"));
            Assert.AreEqual(1, _upgrades.GetTier("wheel_fast"));
        }

        [Test]
        public void SetTier_Count_ReflectsDistinctParts()
        {
            _upgrades.SetTier("arm_heavy", 1);
            _upgrades.SetTier("wheel_fast", 2);
            Assert.AreEqual(2, _upgrades.Count);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_RehydratesTiers()
        {
            var keys   = new List<string> { "arm_heavy", "wheel_fast" };
            var values = new List<int>    { 2, 1 };
            _upgrades.LoadSnapshot(keys, values);
            Assert.AreEqual(2, _upgrades.GetTier("arm_heavy"));
            Assert.AreEqual(1, _upgrades.GetTier("wheel_fast"));
        }

        [Test]
        public void LoadSnapshot_NullLists_ResetsToEmpty()
        {
            _upgrades.SetTier("arm_heavy", 3);
            _upgrades.LoadSnapshot(null, null);
            Assert.AreEqual(0, _upgrades.Count);
            Assert.AreEqual(0, _upgrades.GetTier("arm_heavy"));
        }

        [Test]
        public void LoadSnapshot_NegativeValues_ClampedToZero()
        {
            var keys   = new List<string> { "arm_heavy" };
            var values = new List<int>    { -2 };
            _upgrades.LoadSnapshot(keys, values);
            Assert.AreEqual(0, _upgrades.GetTier("arm_heavy"));
        }

        [Test]
        public void LoadSnapshot_DoesNotFireEvent()
        {
            bool fired = false;
            _event.RegisterCallback(() => fired = true);
            _upgrades.LoadSnapshot(new List<string> { "arm_heavy" }, new List<int> { 1 });
            Assert.IsFalse(fired);
        }

        // ── TakeSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_ReturnsCurrentState()
        {
            _upgrades.SetTier("arm_heavy", 2);
            _upgrades.TakeSnapshot(out List<string> keys, out List<int> values);
            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual("arm_heavy", keys[0]);
            Assert.AreEqual(2, values[0]);
        }

        [Test]
        public void TakeSnapshot_FreshInstance_ReturnsEmptyLists()
        {
            _upgrades.TakeSnapshot(out List<string> keys, out List<int> values);
            Assert.AreEqual(0, keys.Count);
            Assert.AreEqual(0, values.Count);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllData()
        {
            _upgrades.SetTier("arm_heavy", 3);
            _upgrades.Reset();
            Assert.AreEqual(0, _upgrades.GetTier("arm_heavy"));
        }

        [Test]
        public void Reset_CountIsZero()
        {
            _upgrades.SetTier("arm_heavy", 1);
            _upgrades.Reset();
            Assert.AreEqual(0, _upgrades.Count);
        }

        [Test]
        public void Reset_DoesNotFireEvent()
        {
            _upgrades.SetTier("arm_heavy", 1);
            bool fired = false;
            _event.RegisterCallback(() => fired = true);
            _upgrades.Reset();
            Assert.IsFalse(fired);
        }
    }
}
