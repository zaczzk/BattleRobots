using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOperadTests
    {
        private static ZoneControlCaptureOperadSO CreateSO(
            int operationsNeeded = 6,
            int collapsePerBot   = 1,
            int bonusPerCompose  = 3610)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOperadSO>();
            typeof(ZoneControlCaptureOperadSO)
                .GetField("_operationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, operationsNeeded);
            typeof(ZoneControlCaptureOperadSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureOperadSO)
                .GetField("_bonusPerCompose", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompose);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOperadController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOperadController>();
        }

        [Test]
        public void SO_FreshInstance_Operations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Operations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComposeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComposeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesOperations()
        {
            var so = CreateSO(operationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Operations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(operationsNeeded: 3, bonusPerCompose: 3610);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3610));
            Assert.That(so.ComposeCount, Is.EqualTo(1));
            Assert.That(so.Operations,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(operationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CollapsesOperations()
        {
            var so = CreateSO(operationsNeeded: 6, collapsePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Operations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(operationsNeeded: 6, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Operations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OperationProgress_Clamped()
        {
            var so = CreateSO(operationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.OperationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnOperadComposed_FiresEvent()
        {
            var so    = CreateSO(operationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureOperadSO)
                .GetField("_onOperadComposed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(operationsNeeded: 2, bonusPerCompose: 3610);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Operations,        Is.EqualTo(0));
            Assert.That(so.ComposeCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompositions_Accumulate()
        {
            var so = CreateSO(operationsNeeded: 2, bonusPerCompose: 3610);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComposeCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7220));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OperadSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OperadSO, Is.Null);
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
            typeof(ZoneControlCaptureOperadController)
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
