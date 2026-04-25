using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGrothendieckRingTests
    {
        private static ZoneControlCaptureGrothendieckRingSO CreateSO(
            int virtualObjectsNeeded = 6,
            int exactTrianglesPerBot = 2,
            int bonusPerAddition     = 4150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGrothendieckRingSO>();
            typeof(ZoneControlCaptureGrothendieckRingSO)
                .GetField("_virtualObjectsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, virtualObjectsNeeded);
            typeof(ZoneControlCaptureGrothendieckRingSO)
                .GetField("_exactTrianglesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, exactTrianglesPerBot);
            typeof(ZoneControlCaptureGrothendieckRingSO)
                .GetField("_bonusPerAddition", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAddition);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGrothendieckRingController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGrothendieckRingController>();
        }

        [Test]
        public void SO_FreshInstance_VirtualObjects_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VirtualObjects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AdditionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AdditionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesVirtualObjects()
        {
            var so = CreateSO(virtualObjectsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.VirtualObjects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(virtualObjectsNeeded: 3, bonusPerAddition: 4150);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(4150));
            Assert.That(so.AdditionCount, Is.EqualTo(1));
            Assert.That(so.VirtualObjects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(virtualObjectsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesExactTriangles()
        {
            var so = CreateSO(virtualObjectsNeeded: 6, exactTrianglesPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VirtualObjects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(virtualObjectsNeeded: 6, exactTrianglesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VirtualObjects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VirtualObjectProgress_Clamped()
        {
            var so = CreateSO(virtualObjectsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.VirtualObjectProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGrothendieckRingAdded_FiresEvent()
        {
            var so    = CreateSO(virtualObjectsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGrothendieckRingSO)
                .GetField("_onGrothendieckRingAdded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(virtualObjectsNeeded: 2, bonusPerAddition: 4150);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.VirtualObjects,    Is.EqualTo(0));
            Assert.That(so.AdditionCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAdditions_Accumulate()
        {
            var so = CreateSO(virtualObjectsNeeded: 2, bonusPerAddition: 4150);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AdditionCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GrothendieckRingSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GrothendieckRingSO, Is.Null);
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
            typeof(ZoneControlCaptureGrothendieckRingController)
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
