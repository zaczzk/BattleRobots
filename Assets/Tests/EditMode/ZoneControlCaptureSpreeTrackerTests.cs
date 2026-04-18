using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T448: <see cref="ZoneControlCaptureSpreeTrackerSO"/> and
    /// <see cref="ZoneControlCaptureSpreeTrackerController"/>.
    ///
    /// ZoneControlCaptureSpreeTrackerTests (12):
    ///   SO_FreshInstance_CurrentStreak_Zero                              x1
    ///   SO_FreshInstance_IsSpreeActive_False                             x1
    ///   SO_RecordPlayerCapture_ConsecutiveWithinWindow_IncrementsStreak  x1
    ///   SO_RecordPlayerCapture_GapExceedsWindow_ResetsStreak             x1
    ///   SO_RecordPlayerCapture_AtThreshold_ActivatesSpree                x1
    ///   SO_RecordPlayerCapture_DuringSpree_ReturnsBonus                  x1
    ///   SO_RecordPlayerCapture_BelowThreshold_ReturnsZero                x1
    ///   SO_RecordPlayerCapture_FiresOnSpreeStarted_AtThreshold           x1
    ///   SO_BreakSpree_ResetsStreak_AndFiresOnSpreeEnded                  x1
    ///   SO_Reset_ClearsAll                                               x1
    ///   Controller_FreshInstance_SpreeTrackerSO_Null                     x1
    ///   Controller_Refresh_NullSO_HidesPanel                             x1
    /// </summary>
    public sealed class ZoneControlCaptureSpreeTrackerTests
    {
        private static ZoneControlCaptureSpreeTrackerSO CreateSO(
            int   threshold = 3,
            float window    = 8f,
            int   bonus     = 40)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpreeTrackerSO>();
            typeof(ZoneControlCaptureSpreeTrackerSO)
                .GetField("_spreeThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            typeof(ZoneControlCaptureSpreeTrackerSO)
                .GetField("_spreeWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, window);
            typeof(ZoneControlCaptureSpreeTrackerSO)
                .GetField("_bonusPerSpreeCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpreeTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpreeTrackerController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsSpreeActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsSpreeActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ConsecutiveWithinWindow_IncrementsStreak()
        {
            var so = CreateSO(threshold: 5, window: 10f);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(5f);
            Assert.That(so.CurrentStreak, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_GapExceedsWindow_ResetsStreak()
        {
            var so = CreateSO(threshold: 5, window: 3f);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(10f); // gap = 10 > 3
            Assert.That(so.CurrentStreak, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_ActivatesSpree()
        {
            var so = CreateSO(threshold: 3, window: 10f);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordPlayerCapture(2f); // 3rd → spree starts
            Assert.That(so.IsSpreeActive, Is.True);
            Assert.That(so.SpreeCount,    Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DuringSpree_ReturnsBonus()
        {
            var so = CreateSO(threshold: 2, window: 10f, bonus: 40);
            so.RecordPlayerCapture(0f);
            int bonus = so.RecordPlayerCapture(1f); // 2nd → spree + bonus
            Assert.That(bonus, Is.EqualTo(40));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(threshold: 5, window: 10f, bonus: 40);
            int bonus = so.RecordPlayerCapture(0f); // streak=1, not yet spree
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresOnSpreeStarted_AtThreshold()
        {
            var so      = CreateSO(threshold: 2, window: 10f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpreeTrackerSO)
                .GetField("_onSpreeStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture(0f);
            Assert.That(fired, Is.EqualTo(0));
            so.RecordPlayerCapture(1f); // hits threshold
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_BreakSpree_ResetsStreak_AndFiresOnSpreeEnded()
        {
            var so      = CreateSO(threshold: 2, window: 10f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpreeTrackerSO)
                .GetField("_onSpreeEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f); // activate spree
            so.BreakSpree();

            Assert.That(so.IsSpreeActive,  Is.False);
            Assert.That(so.CurrentStreak,  Is.EqualTo(0));
            Assert.That(fired,             Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(threshold: 2, window: 10f, bonus: 40);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.Reset();
            Assert.That(so.CurrentStreak,    Is.EqualTo(0));
            Assert.That(so.IsSpreeActive,    Is.False);
            Assert.That(so.SpreeCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpreeTrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpreeTrackerSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureSpreeTrackerController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
