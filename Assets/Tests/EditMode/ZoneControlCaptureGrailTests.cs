using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGrailTests
    {
        private static ZoneControlCaptureGrailSO CreateSO(
            int dropsNeeded = 6,
            int spillPerBot = 2,
            int bonusPerFill = 640)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGrailSO>();
            typeof(ZoneControlCaptureGrailSO)
                .GetField("_dropsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dropsNeeded);
            typeof(ZoneControlCaptureGrailSO)
                .GetField("_spillPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, spillPerBot);
            typeof(ZoneControlCaptureGrailSO)
                .GetField("_bonusPerFill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFill);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGrailController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGrailController>();
        }

        [Test]
        public void SO_FreshInstance_Drops_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Drops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FillCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FillCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDrops()
        {
            var so = CreateSO(dropsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Drops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsAtThreshold()
        {
            var so    = CreateSO(dropsNeeded: 3, bonusPerFill: 640);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(640));
            Assert.That(so.FillCount,   Is.EqualTo(1));
            Assert.That(so.Drops,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(dropsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SpillsDrops()
        {
            var so = CreateSO(dropsNeeded: 6, spillPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Drops, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(dropsNeeded: 6, spillPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Drops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DropProgress_Clamped()
        {
            var so = CreateSO(dropsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DropProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGrailFilled_FiresEvent()
        {
            var so    = CreateSO(dropsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGrailSO)
                .GetField("_onGrailFilled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(dropsNeeded: 2, bonusPerFill: 640);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Drops,             Is.EqualTo(0));
            Assert.That(so.FillCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFills_Accumulate()
        {
            var so = CreateSO(dropsNeeded: 2, bonusPerFill: 640);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FillCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1280));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GrailSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GrailSO, Is.Null);
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
            typeof(ZoneControlCaptureGrailController)
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
