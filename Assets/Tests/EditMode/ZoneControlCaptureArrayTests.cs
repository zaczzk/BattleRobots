using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureArrayTests
    {
        private static ZoneControlCaptureArraySO CreateSO(
            int elementsNeeded = 7,
            int clearPerBot    = 2,
            int bonusPerFill   = 2035)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureArraySO>();
            typeof(ZoneControlCaptureArraySO)
                .GetField("_elementsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, elementsNeeded);
            typeof(ZoneControlCaptureArraySO)
                .GetField("_clearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, clearPerBot);
            typeof(ZoneControlCaptureArraySO)
                .GetField("_bonusPerFill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFill);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureArrayController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureArrayController>();
        }

        [Test]
        public void SO_FreshInstance_Elements_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Elements, Is.EqualTo(0));
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
            var so    = CreateSO(elementsNeeded: 3, bonusPerFill: 2035);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(2035));
            Assert.That(so.FillCount, Is.EqualTo(1));
            Assert.That(so.Elements,  Is.EqualTo(0));
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
        public void SO_RecordBotCapture_ClearsElements()
        {
            var so = CreateSO(elementsNeeded: 7, clearPerBot: 2);
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
            var so = CreateSO(elementsNeeded: 7, clearPerBot: 10);
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
        public void SO_OnArrayFilled_FiresEvent()
        {
            var so    = CreateSO(elementsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureArraySO)
                .GetField("_onArrayFilled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(elementsNeeded: 2, bonusPerFill: 2035);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Elements,          Is.EqualTo(0));
            Assert.That(so.FillCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFills_Accumulate()
        {
            var so = CreateSO(elementsNeeded: 2, bonusPerFill: 2035);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FillCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4070));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ArraySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ArraySO, Is.Null);
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
            typeof(ZoneControlCaptureArrayController)
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
