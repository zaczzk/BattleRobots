using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCoproductTests
    {
        private static ZoneControlCaptureCoproductSO CreateSO(
            int objectsNeeded    = 5,
            int collapsePerBot   = 1,
            int bonusPerCoproduct = 2725)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCoproductSO>();
            typeof(ZoneControlCaptureCoproductSO)
                .GetField("_objectsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, objectsNeeded);
            typeof(ZoneControlCaptureCoproductSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureCoproductSO)
                .GetField("_bonusPerCoproduct", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCoproduct);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCoproductController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCoproductController>();
        }

        [Test]
        public void SO_FreshInstance_Objects_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Objects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CoproductCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CoproductCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesObjects()
        {
            var so = CreateSO(objectsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Objects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(objectsNeeded: 3, bonusPerCoproduct: 2725);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(2725));
            Assert.That(so.CoproductCount, Is.EqualTo(1));
            Assert.That(so.Objects,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(objectsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesObjects()
        {
            var so = CreateSO(objectsNeeded: 5, collapsePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Objects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(objectsNeeded: 5, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Objects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ObjectProgress_Clamped()
        {
            var so = CreateSO(objectsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ObjectProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCoproductInjected_FiresEvent()
        {
            var so    = CreateSO(objectsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCoproductSO)
                .GetField("_onCoproductInjected", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(objectsNeeded: 2, bonusPerCoproduct: 2725);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Objects,           Is.EqualTo(0));
            Assert.That(so.CoproductCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCoproducts_Accumulate()
        {
            var so = CreateSO(objectsNeeded: 2, bonusPerCoproduct: 2725);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CoproductCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5450));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CoproductSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CoproductSO, Is.Null);
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
            typeof(ZoneControlCaptureCoproductController)
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
