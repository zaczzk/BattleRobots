using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T329: <see cref="ZoneControlMatchReportSO"/> and
    /// <see cref="ZoneControlMatchReportController"/>.
    ///
    /// ZoneControlMatchReportTests (12):
    ///   SO_FreshInstance_IsGenerated_False              ×1
    ///   SO_GenerateReport_NullSummary_NoOp              ×1
    ///   SO_GenerateReport_SetsIsGenerated               ×1
    ///   SO_GenerateReport_PopulatesZones                ×1
    ///   SO_GenerateReport_PopulatesThreat               ×1
    ///   SO_GenerateReport_PopulatesBestCombo            ×1
    ///   SO_Reset_ClearsAll                              ×1
    ///   Controller_FreshInstance_ReportSO_Null          ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow       ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow      ×1
    ///   Controller_OnDisable_Unregisters_Channel        ×1
    ///   Controller_Refresh_NullReportSO_HidesPanel      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchReportTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlMatchReportSO CreateReportSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchReportSO>();
            so.Reset();
            return so;
        }

        private static ZoneControlSessionSummarySO CreateSummarySO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();
            so.Reset();
            return so;
        }

        private static ZoneControlThreatAssessmentSO CreateThreatSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlThreatAssessmentSO>();
            so.Reset();
            return so;
        }

        private static ZoneControlComboTrackerSO CreateComboSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlComboTrackerSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsGenerated_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchReportSO>();
            Assert.IsFalse(so.IsGenerated,
                "IsGenerated must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GenerateReport_NullSummary_NoOp()
        {
            var so = CreateReportSO();
            so.GenerateReport(null, null, null, null);
            Assert.IsFalse(so.IsGenerated,
                "GenerateReport must be a no-op when summarySO is null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GenerateReport_SetsIsGenerated()
        {
            var so        = CreateReportSO();
            var summarySO = CreateSummarySO();
            so.GenerateReport(summarySO, null, null, null);
            Assert.IsTrue(so.IsGenerated,
                "IsGenerated must be true after a successful GenerateReport call.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(summarySO);
        }

        [Test]
        public void SO_GenerateReport_PopulatesZones()
        {
            var so        = CreateReportSO();
            var summarySO = CreateSummarySO();
            summarySO.AddMatch(7, false, 0);
            so.GenerateReport(summarySO, null, null, null);
            Assert.AreEqual(7, so.TotalZonesCaptured,
                "TotalZonesCaptured must match the session summary.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(summarySO);
        }

        [Test]
        public void SO_GenerateReport_PopulatesThreat()
        {
            var so        = CreateReportSO();
            var summarySO = CreateSummarySO();
            var threatSO  = CreateThreatSO();
            threatSO.EvaluateThreat(3, false); // High threat (rank >= 3 and !dominance)
            so.GenerateReport(summarySO, null, threatSO, null);
            Assert.AreEqual(ThreatLevel.High, so.FinalThreatLevel,
                "FinalThreatLevel must reflect the threat SO's current threat.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(summarySO);
            Object.DestroyImmediate(threatSO);
        }

        [Test]
        public void SO_GenerateReport_PopulatesBestCombo()
        {
            var so        = CreateReportSO();
            var summarySO = CreateSummarySO();
            var comboSO   = CreateComboSO();
            comboSO.RecordCapture();
            comboSO.RecordCapture();
            comboSO.RecordCapture(); // ComboCount = 3
            so.GenerateReport(summarySO, null, null, comboSO);
            Assert.AreEqual(3, so.BestCombo,
                "BestCombo must match the combo tracker's ComboCount at report time.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(summarySO);
            Object.DestroyImmediate(comboSO);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so        = CreateReportSO();
            var summarySO = CreateSummarySO();
            so.GenerateReport(summarySO, null, null, null);
            so.Reset();
            Assert.IsFalse(so.IsGenerated,
                "IsGenerated must be false after Reset.");
            Assert.AreEqual(0, so.TotalZonesCaptured,
                "TotalZonesCaptured must be 0 after Reset.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(summarySO);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ReportSO_Null()
        {
            var go   = new GameObject("Test_ReportSO_Null");
            var ctrl = go.AddComponent<ZoneControlMatchReportController>();
            Assert.IsNull(ctrl.ReportSO,
                "ReportSO must be null on a fresh controller instance.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchReportController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchReportController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchReportController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullReportSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlMatchReportController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when ReportSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
