using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFurnaceTests
    {
        private static ZoneControlCaptureFurnaceSO CreateSO(
            int fuelNeeded    = 5,
            int dousePerBot   = 1,
            int bonusPerSmelt = 435)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFurnaceSO>();
            typeof(ZoneControlCaptureFurnaceSO)
                .GetField("_fuelNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fuelNeeded);
            typeof(ZoneControlCaptureFurnaceSO)
                .GetField("_dousePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dousePerBot);
            typeof(ZoneControlCaptureFurnaceSO)
                .GetField("_bonusPerSmelt", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSmelt);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFurnaceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFurnaceController>();
        }

        [Test]
        public void SO_FreshInstance_Fuel_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Fuel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SmeltCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SmeltCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFuel()
        {
            var so = CreateSO(fuelNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Fuel, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SmeltsAtThreshold()
        {
            var so    = CreateSO(fuelNeeded: 3, bonusPerSmelt: 435);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(435));
            Assert.That(so.SmeltCount, Is.EqualTo(1));
            Assert.That(so.Fuel,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFueling()
        {
            var so    = CreateSO(fuelNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DousesFuel()
        {
            var so = CreateSO(fuelNeeded: 5, dousePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fuel, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fuelNeeded: 5, dousePerBot: 4);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fuel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FuelProgress_Clamped()
        {
            var so = CreateSO(fuelNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FuelProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFurnaceSmelted_FiresEvent()
        {
            var so    = CreateSO(fuelNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFurnaceSO)
                .GetField("_onFurnaceSmelted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(fuelNeeded: 2, bonusPerSmelt: 435);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Fuel,              Is.EqualTo(0));
            Assert.That(so.SmeltCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSmelts_Accumulate()
        {
            var so = CreateSO(fuelNeeded: 2, bonusPerSmelt: 435);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SmeltCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(870));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FurnaceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FurnaceSO, Is.Null);
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
            typeof(ZoneControlCaptureFurnaceController)
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
