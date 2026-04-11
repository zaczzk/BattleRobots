using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchTimerWarningSO"/> and its nested
    /// <see cref="MatchTimerWarningSO.TimerThreshold"/> struct.
    ///
    /// Covers:
    ///   • Fresh-instance defaults: <see cref="MatchTimerWarningSO.Thresholds"/> not-null / empty.
    ///   • <see cref="MatchTimerWarningSO.Thresholds"/> exposes an <see cref="IReadOnlyList{T}"/>.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: empty-list no-throw.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: above-threshold does NOT fire.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: at-threshold fires exactly once.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: below-threshold fires exactly once.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: fires only once per threshold (no re-fire).
    ///   • <see cref="MatchTimerWarningSO.Reset"/> allows re-firing.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: null WarningEvent does not throw.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: multiple thresholds — only relevant fire.
    ///   • <see cref="MatchTimerWarningSO.CheckAndFire"/>: multiple thresholds — all fire when below.
    ///   • <see cref="MatchTimerWarningSO.Reset"/> on fresh instance does not throw.
    ///   • Negative secondsRemaining does not throw and fires thresholds with SecondsRemaining ≥ 0.
    ///   • Zero secondsRemaining fires all thresholds (time up).
    /// </summary>
    public class MatchTimerWarningSOTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────────

        private MatchTimerWarningSO _so;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Builds a <see cref="MatchTimerWarningSO.TimerThreshold"/> struct value.
        /// Struct fields are public, so no reflection needed.
        /// </summary>
        private static MatchTimerWarningSO.TimerThreshold MakeThreshold(
            float seconds, VoidGameEvent evt = null)
        {
            return new MatchTimerWarningSO.TimerThreshold
            {
                SecondsRemaining = seconds,
                WarningEvent     = evt
            };
        }

        /// <summary>
        /// Creates a <see cref="VoidGameEvent"/> wired to an external counter.
        /// Returns the event; out-param gives access to the counter.
        /// </summary>
        private static VoidGameEvent MakeCounterEvent(out int[] counter)
        {
            int[] c = { 0 };
            counter = c;
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            evt.RegisterCallback(() => c[0]++);
            return evt;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<MatchTimerWarningSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── 1. FreshInstance_Thresholds_NotNull ───────────────────────────────────

        [Test]
        public void FreshInstance_Thresholds_NotNull()
        {
            Assert.IsNotNull(_so.Thresholds,
                "Thresholds must not be null on a fresh instance.");
        }

        // ── 2. FreshInstance_Thresholds_IsEmpty ───────────────────────────────────

        [Test]
        public void FreshInstance_Thresholds_IsEmpty()
        {
            Assert.AreEqual(0, _so.Thresholds.Count,
                "Thresholds should be empty on a fresh instance.");
        }

        // ── 3. Thresholds_IsIReadOnlyList ─────────────────────────────────────────

        [Test]
        public void Thresholds_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<MatchTimerWarningSO.TimerThreshold>>(
                _so.Thresholds,
                "Thresholds property must return IReadOnlyList<TimerThreshold>.");
        }

        // ── 4. CheckAndFire_EmptyThresholds_DoesNotThrow ──────────────────────────

        [Test]
        public void CheckAndFire_EmptyThresholds_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.CheckAndFire(30f),
                "CheckAndFire on an empty threshold list must not throw.");
        }

        // ── 5. CheckAndFire_AboveThreshold_DoesNotFire ────────────────────────────

        [Test]
        public void CheckAndFire_AboveThreshold_DoesNotFire()
        {
            var evt = MakeCounterEvent(out int[] counter);
            SetField(_so, "_thresholds",
                new List<MatchTimerWarningSO.TimerThreshold> { MakeThreshold(30f, evt) });

            _so.CheckAndFire(45f); // 45s remaining — above the 30s threshold

            Assert.AreEqual(0, counter[0],
                "Threshold must NOT fire when secondsRemaining is above its SecondsRemaining.");

            UnityEngine.Object.DestroyImmediate(evt);
        }

        // ── 6. CheckAndFire_AtThreshold_FiresOnce ────────────────────────────────

        [Test]
        public void CheckAndFire_AtThreshold_FiresOnce()
        {
            var evt = MakeCounterEvent(out int[] counter);
            SetField(_so, "_thresholds",
                new List<MatchTimerWarningSO.TimerThreshold> { MakeThreshold(30f, evt) });

            _so.CheckAndFire(30f); // exactly at threshold

            Assert.AreEqual(1, counter[0],
                "Threshold must fire exactly once when secondsRemaining == SecondsRemaining.");

            UnityEngine.Object.DestroyImmediate(evt);
        }

        // ── 7. CheckAndFire_BelowThreshold_FiresOnce ─────────────────────────────

        [Test]
        public void CheckAndFire_BelowThreshold_FiresOnce()
        {
            var evt = MakeCounterEvent(out int[] counter);
            SetField(_so, "_thresholds",
                new List<MatchTimerWarningSO.TimerThreshold> { MakeThreshold(30f, evt) });

            _so.CheckAndFire(25f); // below the 30s threshold

            Assert.AreEqual(1, counter[0],
                "Threshold must fire exactly once when secondsRemaining < SecondsRemaining.");

            UnityEngine.Object.DestroyImmediate(evt);
        }

        // ── 8. CheckAndFire_FiresOnlyOnce_PerThreshold ───────────────────────────

        [Test]
        public void CheckAndFire_FiresOnlyOnce_PerThreshold()
        {
            var evt = MakeCounterEvent(out int[] counter);
            SetField(_so, "_thresholds",
                new List<MatchTimerWarningSO.TimerThreshold> { MakeThreshold(30f, evt) });

            _so.CheckAndFire(25f); // first crossing — fires
            _so.CheckAndFire(20f); // second call — must NOT fire again
            _so.CheckAndFire(10f); // third call — must NOT fire again

            Assert.AreEqual(1, counter[0],
                "Each threshold may fire at most once per match (without calling Reset).");

            UnityEngine.Object.DestroyImmediate(evt);
        }

        // ── 9. Reset_AllowsReFire ─────────────────────────────────────────────────

        [Test]
        public void Reset_AllowsReFire()
        {
            var evt = MakeCounterEvent(out int[] counter);
            SetField(_so, "_thresholds",
                new List<MatchTimerWarningSO.TimerThreshold> { MakeThreshold(30f, evt) });

            _so.CheckAndFire(25f); // fires once (counter = 1)
            _so.Reset();           // clear fired set
            _so.CheckAndFire(25f); // fires again (counter = 2)

            Assert.AreEqual(2, counter[0],
                "After Reset() the threshold must be allowed to fire again.");

            UnityEngine.Object.DestroyImmediate(evt);
        }

        // ── 10. CheckAndFire_NullEvent_DoesNotThrow ──────────────────────────────

        [Test]
        public void CheckAndFire_NullEvent_DoesNotThrow()
        {
            // Threshold with null WarningEvent — must fire silently without crashing.
            SetField(_so, "_thresholds",
                new List<MatchTimerWarningSO.TimerThreshold> { MakeThreshold(30f, null) });

            Assert.DoesNotThrow(() => _so.CheckAndFire(20f),
                "Threshold with null WarningEvent must not throw.");
        }

        // ── 11. CheckAndFire_MultipleThresholds_OnlyFiresRelevant ────────────────

        [Test]
        public void CheckAndFire_MultipleThresholds_OnlyFiresRelevant()
        {
            var evt30 = MakeCounterEvent(out int[] c30);
            var evt10 = MakeCounterEvent(out int[] c10);

            SetField(_so, "_thresholds", new List<MatchTimerWarningSO.TimerThreshold>
            {
                MakeThreshold(30f, evt30),
                MakeThreshold(10f, evt10)
            });

            _so.CheckAndFire(25f); // below 30 threshold but ABOVE 10 threshold

            Assert.AreEqual(1, c30[0], "30s threshold must fire at 25s remaining.");
            Assert.AreEqual(0, c10[0], "10s threshold must NOT fire at 25s remaining.");

            UnityEngine.Object.DestroyImmediate(evt30);
            UnityEngine.Object.DestroyImmediate(evt10);
        }

        // ── 12. CheckAndFire_MultipleThresholds_FiresAll ─────────────────────────

        [Test]
        public void CheckAndFire_MultipleThresholds_FiresAll()
        {
            var evt60 = MakeCounterEvent(out int[] c60);
            var evt30 = MakeCounterEvent(out int[] c30);
            var evt10 = MakeCounterEvent(out int[] c10);

            SetField(_so, "_thresholds", new List<MatchTimerWarningSO.TimerThreshold>
            {
                MakeThreshold(60f, evt60),
                MakeThreshold(30f, evt30),
                MakeThreshold(10f, evt10)
            });

            _so.CheckAndFire(5f); // below all thresholds — all three must fire

            Assert.AreEqual(1, c60[0], "60s threshold must fire at 5s remaining.");
            Assert.AreEqual(1, c30[0], "30s threshold must fire at 5s remaining.");
            Assert.AreEqual(1, c10[0], "10s threshold must fire at 5s remaining.");

            UnityEngine.Object.DestroyImmediate(evt60);
            UnityEngine.Object.DestroyImmediate(evt30);
            UnityEngine.Object.DestroyImmediate(evt10);
        }

        // ── 13. Reset_OnFreshInstance_DoesNotThrow ────────────────────────────────

        [Test]
        public void Reset_OnFreshInstance_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.Reset(),
                "Reset() on a fresh instance with an empty threshold list must not throw.");
        }

        // ── 14. CheckAndFire_NegativeTime_DoesNotThrow ───────────────────────────

        [Test]
        public void CheckAndFire_NegativeTime_DoesNotThrow()
        {
            var evt = MakeCounterEvent(out int[] counter);
            SetField(_so, "_thresholds",
                new List<MatchTimerWarningSO.TimerThreshold> { MakeThreshold(30f, evt) });

            Assert.DoesNotThrow(() => _so.CheckAndFire(-1f),
                "CheckAndFire with negative secondsRemaining must not throw.");

            // Negative time is still <= 30, so the threshold should fire.
            Assert.AreEqual(1, counter[0],
                "Threshold must fire when secondsRemaining is negative (< SecondsRemaining).");

            UnityEngine.Object.DestroyImmediate(evt);
        }

        // ── 15. CheckAndFire_ZeroTime_FiresAllThresholds ─────────────────────────

        [Test]
        public void CheckAndFire_ZeroTime_FiresAllThresholds()
        {
            var evt60 = MakeCounterEvent(out int[] c60);
            var evt10 = MakeCounterEvent(out int[] c10);

            SetField(_so, "_thresholds", new List<MatchTimerWarningSO.TimerThreshold>
            {
                MakeThreshold(60f, evt60),
                MakeThreshold(10f, evt10)
            });

            _so.CheckAndFire(0f); // time up — both thresholds (0 ≤ 60 and 0 ≤ 10) must fire

            Assert.AreEqual(1, c60[0], "60s threshold must fire when time is 0.");
            Assert.AreEqual(1, c10[0], "10s threshold must fire when time is 0.");

            UnityEngine.Object.DestroyImmediate(evt60);
            UnityEngine.Object.DestroyImmediate(evt10);
        }
    }
}
