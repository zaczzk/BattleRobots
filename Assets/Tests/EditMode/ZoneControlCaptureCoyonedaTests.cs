using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCoyonedaTests
    {
        private static ZoneControlCaptureCoyonedaSO CreateSO(
            int samplesNeeded = 6,
            int discardPerBot = 2,
            int bonusPerLift  = 2440)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCoyonedaSO>();
            typeof(ZoneControlCaptureCoyonedaSO)
                .GetField("_samplesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, samplesNeeded);
            typeof(ZoneControlCaptureCoyonedaSO)
                .GetField("_discardPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, discardPerBot);
            typeof(ZoneControlCaptureCoyonedaSO)
                .GetField("_bonusPerLift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLift);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCoyonedaController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCoyonedaController>();
        }

        [Test]
        public void SO_FreshInstance_Samples_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Samples, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LiftCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LiftCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSamples()
        {
            var so = CreateSO(samplesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Samples, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(samplesNeeded: 3, bonusPerLift: 2440);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(2440));
            Assert.That(so.LiftCount,  Is.EqualTo(1));
            Assert.That(so.Samples,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(samplesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSamples()
        {
            var so = CreateSO(samplesNeeded: 6, discardPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Samples, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(samplesNeeded: 6, discardPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Samples, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SampleProgress_Clamped()
        {
            var so = CreateSO(samplesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SampleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCoyonedaLifted_FiresEvent()
        {
            var so    = CreateSO(samplesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCoyonedaSO)
                .GetField("_onCoyonedaLifted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(samplesNeeded: 2, bonusPerLift: 2440);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Samples,           Is.EqualTo(0));
            Assert.That(so.LiftCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLifts_Accumulate()
        {
            var so = CreateSO(samplesNeeded: 2, bonusPerLift: 2440);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LiftCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4880));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CoyonedaSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CoyonedaSO, Is.Null);
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
            typeof(ZoneControlCaptureCoyonedaController)
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
