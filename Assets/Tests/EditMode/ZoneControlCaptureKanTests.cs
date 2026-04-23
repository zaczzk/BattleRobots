using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureKanTests
    {
        private static ZoneControlCaptureKanSO CreateSO(
            int extensionsNeeded  = 7,
            int collapsePerBot    = 2,
            int bonusPerExtension = 2470)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureKanSO>();
            typeof(ZoneControlCaptureKanSO)
                .GetField("_extensionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, extensionsNeeded);
            typeof(ZoneControlCaptureKanSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureKanSO)
                .GetField("_bonusPerExtension", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExtension);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureKanController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureKanController>();
        }

        [Test]
        public void SO_FreshInstance_Extensions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Extensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExtensionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExtensionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesExtensions()
        {
            var so = CreateSO(extensionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Extensions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(extensionsNeeded: 3, bonusPerExtension: 2470);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(2470));
            Assert.That(so.ExtensionCount,    Is.EqualTo(1));
            Assert.That(so.Extensions,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(extensionsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesExtensions()
        {
            var so = CreateSO(extensionsNeeded: 7, collapsePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Extensions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(extensionsNeeded: 7, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Extensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ExtensionProgress_Clamped()
        {
            var so = CreateSO(extensionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ExtensionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnKanExtended_FiresEvent()
        {
            var so    = CreateSO(extensionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureKanSO)
                .GetField("_onKanExtended", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(extensionsNeeded: 2, bonusPerExtension: 2470);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Extensions,        Is.EqualTo(0));
            Assert.That(so.ExtensionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExtensions_Accumulate()
        {
            var so = CreateSO(extensionsNeeded: 2, bonusPerExtension: 2470);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExtensionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4940));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_KanSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.KanSO, Is.Null);
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
            typeof(ZoneControlCaptureKanController)
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
