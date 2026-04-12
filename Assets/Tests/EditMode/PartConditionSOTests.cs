using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartConditionSO"/>.
    ///
    /// Covers:
    ///   • OnEnable initialises CurrentHP to MaxHP and IsDestroyed to false.
    ///   • HPRatio is 1.0 on a fresh instance.
    ///   • TakeDamage reduces CurrentHP.
    ///   • TakeDamage clamps CurrentHP to 0 (no negative HP).
    ///   • TakeDamage sets IsDestroyed when HP reaches zero.
    ///   • TakeDamage fires _onPartDestroyed event exactly once on destruction.
    ///   • TakeDamage is a no-op on an already-destroyed part.
    ///   • TakeDamage ignores zero and negative amounts.
    ///   • Repair restores CurrentHP.
    ///   • Repair clamps to MaxHP (no overflow).
    ///   • Repair revives a destroyed part (IsDestroyed → false).
    ///   • ResetToMax restores HP and clears destroyed state; fires no events.
    /// </summary>
    public class PartConditionSOTests
    {
        private PartConditionSO _so;

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
            _so = ScriptableObject.CreateInstance<PartConditionSO>();
            // OnEnable is called by CreateInstance, setting CurrentHP = MaxHP (50).
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── OnEnable / fresh-instance ─────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentHPEqualsMaxHP()
        {
            Assert.AreEqual(_so.MaxHP, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void FreshInstance_IsDestroyedFalse()
        {
            Assert.IsFalse(_so.IsDestroyed);
        }

        [Test]
        public void FreshInstance_HPRatioIsOne()
        {
            Assert.AreEqual(1f, _so.HPRatio, 0.001f);
        }

        // ── TakeDamage ────────────────────────────────────────────────────────

        [Test]
        public void TakeDamage_ReducesCurrentHP()
        {
            float before = _so.CurrentHP;
            _so.TakeDamage(10f);

            Assert.AreEqual(before - 10f, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void TakeDamage_ClampsCurrentHPAtZero()
        {
            _so.TakeDamage(_so.MaxHP + 999f);

            Assert.AreEqual(0f, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void TakeDamage_SetsIsDestroyedWhenHPReachesZero()
        {
            _so.TakeDamage(_so.MaxHP);

            Assert.IsTrue(_so.IsDestroyed);
        }

        [Test]
        public void TakeDamage_FiresOnPartDestroyedEventExactlyOnce()
        {
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            evt.RegisterCallback(() => count++);
            SetField(_so, "_onPartDestroyed", evt);

            _so.TakeDamage(_so.MaxHP);       // destroys the part
            _so.TakeDamage(1f);              // should be a no-op (already destroyed)

            Assert.AreEqual(1, count, "Event must fire exactly once on first destruction.");

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void TakeDamage_NoOpOnAlreadyDestroyedPart()
        {
            _so.TakeDamage(_so.MaxHP); // destroy
            float hpAfterDestroy = _so.CurrentHP; // 0

            _so.TakeDamage(999f); // should not change HP further

            Assert.AreEqual(hpAfterDestroy, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void TakeDamage_IgnoresZeroAmount()
        {
            float before = _so.CurrentHP;
            _so.TakeDamage(0f);

            Assert.AreEqual(before, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void TakeDamage_IgnoresNegativeAmount()
        {
            float before = _so.CurrentHP;
            _so.TakeDamage(-5f);

            Assert.AreEqual(before, _so.CurrentHP, 0.001f);
        }

        // ── Repair ────────────────────────────────────────────────────────────

        [Test]
        public void Repair_RestoresCurrentHP()
        {
            _so.TakeDamage(20f);
            float afterDamage = _so.CurrentHP;

            _so.Repair(10f);

            Assert.AreEqual(afterDamage + 10f, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void Repair_ClampsAtMaxHP()
        {
            _so.TakeDamage(5f);
            _so.Repair(_so.MaxHP * 10f);

            Assert.AreEqual(_so.MaxHP, _so.CurrentHP, 0.001f);
        }

        [Test]
        public void Repair_RevivesDestroyedPart()
        {
            _so.TakeDamage(_so.MaxHP); // destroy
            Assert.IsTrue(_so.IsDestroyed, "Pre-condition: part must be destroyed");

            _so.Repair(1f);

            Assert.IsFalse(_so.IsDestroyed, "Repair should revive a destroyed part");
        }

        // ── ResetToMax ────────────────────────────────────────────────────────

        [Test]
        public void ResetToMax_RestoresHPAndClearsDestroyedState()
        {
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            evt.RegisterCallback(() => count++);
            SetField(_so, "_onPartDestroyed", evt);

            _so.TakeDamage(_so.MaxHP); // destroy
            int beforeReset = count;
            _so.ResetToMax();

            Assert.AreEqual(_so.MaxHP, _so.CurrentHP, 0.001f);
            Assert.IsFalse(_so.IsDestroyed);
            Assert.AreEqual(beforeReset, count, "ResetToMax must not fire any events.");

            Object.DestroyImmediate(evt);
        }

        // ── LoadSnapshot ─────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_FullRatio_RestoresMaxHP()
        {
            _so.TakeDamage(25f); // damage the part first
            _so.LoadSnapshot(1f);

            Assert.AreEqual(_so.MaxHP, _so.CurrentHP, 0.001f);
            Assert.IsFalse(_so.IsDestroyed);
        }

        [Test]
        public void LoadSnapshot_HalfRatio_SetsHalfHP()
        {
            _so.LoadSnapshot(0.5f);

            Assert.AreEqual(_so.MaxHP * 0.5f, _so.CurrentHP, 0.001f);
            Assert.IsFalse(_so.IsDestroyed);
        }

        [Test]
        public void LoadSnapshot_ZeroRatio_SetsDestroyedState()
        {
            _so.LoadSnapshot(0f);

            Assert.AreEqual(0f, _so.CurrentHP, 0.001f);
            Assert.IsTrue(_so.IsDestroyed, "HPRatio 0 should mark part as destroyed.");
        }

        [Test]
        public void LoadSnapshot_RatioAboveOne_ClampsToMaxHP()
        {
            _so.LoadSnapshot(2f);

            Assert.AreEqual(_so.MaxHP, _so.CurrentHP, 0.001f,
                "Ratio > 1 must be clamped to MaxHP.");
        }

        [Test]
        public void LoadSnapshot_NegativeRatio_ClampsToZeroAndDestroyed()
        {
            _so.LoadSnapshot(-0.5f);

            Assert.AreEqual(0f, _so.CurrentHP, 0.001f,
                "Negative ratio must be clamped to 0.");
            Assert.IsTrue(_so.IsDestroyed);
        }
    }
}
