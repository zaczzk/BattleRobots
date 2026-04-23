using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureExtensionTests
    {
        private static ZoneControlCaptureExtensionSO CreateSO(
            int elementsNeeded   = 6,
            int splitPerBot      = 2,
            int bonusPerExtension = 2680)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureExtensionSO>();
            typeof(ZoneControlCaptureExtensionSO)
                .GetField("_elementsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, elementsNeeded);
            typeof(ZoneControlCaptureExtensionSO)
                .GetField("_splitPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, splitPerBot);
            typeof(ZoneControlCaptureExtensionSO)
                .GetField("_bonusPerExtension", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExtension);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureExtensionController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureExtensionController>();
        }

        [Test]
        public void SO_FreshInstance_Elements_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Elements, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesElements()
        {
            var so = CreateSO(elementsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Elements, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(elementsNeeded: 3, bonusPerExtension: 2680);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(2680));
            Assert.That(so.ExtensionCount,   Is.EqualTo(1));
            Assert.That(so.Elements,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(elementsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesElements()
        {
            var so = CreateSO(elementsNeeded: 6, splitPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Elements, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(elementsNeeded: 6, splitPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Elements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ElementProgress_Clamped()
        {
            var so = CreateSO(elementsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ElementProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnExtensionApplied_FiresEvent()
        {
            var so    = CreateSO(elementsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureExtensionSO)
                .GetField("_onExtensionApplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(elementsNeeded: 2, bonusPerExtension: 2680);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Elements,          Is.EqualTo(0));
            Assert.That(so.ExtensionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExtensions_Accumulate()
        {
            var so = CreateSO(elementsNeeded: 2, bonusPerExtension: 2680);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExtensionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5360));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ExtensionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ExtensionSO, Is.Null);
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
            typeof(ZoneControlCaptureExtensionController)
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
