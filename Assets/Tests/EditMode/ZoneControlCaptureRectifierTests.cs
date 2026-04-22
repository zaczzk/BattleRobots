using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRectifierTests
    {
        private static ZoneControlCaptureRectifierSO CreateSO(
            int wavesNeeded        = 6,
            int ripplePerBot       = 2,
            int bonusPerConversion = 1480)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRectifierSO>();
            typeof(ZoneControlCaptureRectifierSO)
                .GetField("_wavesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wavesNeeded);
            typeof(ZoneControlCaptureRectifierSO)
                .GetField("_ripplePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ripplePerBot);
            typeof(ZoneControlCaptureRectifierSO)
                .GetField("_bonusPerConversion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConversion);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRectifierController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRectifierController>();
        }

        [Test]
        public void SO_FreshInstance_Waves_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Waves, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConversionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConversionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWaves()
        {
            var so = CreateSO(wavesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Waves, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WavesAtThreshold()
        {
            var so    = CreateSO(wavesNeeded: 3, bonusPerConversion: 1480);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(1480));
            Assert.That(so.ConversionCount, Is.EqualTo(1));
            Assert.That(so.Waves,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(wavesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesWaves()
        {
            var so = CreateSO(wavesNeeded: 6, ripplePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Waves, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(wavesNeeded: 6, ripplePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Waves, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WaveProgress_Clamped()
        {
            var so = CreateSO(wavesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.WaveProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRectifierConverted_FiresEvent()
        {
            var so    = CreateSO(wavesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRectifierSO)
                .GetField("_onRectifierConverted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(wavesNeeded: 2, bonusPerConversion: 1480);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Waves,             Is.EqualTo(0));
            Assert.That(so.ConversionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConversions_Accumulate()
        {
            var so = CreateSO(wavesNeeded: 2, bonusPerConversion: 1480);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConversionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2960));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RectifierSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RectifierSO, Is.Null);
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
            typeof(ZoneControlCaptureRectifierController)
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
