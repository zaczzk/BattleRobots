using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlScoutReportSO"/> and
    /// <see cref="ZoneControlScoutReportController"/>.
    /// </summary>
    public sealed class ZoneControlScoutReportTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlScoutReportSO CreateReportSO() =>
            ScriptableObject.CreateInstance<ZoneControlScoutReportSO>();

        private static ZoneControlScoutReportController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlScoutReportController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsGenerated_False()
        {
            var so = CreateReportSO();
            Assert.That(so.IsGenerated, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GetSummary_ReturnsNoData()
        {
            var so = CreateReportSO();
            Assert.That(so.GetSummary(), Is.EqualTo("No data"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateReport_ZeroDuration_DoesNotGenerate()
        {
            var so = CreateReportSO();
            so.UpdateReport(5, 0f);
            Assert.That(so.IsGenerated, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateReport_CalculatesCaptureRate()
        {
            var so = CreateReportSO();
            // 6 captures in 60 seconds = 6 caps/min
            so.UpdateReport(6, 60f);
            Assert.That(so.BotCaptureRate, Is.EqualTo(6f).Within(0.01f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateReport_AboveThreshold_SetsSystematic()
        {
            var so = CreateReportSO();
            // default threshold = 1.0; 6 caps in 60s = 6 caps/min → Systematic
            so.UpdateReport(6, 60f);
            Assert.That(so.BotPattern, Is.EqualTo(ZoneControlBotPattern.Systematic));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateReport_BelowThreshold_SetsRandom()
        {
            var so = CreateReportSO();
            // 1 cap in 600s = 0.1 caps/min → Random
            so.UpdateReport(1, 600f);
            Assert.That(so.BotPattern, Is.EqualTo(ZoneControlBotPattern.Random));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UpdateReport_FiresReportGenerated()
        {
            var so      = CreateReportSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoutReportSO)
                .GetField("_onReportGenerated",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.UpdateReport(3, 60f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsGeneratedFlag()
        {
            var so = CreateReportSO();
            so.UpdateReport(5, 60f);
            so.Reset();
            Assert.That(so.IsGenerated,    Is.False);
            Assert.That(so.BotCaptureRate, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ReportSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ReportSO, Is.Null);
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
        public void Controller_Refresh_NullReportSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlScoutReportController)
                .GetField("_panel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_RecordBotCapture_IncrementsBotCount()
        {
            var ctrl = CreateController();
            ctrl.RecordBotCapture();
            ctrl.RecordBotCapture();
            Assert.That(ctrl.BotCaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
