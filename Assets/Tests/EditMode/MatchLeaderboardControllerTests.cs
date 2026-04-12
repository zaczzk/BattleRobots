using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchLeaderboardController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all refs null      → DoesNotThrow.
    ///   • OnEnable / OnDisable with null channel        → DoesNotThrow.
    ///   • Refresh with null _leaderboard               → DoesNotThrow.
    ///   • Refresh with null _listContainer             → DoesNotThrow.
    ///   • OnDisable unregisters delegate from _onLeaderboardUpdated
    ///     (external-counter pattern verifies callback is removed).
    ///   • FormatDuration helper formatting (via reflection).
    ///
    /// All tests run headless (no Canvas required).
    /// The inactive-GO pattern prevents OnEnable from firing before fields are injected.
    /// </summary>
    public class MatchLeaderboardControllerTests
    {
        private GameObject                  _go;
        private MatchLeaderboardController  _ctrl;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static string CallFormatDuration(float seconds)
        {
            MethodInfo mi = typeof(MatchLeaderboardController)
                .GetMethod("FormatDuration",
                    BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, "FormatDuration method not found.");
            return (string)mi.Invoke(null, new object[] { seconds });
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("MatchLeaderboardController");
            _go.SetActive(false); // inactive until fields are wired
            _ctrl = _go.AddComponent<MatchLeaderboardController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private void Activate() => _go.SetActive(true);

        // ── OnEnable / OnDisable null-guard paths ─────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var board = ScriptableObject.CreateInstance<MatchLeaderboardSO>();
            SetField(_ctrl, "_leaderboard", board);
            // _onLeaderboardUpdated stays null
            Assert.DoesNotThrow(() => Activate());
            Object.DestroyImmediate(board);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var board = ScriptableObject.CreateInstance<MatchLeaderboardSO>();
            SetField(_ctrl, "_leaderboard", board);
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
            Object.DestroyImmediate(board);
        }

        // ── Refresh null-guard paths ──────────────────────────────────────────

        [Test]
        public void Refresh_NullLeaderboard_DoesNotThrow()
        {
            // No _leaderboard assigned — Refresh must silently return.
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void Refresh_NullListContainer_DoesNotThrow()
        {
            var board = ScriptableObject.CreateInstance<MatchLeaderboardSO>();
            SetField(_ctrl, "_leaderboard", board);
            // _listContainer stays null
            Assert.DoesNotThrow(() => Activate());
            Object.DestroyImmediate(board);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnLeaderboardUpdated()
        {
            // External-counter pattern: after OnDisable, raising the channel must NOT
            // trigger the controller's Refresh delegate.
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onLeaderboardUpdated", channel);

            Activate();           // OnEnable — registers _refreshDelegate + calls Refresh once.
            _go.SetActive(false); // OnDisable — must unregister delegate.

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);
            channel.Raise(); // only the external counter should fire

            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, callCount,
                "Only the external counter should fire after the controller unregisters.");
        }

        // ── FormatDuration helper ─────────────────────────────────────────────

        [Test]
        public void FormatDuration_Zero_ReturnsZeroSeconds()
        {
            Assert.AreEqual("0s", CallFormatDuration(0f));
        }

        [Test]
        public void FormatDuration_BelowOneMinute_ReturnsSecondsOnly()
        {
            Assert.AreEqual("30s", CallFormatDuration(30f));
        }

        [Test]
        public void FormatDuration_ExactlyOneMinute_ReturnsMinsAndZeroSeconds()
        {
            Assert.AreEqual("1m 0s", CallFormatDuration(60f));
        }

        [Test]
        public void FormatDuration_MinutesAndSeconds_CorrectFormat()
        {
            Assert.AreEqual("1m 30s", CallFormatDuration(90f));
        }

        [Test]
        public void FormatDuration_NegativeInput_ClampedToZero()
        {
            Assert.AreEqual("0s", CallFormatDuration(-5f));
        }
    }
}
