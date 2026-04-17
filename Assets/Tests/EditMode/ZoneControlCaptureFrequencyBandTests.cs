using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T392: <see cref="ZoneControlCaptureFrequencyBandSO"/> and
    /// <see cref="ZoneControlCaptureFrequencyBandController"/>.
    ///
    /// ZoneControlCaptureFrequencyBandTests (12):
    ///   SO_FreshInstance_Band_IsLow                             ×1
    ///   SO_EvaluateBand_BelowMediumThreshold_ReturnsLow         ×1
    ///   SO_EvaluateBand_MeetsHighThreshold_ReturnsHigh          ×1
    ///   SO_EvaluateBand_MeetsExtremeThreshold_ReturnsExtreme    ×1
    ///   SO_EvaluateBand_FiresBandChanged_OnTransition           ×1
    ///   SO_EvaluateBand_NoBandChange_DoesNotFireEvent           ×1
    ///   SO_GetBandLabel_ReturnsCorrectString                    ×1
    ///   SO_Reset_ReturnsBandToLow                               ×1
    ///   Controller_FreshInstance_BandSO_Null                    ×1
    ///   Controller_FreshInstance_TrackerSO_Null                 ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow               ×1
    ///   Controller_Refresh_NullBandSO_HidesPanel                ×1
    /// </summary>
    public sealed class ZoneControlCaptureFrequencyBandTests
    {
        private static ZoneControlCaptureFrequencyBandSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureFrequencyBandSO>();

        private static ZoneControlCaptureFrequencyBandController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFrequencyBandController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Band_IsLow()
        {
            var so = CreateSO();
            Assert.That(so.CurrentBand, Is.EqualTo(CaptureFrequencyBand.Low));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBand_BelowMediumThreshold_ReturnsLow()
        {
            var so   = CreateSO();
            var band = so.EvaluateBand(so.MediumThreshold - 0.01f);
            Assert.That(band, Is.EqualTo(CaptureFrequencyBand.Low));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBand_MeetsHighThreshold_ReturnsHigh()
        {
            var so   = CreateSO();
            var band = so.EvaluateBand(so.HighThreshold);
            Assert.That(band, Is.EqualTo(CaptureFrequencyBand.High));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBand_MeetsExtremeThreshold_ReturnsExtreme()
        {
            var so   = CreateSO();
            var band = so.EvaluateBand(so.ExtremeThreshold);
            Assert.That(band, Is.EqualTo(CaptureFrequencyBand.Extreme));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBand_FiresBandChanged_OnTransition()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFrequencyBandSO)
                .GetField("_onBandChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.EvaluateBand(so.ExtremeThreshold);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_EvaluateBand_NoBandChange_DoesNotFireEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFrequencyBandSO)
                .GetField("_onBandChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.EvaluateBand(0f);
            so.EvaluateBand(0f);

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetBandLabel_ReturnsCorrectString()
        {
            var so = CreateSO();
            so.EvaluateBand(so.ExtremeThreshold);
            Assert.That(so.GetBandLabel(), Is.EqualTo("Extreme"));
            so.EvaluateBand(so.HighThreshold);
            Assert.That(so.GetBandLabel(), Is.EqualTo("High"));
            so.EvaluateBand(so.MediumThreshold);
            Assert.That(so.GetBandLabel(), Is.EqualTo("Medium"));
            so.Reset();
            Assert.That(so.GetBandLabel(), Is.EqualTo("Low"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ReturnsBandToLow()
        {
            var so = CreateSO();
            so.EvaluateBand(so.ExtremeThreshold);
            so.Reset();
            Assert.That(so.CurrentBand, Is.EqualTo(CaptureFrequencyBand.Low));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BandSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BandSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TrackerSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullBandSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureFrequencyBandController)
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
