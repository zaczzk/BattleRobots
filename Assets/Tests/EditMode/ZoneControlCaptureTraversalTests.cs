using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTraversalTests
    {
        private static ZoneControlCaptureTraversalSO CreateSO(
            int elementsNeeded   = 6,
            int skipPerBot       = 2,
            int bonusPerTraverse = 2380)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTraversalSO>();
            typeof(ZoneControlCaptureTraversalSO)
                .GetField("_elementsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, elementsNeeded);
            typeof(ZoneControlCaptureTraversalSO)
                .GetField("_skipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, skipPerBot);
            typeof(ZoneControlCaptureTraversalSO)
                .GetField("_bonusPerTraverse", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTraverse);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTraversalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTraversalController>();
        }

        [Test]
        public void SO_FreshInstance_Elements_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Elements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TraverseCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TraverseCount, Is.EqualTo(0));
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
            var so    = CreateSO(elementsNeeded: 3, bonusPerTraverse: 2380);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(2380));
            Assert.That(so.TraverseCount,  Is.EqualTo(1));
            Assert.That(so.Elements,       Is.EqualTo(0));
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
            var so = CreateSO(elementsNeeded: 6, skipPerBot: 2);
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
            var so = CreateSO(elementsNeeded: 6, skipPerBot: 10);
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
        public void SO_OnTraversalComplete_FiresEvent()
        {
            var so    = CreateSO(elementsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTraversalSO)
                .GetField("_onTraversalComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(elementsNeeded: 2, bonusPerTraverse: 2380);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Elements,          Is.EqualTo(0));
            Assert.That(so.TraverseCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTraversals_Accumulate()
        {
            var so = CreateSO(elementsNeeded: 2, bonusPerTraverse: 2380);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TraverseCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4760));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TraversalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TraversalSO, Is.Null);
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
            typeof(ZoneControlCaptureTraversalController)
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
