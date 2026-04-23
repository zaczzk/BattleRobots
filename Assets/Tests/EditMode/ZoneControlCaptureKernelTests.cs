using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureKernelTests
    {
        private static ZoneControlCaptureKernelSO CreateSO(
            int elementsNeeded = 7,
            int dissolvePerBot = 2,
            int bonusPerKernel = 2815)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureKernelSO>();
            typeof(ZoneControlCaptureKernelSO)
                .GetField("_elementsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, elementsNeeded);
            typeof(ZoneControlCaptureKernelSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureKernelSO)
                .GetField("_bonusPerKernel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerKernel);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureKernelController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureKernelController>();
        }

        [Test]
        public void SO_FreshInstance_Elements_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Elements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_KernelCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.KernelCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesElements()
        {
            var so = CreateSO(elementsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Elements, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(elementsNeeded: 3, bonusPerKernel: 2815);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2815));
            Assert.That(so.KernelCount, Is.EqualTo(1));
            Assert.That(so.Elements,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(elementsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesElements()
        {
            var so = CreateSO(elementsNeeded: 7, dissolvePerBot: 2);
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
            var so = CreateSO(elementsNeeded: 7, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Elements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ElementProgress_Clamped()
        {
            var so = CreateSO(elementsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ElementProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnKernelComputed_FiresEvent()
        {
            var so    = CreateSO(elementsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureKernelSO)
                .GetField("_onKernelComputed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(elementsNeeded: 2, bonusPerKernel: 2815);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Elements,          Is.EqualTo(0));
            Assert.That(so.KernelCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleKernels_Accumulate()
        {
            var so = CreateSO(elementsNeeded: 2, bonusPerKernel: 2815);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.KernelCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5630));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_KernelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.KernelSO, Is.Null);
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
            typeof(ZoneControlCaptureKernelController)
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
