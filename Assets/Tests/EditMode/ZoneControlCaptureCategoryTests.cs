using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCategoryTests
    {
        private static ZoneControlCaptureCategorySO CreateSO(
            int objectsNeeded    = 5,
            int disconnectPerBot = 1,
            int bonusPerMorphism = 2335)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCategorySO>();
            typeof(ZoneControlCaptureCategorySO)
                .GetField("_objectsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, objectsNeeded);
            typeof(ZoneControlCaptureCategorySO)
                .GetField("_disconnectPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, disconnectPerBot);
            typeof(ZoneControlCaptureCategorySO)
                .GetField("_bonusPerMorphism", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerMorphism);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCategoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCategoryController>();
        }

        [Test]
        public void SO_FreshInstance_Objects_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Objects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MorphismCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MorphismCount, Is.EqualTo(0));
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
            var so    = CreateSO(objectsNeeded: 3, bonusPerMorphism: 2335);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(2335));
            Assert.That(so.MorphismCount, Is.EqualTo(1));
            Assert.That(so.Objects,       Is.EqualTo(0));
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
            var so = CreateSO(objectsNeeded: 5, disconnectPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Objects, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(objectsNeeded: 5, disconnectPerBot: 10);
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
        public void SO_OnMorphismFormed_FiresEvent()
        {
            var so    = CreateSO(objectsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCategorySO)
                .GetField("_onMorphismFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(objectsNeeded: 2, bonusPerMorphism: 2335);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Objects,           Is.EqualTo(0));
            Assert.That(so.MorphismCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMorphisms_Accumulate()
        {
            var so = CreateSO(objectsNeeded: 2, bonusPerMorphism: 2335);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.MorphismCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4670));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CategorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CategorySO, Is.Null);
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
            typeof(ZoneControlCaptureCategoryController)
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
