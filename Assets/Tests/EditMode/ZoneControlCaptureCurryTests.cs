using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCurryTests
    {
        private static ZoneControlCaptureCurrySO CreateSO(
            int argsNeeded   = 5,
            int removePerBot = 1,
            int bonusPerCurry = 2170)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCurrySO>();
            typeof(ZoneControlCaptureCurrySO)
                .GetField("_argsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, argsNeeded);
            typeof(ZoneControlCaptureCurrySO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureCurrySO)
                .GetField("_bonusPerCurry", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCurry);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCurryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCurryController>();
        }

        [Test]
        public void SO_FreshInstance_Args_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Args, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurryCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesArgs()
        {
            var so = CreateSO(argsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Args, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(argsNeeded: 3, bonusPerCurry: 2170);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2170));
            Assert.That(so.CurryCount,  Is.EqualTo(1));
            Assert.That(so.Args,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(argsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesArgs()
        {
            var so = CreateSO(argsNeeded: 5, removePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Args, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(argsNeeded: 5, removePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Args, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ArgProgress_Clamped()
        {
            var so = CreateSO(argsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ArgProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCurryComplete_FiresEvent()
        {
            var so    = CreateSO(argsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCurrySO)
                .GetField("_onCurryComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(argsNeeded: 2, bonusPerCurry: 2170);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Args,              Is.EqualTo(0));
            Assert.That(so.CurryCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCurries_Accumulate()
        {
            var so = CreateSO(argsNeeded: 2, bonusPerCurry: 2170);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CurryCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4340));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CurrySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CurrySO, Is.Null);
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
            typeof(ZoneControlCaptureCurryController)
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
