using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="OpponentRosterSO"/>.
    ///
    /// Covers:
    ///   • Fresh instance: list is not null and empty.
    ///   • IReadOnlyList contract.
    ///   • Insertion of one and two profiles — count matches; insertion order preserved.
    ///   • Null entry is allowed in the backing list (OnValidate warns but does not strip).
    /// </summary>
    public class OpponentRosterSOTests
    {
        private OpponentRosterSO   _roster;
        private OpponentProfileSO  _profileA;
        private OpponentProfileSO  _profileB;

        // ── Helpers ───────────────────────────────────────────────────────────

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
            _roster   = ScriptableObject.CreateInstance<OpponentRosterSO>();
            _profileA = ScriptableObject.CreateInstance<OpponentProfileSO>();
            _profileB = ScriptableObject.CreateInstance<OpponentProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_roster);
            Object.DestroyImmediate(_profileA);
            Object.DestroyImmediate(_profileB);
            _roster   = null;
            _profileA = null;
            _profileB = null;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Opponents_IsNotNull()
        {
            Assert.IsNotNull(_roster.Opponents);
        }

        [Test]
        public void FreshInstance_Opponents_IsEmpty()
        {
            Assert.AreEqual(0, _roster.Opponents.Count);
        }

        [Test]
        public void Opponents_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<System.Collections.Generic.IReadOnlyList<OpponentProfileSO>>(
                _roster.Opponents);
        }

        // ── With entries ──────────────────────────────────────────────────────

        [Test]
        public void WithOneEntry_Count_IsOne()
        {
            SetField(_roster, "_opponents", new List<OpponentProfileSO> { _profileA });
            Assert.AreEqual(1, _roster.Opponents.Count);
        }

        [Test]
        public void WithTwoEntries_Count_IsTwo()
        {
            SetField(_roster, "_opponents", new List<OpponentProfileSO> { _profileA, _profileB });
            Assert.AreEqual(2, _roster.Opponents.Count);
        }

        [Test]
        public void PreservesInsertionOrder()
        {
            SetField(_roster, "_opponents", new List<OpponentProfileSO> { _profileA, _profileB });
            Assert.AreSame(_profileA, _roster.Opponents[0]);
            Assert.AreSame(_profileB, _roster.Opponents[1]);
        }

        [Test]
        public void NullEntry_IsPreservedInList()
        {
            // OnValidate warns but never strips null entries — test that the list
            // still reflects what was injected (null-safe consumer responsibility).
            SetField(_roster, "_opponents", new List<OpponentProfileSO> { null, _profileA });
            Assert.AreEqual(2, _roster.Opponents.Count);
            Assert.IsNull(_roster.Opponents[0]);
            Assert.AreSame(_profileA, _roster.Opponents[1]);
        }
    }
}
