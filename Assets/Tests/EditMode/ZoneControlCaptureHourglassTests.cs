using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHourglassTests
    {
        private static ZoneControlCaptureHourglassSO CreateSO(
            int grainsNeeded     = 5,
            int spillPerBot      = 1,
            int bonusPerInversion = 805)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHourglassSO>();
            typeof(ZoneControlCaptureHourglassSO)
                .GetField("_grainsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, grainsNeeded);
            typeof(ZoneControlCaptureHourglassSO)
                .GetField("_spillPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, spillPerBot);
            typeof(ZoneControlCaptureHourglassSO)
                .GetField("_bonusPerInversion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInversion);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHourglassController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHourglassController>();
        }

        [Test]
        public void SO_FreshInstance_Grains_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Grains, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InversionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InversionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGrains()
        {
            var so = CreateSO(grainsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Grains, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_InvertsAtThreshold()
        {
            var so    = CreateSO(grainsNeeded: 3, bonusPerInversion: 805);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(805));
            Assert.That(so.InversionCount,  Is.EqualTo(1));
            Assert.That(so.Grains,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(grainsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SpillsGrains()
        {
            var so = CreateSO(grainsNeeded: 5, spillPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Grains, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(grainsNeeded: 5, spillPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Grains, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GrainProgress_Clamped()
        {
            var so = CreateSO(grainsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.GrainProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHourglassInverted_FiresEvent()
        {
            var so    = CreateSO(grainsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHourglassSO)
                .GetField("_onHourglassInverted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(grainsNeeded: 2, bonusPerInversion: 805);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Grains,            Is.EqualTo(0));
            Assert.That(so.InversionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInversions_Accumulate()
        {
            var so = CreateSO(grainsNeeded: 2, bonusPerInversion: 805);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InversionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1610));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HourglassSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HourglassSO, Is.Null);
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
            typeof(ZoneControlCaptureHourglassController)
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
