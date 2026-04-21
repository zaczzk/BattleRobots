using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCrucibleTests
    {
        private static ZoneControlCaptureCrucibleSO CreateSO(
            int oreNeeded     = 7,
            int removePerBot  = 2,
            int bonusPerAlloy = 970)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCrucibleSO>();
            typeof(ZoneControlCaptureCrucibleSO)
                .GetField("_oreNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, oreNeeded);
            typeof(ZoneControlCaptureCrucibleSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureCrucibleSO)
                .GetField("_bonusPerAlloy", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAlloy);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCrucibleController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCrucibleController>();
        }

        [Test]
        public void SO_FreshInstance_Ore_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ore, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AlloyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AlloyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesOre()
        {
            var so = CreateSO(oreNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Ore, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OreAtThreshold()
        {
            var so    = CreateSO(oreNeeded: 3, bonusPerAlloy: 970);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(970));
            Assert.That(so.AlloyCount,  Is.EqualTo(1));
            Assert.That(so.Ore,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(oreNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesOre()
        {
            var so = CreateSO(oreNeeded: 7, removePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ore, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(oreNeeded: 7, removePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ore, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OreProgress_Clamped()
        {
            var so = CreateSO(oreNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.OreProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCrucibleAlloyed_FiresEvent()
        {
            var so    = CreateSO(oreNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCrucibleSO)
                .GetField("_onCrucibleAlloyed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(oreNeeded: 2, bonusPerAlloy: 970);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ore,               Is.EqualTo(0));
            Assert.That(so.AlloyCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAlloys_Accumulate()
        {
            var so = CreateSO(oreNeeded: 2, bonusPerAlloy: 970);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AlloyCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1940));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CrucibleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CrucibleSO, Is.Null);
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
            typeof(ZoneControlCaptureCrucibleController)
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
