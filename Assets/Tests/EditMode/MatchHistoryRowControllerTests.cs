using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchHistoryRowController"/>.
    ///
    /// Because the display-logic lives in two private static helper methods
    /// (<c>FormatDuration</c> and <c>FormatTimestamp</c>), they are invoked here
    /// via reflection so the formatting contracts can be tested without wiring up
    /// uGUI Text components.
    ///
    /// Covers:
    ///   • Setup(null) null-safety guard — must not throw.
    ///   • FormatDuration: zero, typical values, large value, negative clamp.
    ///   • FormatTimestamp: null / empty → "--" fallback; valid ISO-8601 → parsed
    ///     date containing the correct year; invalid string → returned as-is.
    /// </summary>
    public class MatchHistoryRowControllerTests
    {
        // ── Reflection — bind once per test run ───────────────────────────────

        private static readonly MethodInfo _formatDuration =
            typeof(MatchHistoryRowController).GetMethod(
                "FormatDuration",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(float) },
                null);

        private static readonly MethodInfo _formatTimestamp =
            typeof(MatchHistoryRowController).GetMethod(
                "FormatTimestamp",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

        // ── Invocation helpers ────────────────────────────────────────────────

        private static string Duration(float seconds)
            => (string)_formatDuration.Invoke(null, new object[] { seconds });

        private static string Timestamp(string raw)
            => (string)_formatTimestamp.Invoke(null, new object[] { raw });

        // ── Reflection sanity ─────────────────────────────────────────────────

        [Test]
        public void ReflectionSetup_FormatDuration_MethodFound()
        {
            Assert.IsNotNull(_formatDuration,
                "Private static method 'FormatDuration(float)' not found on " +
                "MatchHistoryRowController — has the method been renamed or removed?");
        }

        [Test]
        public void ReflectionSetup_FormatTimestamp_MethodFound()
        {
            Assert.IsNotNull(_formatTimestamp,
                "Private static method 'FormatTimestamp(string)' not found on " +
                "MatchHistoryRowController — has the method been renamed or removed?");
        }

        // ── Setup null-safety ─────────────────────────────────────────────────

        [Test]
        public void Setup_NullRecord_DoesNotThrow()
        {
            var go   = new GameObject("TestRow");
            var ctrl = go.AddComponent<MatchHistoryRowController>();
            Assert.DoesNotThrow(() => ctrl.Setup(null),
                "Setup(null) must return early without throwing.");
            Object.DestroyImmediate(go);
        }

        // ── FormatDuration ─────────────────────────────────────────────────────

        [Test]
        public void FormatDuration_Zero_Returns00Colon00()
        {
            Assert.AreEqual("00:00", Duration(0f));
        }

        [Test]
        public void FormatDuration_60Seconds_Returns01Colon00()
        {
            Assert.AreEqual("01:00", Duration(60f));
        }

        [Test]
        public void FormatDuration_90Seconds_Returns01Colon30()
        {
            Assert.AreEqual("01:30", Duration(90f));
        }

        [Test]
        public void FormatDuration_59Seconds_Returns00Colon59()
        {
            Assert.AreEqual("00:59", Duration(59f));
        }

        [Test]
        public void FormatDuration_3661Seconds_Returns61Colon01()
        {
            // Verifies that minutes are not clamped to 59.
            Assert.AreEqual("61:01", Duration(3661f));
        }

        [Test]
        public void FormatDuration_NegativeSeconds_ClampsToZero()
        {
            Assert.AreEqual("00:00", Duration(-5f),
                "Negative durations must be clamped to 00:00.");
        }

        // ── FormatTimestamp ────────────────────────────────────────────────────

        [Test]
        public void FormatTimestamp_Null_ReturnsDash()
        {
            Assert.AreEqual("--", Timestamp(null));
        }

        [Test]
        public void FormatTimestamp_Empty_ReturnsDash()
        {
            Assert.AreEqual("--", Timestamp(string.Empty));
        }

        [Test]
        public void FormatTimestamp_ValidISO8601_ContainsYear()
        {
            // Noon UTC on a mid-year date — the year 2026 is stable across all UTC offsets.
            string result = Timestamp("2026-06-15T12:00:00Z");
            Assert.IsNotNull(result);
            Assert.AreNotEqual("--", result,
                "A valid ISO-8601 timestamp should produce a formatted date, not '--'.");
            StringAssert.Contains("2026", result,
                "The formatted date must contain the source year.");
        }

        [Test]
        public void FormatTimestamp_InvalidString_ReturnsRawString()
        {
            const string raw = "not-a-date-string";
            Assert.AreEqual(raw, Timestamp(raw),
                "Unparseable timestamps must be returned unchanged as a fallback.");
        }
    }
}
