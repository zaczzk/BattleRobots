using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFunctorTests
    {
        private static ZoneControlCaptureFunctorSO CreateSO(
            int elementsNeeded = 7,
            int removePerBot   = 2,
            int bonusPerLift   = 2230)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFunctorSO>();
            typeof(ZoneControlCaptureFunctorSO)
                .GetField("_elementsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, elementsNeeded);
            typeof(ZoneControlCaptureFunctorSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureFunctorSO)
                .GetField("_bonusPerLift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLift);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFunctorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFunctorController>();
        }

        [Test]
        public void SO_FreshInstance_Elements_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Elements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LiftCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LiftCount, Is.EqualTo(0));
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
            var so    = CreateSO(elementsNeeded: 3, bonusPerLift: 2230);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2230));
            Assert.That(so.LiftCount,   Is.EqualTo(1));
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
            var so = CreateSO(elementsNeeded: 7, removePerBot: 2);
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
            var so = CreateSO(elementsNeeded: 7, removePerBot: 10);
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
        public void SO_OnFunctorLifted_FiresEvent()
        {
            var so    = CreateSO(elementsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFunctorSO)
                .GetField("_onFunctorLifted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(elementsNeeded: 2, bonusPerLift: 2230);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Elements,          Is.EqualTo(0));
            Assert.That(so.LiftCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLifts_Accumulate()
        {
            var so = CreateSO(elementsNeeded: 2, bonusPerLift: 2230);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LiftCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4460));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FunctorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FunctorSO, Is.Null);
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
            typeof(ZoneControlCaptureFunctorController)
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
