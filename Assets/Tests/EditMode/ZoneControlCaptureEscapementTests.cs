using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEscapementTests
    {
        private static ZoneControlCaptureEscapementSO CreateSO(
            int ticksNeeded    = 7,
            int slipPerBot     = 2,
            int bonusPerRelease = 1390)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEscapementSO>();
            typeof(ZoneControlCaptureEscapementSO)
                .GetField("_ticksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ticksNeeded);
            typeof(ZoneControlCaptureEscapementSO)
                .GetField("_slipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slipPerBot);
            typeof(ZoneControlCaptureEscapementSO)
                .GetField("_bonusPerRelease", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRelease);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEscapementController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEscapementController>();
        }

        [Test]
        public void SO_FreshInstance_Ticks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ticks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ReleaseCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ReleaseCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTicks()
        {
            var so = CreateSO(ticksNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Ticks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TicksAtThreshold()
        {
            var so    = CreateSO(ticksNeeded: 3, bonusPerRelease: 1390);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1390));
            Assert.That(so.ReleaseCount, Is.EqualTo(1));
            Assert.That(so.Ticks,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ticksNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesTicks()
        {
            var so = CreateSO(ticksNeeded: 7, slipPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ticks, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ticksNeeded: 7, slipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ticks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TickProgress_Clamped()
        {
            var so = CreateSO(ticksNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.TickProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEscapementReleased_FiresEvent()
        {
            var so    = CreateSO(ticksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEscapementSO)
                .GetField("_onEscapementReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(ticksNeeded: 2, bonusPerRelease: 1390);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ticks,             Is.EqualTo(0));
            Assert.That(so.ReleaseCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleReleases_Accumulate()
        {
            var so = CreateSO(ticksNeeded: 2, bonusPerRelease: 1390);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ReleaseCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2780));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EscapementSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EscapementSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureEscapementController)
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
