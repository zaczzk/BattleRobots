using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGovernorTests
    {
        private static ZoneControlCaptureGovernorSO CreateSO(
            int flyweightsNeeded  = 5,
            int lossPerBot        = 1,
            int bonusPerRegulation = 1375)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGovernorSO>();
            typeof(ZoneControlCaptureGovernorSO)
                .GetField("_flyweightsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, flyweightsNeeded);
            typeof(ZoneControlCaptureGovernorSO)
                .GetField("_lossPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lossPerBot);
            typeof(ZoneControlCaptureGovernorSO)
                .GetField("_bonusPerRegulation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRegulation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGovernorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGovernorController>();
        }

        [Test]
        public void SO_FreshInstance_Flyweights_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Flyweights, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RegulationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RegulationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFlyweights()
        {
            var so = CreateSO(flyweightsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Flyweights, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FlyweightsAtThreshold()
        {
            var so    = CreateSO(flyweightsNeeded: 3, bonusPerRegulation: 1375);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(1375));
            Assert.That(so.RegulationCount,  Is.EqualTo(1));
            Assert.That(so.Flyweights,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(flyweightsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFlyweights()
        {
            var so = CreateSO(flyweightsNeeded: 5, lossPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Flyweights, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(flyweightsNeeded: 5, lossPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Flyweights, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FlyweightProgress_Clamped()
        {
            var so = CreateSO(flyweightsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FlyweightProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGovernorRegulated_FiresEvent()
        {
            var so    = CreateSO(flyweightsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGovernorSO)
                .GetField("_onGovernorRegulated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(flyweightsNeeded: 2, bonusPerRegulation: 1375);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Flyweights,        Is.EqualTo(0));
            Assert.That(so.RegulationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRegulations_Accumulate()
        {
            var so = CreateSO(flyweightsNeeded: 2, bonusPerRegulation: 1375);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RegulationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2750));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GovernorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GovernorSO, Is.Null);
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
            typeof(ZoneControlCaptureGovernorController)
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
