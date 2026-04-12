using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ComboCounterSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (hitCount 0, maxCombo 0, multiplier 1, inactive).
    ///   • RecordHit — increments HitCount and sets IsComboActive.
    ///   • RecordHit — updates MaxCombo when a new high is reached.
    ///   • RecordHit — MaxCombo does not decrease on subsequent lower combos.
    ///   • RecordHit — multiplier tiers (every 5 hits adds 0.1×).
    ///   • RecordHit — multiplier capped at 2.0 for very long streaks.
    ///   • RecordHit — fires _onComboChanged event.
    ///   • RecordHit — fires _onNewMaxCombo only when MaxCombo is beaten.
    ///   • Tick — breaks combo when window expires; resets HitCount + multiplier.
    ///   • Tick — fires _onComboBreak and _onComboChanged on expiry.
    ///   • Tick — no-op when combo is not active.
    ///   • ComboWindowRatio — correct ratio while active, 0 when inactive.
    ///   • Reset — clears all state including MaxCombo; fires no events.
    /// </summary>
    public class ComboCounterSOTests
    {
        private ComboCounterSO _so;

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
            _so = ScriptableObject.CreateInstance<ComboCounterSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_HitCountIsZero()
        {
            Assert.AreEqual(0, _so.HitCount);
        }

        [Test]
        public void FreshInstance_MaxComboIsZero()
        {
            Assert.AreEqual(0, _so.MaxCombo);
        }

        [Test]
        public void FreshInstance_MultiplierIsOne()
        {
            Assert.AreEqual(1f, _so.ComboMultiplier, 0.001f);
        }

        [Test]
        public void FreshInstance_IsComboActiveFalse()
        {
            Assert.IsFalse(_so.IsComboActive);
        }

        // ── RecordHit ─────────────────────────────────────────────────────────

        [Test]
        public void RecordHit_IncrementsHitCount()
        {
            _so.RecordHit();
            _so.RecordHit();

            Assert.AreEqual(2, _so.HitCount);
        }

        [Test]
        public void RecordHit_SetsIsComboActive()
        {
            _so.RecordHit();

            Assert.IsTrue(_so.IsComboActive);
        }

        [Test]
        public void RecordHit_UpdatesMaxComboWhenExceeded()
        {
            _so.RecordHit();
            _so.RecordHit();
            _so.RecordHit();

            Assert.AreEqual(3, _so.MaxCombo);
        }

        [Test]
        public void RecordHit_MultiplierAtFiveHitsTierOne()
        {
            for (int i = 0; i < 5; i++) _so.RecordHit();

            // floor(5/5)*0.1 = 0.1 → multiplier = 1.1
            Assert.AreEqual(1.1f, _so.ComboMultiplier, 0.001f);
        }

        [Test]
        public void RecordHit_MultiplierAtTenHitsTierTwo()
        {
            for (int i = 0; i < 10; i++) _so.RecordHit();

            // floor(10/5)*0.1 = 0.2 → multiplier = 1.2
            Assert.AreEqual(1.2f, _so.ComboMultiplier, 0.001f);
        }

        [Test]
        public void RecordHit_MultiplierCappedAtTwo()
        {
            // 50 hits: floor(50/5)*0.1 = 1.0 → 1.0+1.0=2.0 (cap reached)
            // 55 hits: floor(55/5)*0.1 = 1.1 → clamped to 2.0
            for (int i = 0; i < 55; i++) _so.RecordHit();

            Assert.AreEqual(2f, _so.ComboMultiplier, 0.001f);
        }

        [Test]
        public void RecordHit_FiresOnComboChangedEvent()
        {
            var evt     = ScriptableObject.CreateInstance<VoidGameEvent>();
            int counter = 0;
            evt.RegisterCallback(() => counter++);
            SetField(_so, "_onComboChanged", evt);

            _so.RecordHit();
            _so.RecordHit();

            Assert.AreEqual(2, counter);

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void RecordHit_FiresOnNewMaxComboOnlyWhenBeaten()
        {
            var evt       = ScriptableObject.CreateInstance<VoidGameEvent>();
            int fireCount = 0;
            evt.RegisterCallback(() => fireCount++);
            SetField(_so, "_onNewMaxCombo", evt);

            // Hit 1, 2, 3 — each beats the previous max → 3 fires.
            _so.RecordHit();
            _so.RecordHit();
            _so.RecordHit();
            int afterThree = fireCount;

            // Break then re-hit to 2 — does NOT beat MaxCombo of 3 → no more fires.
            // Simulate break by ticking past window
            SetField(_so, "_comboTimer", -1f);
            _so.Tick(0.001f); // trigger break
            _so.RecordHit();
            _so.RecordHit();

            Assert.AreEqual(3, afterThree, "Expected 3 fires for hits 1, 2, 3");
            Assert.AreEqual(3, fireCount,  "No additional fires for sub-max combo");

            Object.DestroyImmediate(evt);
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        [Test]
        public void Tick_BreaksComboWhenWindowExpires()
        {
            _so.RecordHit();
            _so.RecordHit();

            // Force timer to near-zero then tick past it.
            SetField(_so, "_comboTimer", 0.05f);
            _so.Tick(0.1f);

            Assert.AreEqual(0, _so.HitCount);
            Assert.IsFalse(_so.IsComboActive);
            Assert.AreEqual(1f, _so.ComboMultiplier, 0.001f);
        }

        [Test]
        public void Tick_FiresBreakAndChangedEventsOnExpiry()
        {
            var breakEvt    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var changedEvt  = ScriptableObject.CreateInstance<VoidGameEvent>();
            int breakCount  = 0;
            int changedCount = 0;
            breakEvt.RegisterCallback(() => breakCount++);
            changedEvt.RegisterCallback(() => changedCount++);
            SetField(_so, "_onComboBreak",   breakEvt);
            SetField(_so, "_onComboChanged", changedEvt);

            _so.RecordHit(); // changedCount = 1
            SetField(_so, "_comboTimer", 0.05f);
            _so.Tick(0.1f);  // break fires both events

            Assert.AreEqual(1, breakCount,  "OnComboBreak should fire once on expiry");
            Assert.AreEqual(2, changedCount, "OnComboChanged fired on hit + on break");

            Object.DestroyImmediate(breakEvt);
            Object.DestroyImmediate(changedEvt);
        }

        [Test]
        public void Tick_NoOpWhenComboNotActive()
        {
            // Should not throw or modify state when HitCount == 0.
            Assert.DoesNotThrow(() => _so.Tick(10f));
            Assert.AreEqual(0, _so.HitCount);
        }

        // ── ComboWindowRatio ──────────────────────────────────────────────────

        [Test]
        public void ComboWindowRatio_IsZeroWhenInactive()
        {
            Assert.AreEqual(0f, _so.ComboWindowRatio, 0.001f);
        }

        [Test]
        public void ComboWindowRatio_IsOneImmediatelyAfterHit()
        {
            _so.RecordHit();

            // Timer was just set to ComboWindowSeconds, so ratio should be 1.
            Assert.AreEqual(1f, _so.ComboWindowRatio, 0.001f);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllStateIncludingMaxCombo()
        {
            _so.RecordHit();
            _so.RecordHit();
            _so.RecordHit();
            _so.Reset();

            Assert.AreEqual(0,  _so.HitCount);
            Assert.AreEqual(0,  _so.MaxCombo);
            Assert.AreEqual(1f, _so.ComboMultiplier, 0.001f);
            Assert.IsFalse(_so.IsComboActive);
            Assert.AreEqual(0f, _so.ComboWindowRatio, 0.001f);
        }

        [Test]
        public void Reset_FiresNoEvents()
        {
            var evt     = ScriptableObject.CreateInstance<VoidGameEvent>();
            int counter = 0;
            evt.RegisterCallback(() => counter++);
            SetField(_so, "_onComboChanged", evt);
            SetField(_so, "_onComboBreak",   evt);
            SetField(_so, "_onNewMaxCombo",  evt);

            _so.RecordHit(); // +1 event (comboChanged)
            int beforeReset = counter;
            _so.Reset();

            Assert.AreEqual(beforeReset, counter, "Reset must not fire any events.");

            Object.DestroyImmediate(evt);
        }
    }
}
