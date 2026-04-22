using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureClosureTests
    {
        private static ZoneControlCaptureClosureSO CreateSO(
            int bindingsNeeded = 6,
            int unbindPerBot   = 2,
            int bonusPerClosure = 2200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureClosureSO>();
            typeof(ZoneControlCaptureClosureSO)
                .GetField("_bindingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bindingsNeeded);
            typeof(ZoneControlCaptureClosureSO)
                .GetField("_unbindPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unbindPerBot);
            typeof(ZoneControlCaptureClosureSO)
                .GetField("_bonusPerClosure", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClosure);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureClosureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureClosureController>();
        }

        [Test]
        public void SO_FreshInstance_Bindings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bindings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClosureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClosureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBindings()
        {
            var so = CreateSO(bindingsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Bindings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bindingsNeeded: 3, bonusPerClosure: 2200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(2200));
            Assert.That(so.ClosureCount,   Is.EqualTo(1));
            Assert.That(so.Bindings,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bindingsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBindings()
        {
            var so = CreateSO(bindingsNeeded: 6, unbindPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bindings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bindingsNeeded: 6, unbindPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bindings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BindingProgress_Clamped()
        {
            var so = CreateSO(bindingsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.BindingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnClosureSealed_FiresEvent()
        {
            var so    = CreateSO(bindingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureClosureSO)
                .GetField("_onClosureSealed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bindingsNeeded: 2, bonusPerClosure: 2200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bindings,          Is.EqualTo(0));
            Assert.That(so.ClosureCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClosures_Accumulate()
        {
            var so = CreateSO(bindingsNeeded: 2, bonusPerClosure: 2200);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClosureCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ClosureSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ClosureSO, Is.Null);
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
            typeof(ZoneControlCaptureClosureController)
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
