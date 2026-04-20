using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBellTests
    {
        private static ZoneControlCaptureBellSO CreateSO(
            int ringsNeeded  = 5,
            int mutePerBot   = 1,
            int bonusPerToll = 505)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBellSO>();
            typeof(ZoneControlCaptureBellSO)
                .GetField("_ringsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ringsNeeded);
            typeof(ZoneControlCaptureBellSO)
                .GetField("_mutePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, mutePerBot);
            typeof(ZoneControlCaptureBellSO)
                .GetField("_bonusPerToll", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerToll);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBellController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBellController>();
        }

        [Test]
        public void SO_FreshInstance_Rings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Rings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TollCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TollCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRings()
        {
            var so = CreateSO(ringsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Rings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TollsAtThreshold()
        {
            var so    = CreateSO(ringsNeeded: 3, bonusPerToll: 505);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(505));
            Assert.That(so.TollCount,  Is.EqualTo(1));
            Assert.That(so.Rings,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ringsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_MutesRings()
        {
            var so = CreateSO(ringsNeeded: 5, mutePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Rings, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ringsNeeded: 5, mutePerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Rings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RingProgress_Clamped()
        {
            var so = CreateSO(ringsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBellTolled_FiresEvent()
        {
            var so    = CreateSO(ringsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBellSO)
                .GetField("_onBellTolled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ringsNeeded: 2, bonusPerToll: 505);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Rings,             Is.EqualTo(0));
            Assert.That(so.TollCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTolls_Accumulate()
        {
            var so = CreateSO(ringsNeeded: 2, bonusPerToll: 505);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TollCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1010));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BellSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BellSO, Is.Null);
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
            typeof(ZoneControlCaptureBellController)
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
