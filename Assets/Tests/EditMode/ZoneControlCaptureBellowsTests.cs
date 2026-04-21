using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBellowsTests
    {
        private static ZoneControlCaptureBellowsSO CreateSO(
            int pumpsNeeded   = 6,
            int releasePerBot = 2,
            int bonusPerBlast = 1000)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBellowsSO>();
            typeof(ZoneControlCaptureBellowsSO)
                .GetField("_pumpsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pumpsNeeded);
            typeof(ZoneControlCaptureBellowsSO)
                .GetField("_releasePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, releasePerBot);
            typeof(ZoneControlCaptureBellowsSO)
                .GetField("_bonusPerBlast", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBlast);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBellowsController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBellowsController>();
        }

        [Test]
        public void SO_FreshInstance_Pumps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Pumps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BlastCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BlastCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPumps()
        {
            var so = CreateSO(pumpsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Pumps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PumpsAtThreshold()
        {
            var so    = CreateSO(pumpsNeeded: 3, bonusPerBlast: 1000);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1000));
            Assert.That(so.BlastCount,  Is.EqualTo(1));
            Assert.That(so.Pumps,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(pumpsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPumps()
        {
            var so = CreateSO(pumpsNeeded: 6, releasePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pumps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(pumpsNeeded: 6, releasePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pumps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PumpProgress_Clamped()
        {
            var so = CreateSO(pumpsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PumpProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBellowsBlasted_FiresEvent()
        {
            var so    = CreateSO(pumpsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBellowsSO)
                .GetField("_onBellowsBlasted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(pumpsNeeded: 2, bonusPerBlast: 1000);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Pumps,             Is.EqualTo(0));
            Assert.That(so.BlastCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBlasts_Accumulate()
        {
            var so = CreateSO(pumpsNeeded: 2, bonusPerBlast: 1000);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BlastCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2000));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BellowsSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BellowsSO, Is.Null);
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
            typeof(ZoneControlCaptureBellowsController)
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
