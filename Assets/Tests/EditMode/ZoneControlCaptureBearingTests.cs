using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBearingTests
    {
        private static ZoneControlCaptureBearingSO CreateSO(
            int racesNeeded  = 7,
            int driftPerBot  = 2,
            int bonusPerSpin = 1270)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBearingSO>();
            typeof(ZoneControlCaptureBearingSO)
                .GetField("_racesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, racesNeeded);
            typeof(ZoneControlCaptureBearingSO)
                .GetField("_driftPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, driftPerBot);
            typeof(ZoneControlCaptureBearingSO)
                .GetField("_bonusPerSpin", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSpin);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBearingController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBearingController>();
        }

        [Test]
        public void SO_FreshInstance_Races_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Races, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SpinCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SpinCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRaces()
        {
            var so = CreateSO(racesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Races, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RacesAtThreshold()
        {
            var so    = CreateSO(racesNeeded: 3, bonusPerSpin: 1270);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(1270));
            Assert.That(so.SpinCount, Is.EqualTo(1));
            Assert.That(so.Races,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(racesNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesRaces()
        {
            var so = CreateSO(racesNeeded: 7, driftPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Races, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(racesNeeded: 7, driftPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Races, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RaceProgress_Clamped()
        {
            var so = CreateSO(racesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.RaceProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBearingSpun_FiresEvent()
        {
            var so    = CreateSO(racesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBearingSO)
                .GetField("_onBearingSpun", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(racesNeeded: 2, bonusPerSpin: 1270);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Races,             Is.EqualTo(0));
            Assert.That(so.SpinCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSpins_Accumulate()
        {
            var so = CreateSO(racesNeeded: 2, bonusPerSpin: 1270);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SpinCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2540));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BearingSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BearingSO, Is.Null);
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
            typeof(ZoneControlCaptureBearingController)
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
