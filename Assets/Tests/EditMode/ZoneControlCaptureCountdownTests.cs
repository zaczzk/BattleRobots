using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T461: <see cref="ZoneControlCaptureCountdownSO"/> and
    /// <see cref="ZoneControlCaptureCountdownController"/>.
    ///
    /// ZoneControlCaptureCountdownTests (12):
    ///   SO_FreshInstance_IsActive_False                                            x1
    ///   SO_FreshInstance_CaptureCount_Zero                                         x1
    ///   SO_StartCountdown_SetsActive                                               x1
    ///   SO_RecordCapture_NotActive_NoOp                                            x1
    ///   SO_RecordCapture_IncrementsCount                                            x1
    ///   SO_RecordCapture_ReachesTarget_ResolvesSuccess                             x1
    ///   SO_Tick_BeforeTimeout_NotResolved                                          x1
    ///   SO_Tick_AfterTimeout_ResolvesFail                                          x1
    ///   SO_RecordCapture_AfterResolved_NoOp                                        x1
    ///   SO_Reset_ClearsAll                                                         x1
    ///   Controller_FreshInstance_CountdownSO_Null                                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                                       x1
    /// </summary>
    public sealed class ZoneControlCaptureCountdownTests
    {
        private static ZoneControlCaptureCountdownSO CreateSO(int target = 8, float countdown = 60f, int bonus = 400)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCountdownSO>();
            typeof(ZoneControlCaptureCountdownSO)
                .GetField("_captureTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, target);
            typeof(ZoneControlCaptureCountdownSO)
                .GetField("_countdownSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, countdown);
            typeof(ZoneControlCaptureCountdownSO)
                .GetField("_bonusOnSuccess", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCountdownController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCountdownController>();
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCountdown_SetsActive()
        {
            var so = CreateSO();
            so.StartCountdown();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_NotActive_NoOp()
        {
            var so = CreateSO(target: 5);
            so.RecordCapture();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCount()
        {
            var so = CreateSO(target: 5);
            so.StartCountdown();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.CaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ReachesTarget_ResolvesSuccess()
        {
            var so = CreateSO(target: 3);
            so.StartCountdown();
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.IsResolved, Is.True);
            Assert.That(so.Succeeded,  Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BeforeTimeout_NotResolved()
        {
            var so = CreateSO(target: 10, countdown: 30f);
            so.StartCountdown();
            so.Tick(10f);
            Assert.That(so.IsResolved, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AfterTimeout_ResolvesFail()
        {
            var so = CreateSO(target: 10, countdown: 30f);
            so.StartCountdown();
            so.Tick(31f);
            Assert.That(so.IsResolved, Is.True);
            Assert.That(so.Succeeded,  Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AfterResolved_NoOp()
        {
            var so = CreateSO(target: 2, countdown: 30f);
            so.StartCountdown();
            so.RecordCapture();
            so.RecordCapture();
            int countAfterResolve = so.CaptureCount;
            so.RecordCapture();
            Assert.That(so.CaptureCount, Is.EqualTo(countAfterResolve));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(target: 5, countdown: 30f);
            so.StartCountdown();
            so.RecordCapture();
            so.Tick(5f);
            so.Reset();
            Assert.That(so.IsActive,      Is.False);
            Assert.That(so.IsResolved,    Is.False);
            Assert.That(so.CaptureCount,  Is.EqualTo(0));
            Assert.That(so.ElapsedTime,   Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CountdownSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CountdownSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureCountdownController)
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
