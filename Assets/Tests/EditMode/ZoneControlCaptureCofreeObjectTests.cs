using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCofreeObjectTests
    {
        private static ZoneControlCaptureCofreeObjectSO CreateSO(
            int cofactorsNeeded      = 6,
            int dissolvePerBot       = 2,
            int bonusPerCofreeObject = 2980)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCofreeObjectSO>();
            typeof(ZoneControlCaptureCofreeObjectSO)
                .GetField("_cofactorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cofactorsNeeded);
            typeof(ZoneControlCaptureCofreeObjectSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureCofreeObjectSO)
                .GetField("_bonusPerCofreeObject", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCofreeObject);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCofreeObjectController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCofreeObjectController>();
        }

        [Test]
        public void SO_FreshInstance_Cofactors_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cofactors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CofreeObjectCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CofreeObjectCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCofactors()
        {
            var so = CreateSO(cofactorsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Cofactors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cofactorsNeeded: 3, bonusPerCofreeObject: 2980);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(2980));
            Assert.That(so.CofreeObjectCount,   Is.EqualTo(1));
            Assert.That(so.Cofactors,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cofactorsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCofactors()
        {
            var so = CreateSO(cofactorsNeeded: 6, dissolvePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cofactors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cofactorsNeeded: 6, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cofactors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CofactorProgress_Clamped()
        {
            var so = CreateSO(cofactorsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CofactorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCofreeObjectCofreed_FiresEvent()
        {
            var so    = CreateSO(cofactorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCofreeObjectSO)
                .GetField("_onCofreeObjectCofreed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cofactorsNeeded: 2, bonusPerCofreeObject: 2980);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cofactors,          Is.EqualTo(0));
            Assert.That(so.CofreeObjectCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCofreeObjects_Accumulate()
        {
            var so = CreateSO(cofactorsNeeded: 2, bonusPerCofreeObject: 2980);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CofreeObjectCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(5960));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CofreeObjectSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CofreeObjectSO, Is.Null);
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
            typeof(ZoneControlCaptureCofreeObjectController)
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
