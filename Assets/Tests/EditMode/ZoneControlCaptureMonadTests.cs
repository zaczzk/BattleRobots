using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMonadTests
    {
        private static ZoneControlCaptureMonadSO CreateSO(
            int operationsNeeded = 5,
            int abortPerBot      = 1,
            int bonusPerChain    = 2215)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMonadSO>();
            typeof(ZoneControlCaptureMonadSO)
                .GetField("_operationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, operationsNeeded);
            typeof(ZoneControlCaptureMonadSO)
                .GetField("_abortPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, abortPerBot);
            typeof(ZoneControlCaptureMonadSO)
                .GetField("_bonusPerChain", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerChain);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMonadController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMonadController>();
        }

        [Test]
        public void SO_FreshInstance_Operations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Operations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChainCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChainCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesOperations()
        {
            var so = CreateSO(operationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Operations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(operationsNeeded: 3, bonusPerChain: 2215);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2215));
            Assert.That(so.ChainCount,  Is.EqualTo(1));
            Assert.That(so.Operations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(operationsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesOperations()
        {
            var so = CreateSO(operationsNeeded: 5, abortPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Operations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(operationsNeeded: 5, abortPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Operations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OperationProgress_Clamped()
        {
            var so = CreateSO(operationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.OperationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMonadChained_FiresEvent()
        {
            var so    = CreateSO(operationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMonadSO)
                .GetField("_onMonadChained", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(operationsNeeded: 2, bonusPerChain: 2215);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Operations,        Is.EqualTo(0));
            Assert.That(so.ChainCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleChains_Accumulate()
        {
            var so = CreateSO(operationsNeeded: 2, bonusPerChain: 2215);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ChainCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4430));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MonadSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MonadSO, Is.Null);
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
            typeof(ZoneControlCaptureMonadController)
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
