using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDemodulatorTests
    {
        private static ZoneControlCaptureDemodulatorSO CreateSO(
            int samplesNeeded      = 6,
            int noisePerBot        = 2,
            int bonusPerExtraction = 1645)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDemodulatorSO>();
            typeof(ZoneControlCaptureDemodulatorSO)
                .GetField("_samplesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, samplesNeeded);
            typeof(ZoneControlCaptureDemodulatorSO)
                .GetField("_noisePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, noisePerBot);
            typeof(ZoneControlCaptureDemodulatorSO)
                .GetField("_bonusPerExtraction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExtraction);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDemodulatorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDemodulatorController>();
        }

        [Test]
        public void SO_FreshInstance_Samples_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Samples, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExtractionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExtractionCount, Is.EqualTo(0));
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
            var so    = CreateSO(samplesNeeded: 3, bonusPerExtraction: 1645);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(1645));
            Assert.That(so.ExtractionCount,   Is.EqualTo(1));
            Assert.That(so.Samples,           Is.EqualTo(0));
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
            var so = CreateSO(samplesNeeded: 6, noisePerBot: 2);
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
            var so = CreateSO(samplesNeeded: 6, noisePerBot: 10);
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
        public void SO_OnDemodulatorExtracted_FiresEvent()
        {
            var so    = CreateSO(samplesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDemodulatorSO)
                .GetField("_onDemodulatorExtracted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(samplesNeeded: 2, bonusPerExtraction: 1645);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Samples,           Is.EqualTo(0));
            Assert.That(so.ExtractionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExtractions_Accumulate()
        {
            var so = CreateSO(samplesNeeded: 2, bonusPerExtraction: 1645);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExtractionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3290));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DemodulatorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DemodulatorSO, Is.Null);
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
            typeof(ZoneControlCaptureDemodulatorController)
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
