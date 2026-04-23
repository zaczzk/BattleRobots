using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFreeObjectTests
    {
        private static ZoneControlCaptureFreeObjectSO CreateSO(
            int generatorsNeeded   = 5,
            int releasePerBot      = 1,
            int bonusPerFreeObject = 2965)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFreeObjectSO>();
            typeof(ZoneControlCaptureFreeObjectSO)
                .GetField("_generatorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, generatorsNeeded);
            typeof(ZoneControlCaptureFreeObjectSO)
                .GetField("_releasePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, releasePerBot);
            typeof(ZoneControlCaptureFreeObjectSO)
                .GetField("_bonusPerFreeObject", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFreeObject);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFreeObjectController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFreeObjectController>();
        }

        [Test]
        public void SO_FreshInstance_Generators_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Generators, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FreeObjectCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FreeObjectCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGenerators()
        {
            var so = CreateSO(generatorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Generators, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(generatorsNeeded: 3, bonusPerFreeObject: 2965);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(2965));
            Assert.That(so.FreeObjectCount,  Is.EqualTo(1));
            Assert.That(so.Generators,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(generatorsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesGenerators()
        {
            var so = CreateSO(generatorsNeeded: 5, releasePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Generators, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(generatorsNeeded: 5, releasePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Generators, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GeneratorProgress_Clamped()
        {
            var so = CreateSO(generatorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.GeneratorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFreeObjectGenerated_FiresEvent()
        {
            var so    = CreateSO(generatorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFreeObjectSO)
                .GetField("_onFreeObjectGenerated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(generatorsNeeded: 2, bonusPerFreeObject: 2965);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Generators,        Is.EqualTo(0));
            Assert.That(so.FreeObjectCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFreeObjects_Accumulate()
        {
            var so = CreateSO(generatorsNeeded: 2, bonusPerFreeObject: 2965);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FreeObjectCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5930));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FreeObjectSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FreeObjectSO, Is.Null);
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
            typeof(ZoneControlCaptureFreeObjectController)
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
