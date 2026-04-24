using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMeasureTests
    {
        private static ZoneControlCaptureMeasureSO CreateSO(
            int samplesNeeded      = 6,
            int removePerBot       = 2,
            int bonusPerIntegration = 3340)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMeasureSO>();
            typeof(ZoneControlCaptureMeasureSO)
                .GetField("_samplesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, samplesNeeded);
            typeof(ZoneControlCaptureMeasureSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureMeasureSO)
                .GetField("_bonusPerIntegration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIntegration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMeasureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMeasureController>();
        }

        [Test]
        public void SO_FreshInstance_Samples_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Samples, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IntegrationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IntegrationCount, Is.EqualTo(0));
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
            var so    = CreateSO(samplesNeeded: 3, bonusPerIntegration: 3340);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(3340));
            Assert.That(so.IntegrationCount, Is.EqualTo(1));
            Assert.That(so.Samples,          Is.EqualTo(0));
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
            var so = CreateSO(samplesNeeded: 6, removePerBot: 2);
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
            var so = CreateSO(samplesNeeded: 6, removePerBot: 10);
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
        public void SO_OnMeasureIntegrated_FiresEvent()
        {
            var so    = CreateSO(samplesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMeasureSO)
                .GetField("_onMeasureIntegrated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(samplesNeeded: 2, bonusPerIntegration: 3340);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Samples,           Is.EqualTo(0));
            Assert.That(so.IntegrationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIntegrations_Accumulate()
        {
            var so = CreateSO(samplesNeeded: 2, bonusPerIntegration: 3340);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IntegrationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6680));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MeasureSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MeasureSO, Is.Null);
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
            typeof(ZoneControlCaptureMeasureController)
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
