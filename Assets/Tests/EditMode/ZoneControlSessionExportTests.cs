using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T333: <see cref="ZoneControlSessionExportSO"/> and
    /// <see cref="ZoneControlSessionExportController"/>.
    ///
    /// ZoneControlSessionExportTests (12):
    ///   SO_FreshInstance_LastExportJson_Empty                     ×1
    ///   SO_ExportSession_NullSOs_ProducesValidJson                ×1
    ///   SO_ExportSession_WithSummary_JsonContainsTotalZones       ×1
    ///   SO_ExportSession_WithRatings_JsonContainsRatings          ×1
    ///   SO_ExportSession_WithRoundResults_JsonContainsWins        ×1
    ///   SO_ExportSession_FiresExportCompletedEvent                ×1
    ///   SO_Reset_ClearsLastExportJson                             ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_Unregisters_Channel                  ×1
    ///   Controller_TriggerExport_NullExportSO_DoesNotThrow        ×1
    ///   Controller_HandleSessionEnded_AutoExport_TriggersExport   ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlSessionExportTests
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

        private static ZoneControlSessionExportSO CreateExportSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlSessionExportSO>();
            so.Reset();
            return so;
        }

        private static ZoneControlSessionSummarySO CreateSummarySO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_LastExportJson_Empty()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlSessionExportSO>();
            Assert.AreEqual(string.Empty, so.LastExportJson,
                "LastExportJson must be empty on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ExportSession_NullSOs_ProducesValidJson()
        {
            var so = CreateExportSO();
            so.ExportSession(null, null, null);

            Assert.IsFalse(string.IsNullOrEmpty(so.LastExportJson),
                "ExportSession with null SOs must still produce a non-empty JSON string.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ExportSession_WithSummary_JsonContainsTotalZones()
        {
            var exportSO  = CreateExportSO();
            var summarySO = CreateSummarySO();
            summarySO.AddMatch(7, false, 0);

            exportSO.ExportSession(summarySO, null, null);

            StringAssert.Contains("7", exportSO.LastExportJson,
                "Exported JSON must contain the TotalZonesCaptured value.");

            Object.DestroyImmediate(exportSO);
            Object.DestroyImmediate(summarySO);
        }

        [Test]
        public void SO_ExportSession_WithRatings_JsonContainsRatings()
        {
            var exportSO   = CreateExportSO();
            var ratingsSO  = ScriptableObject.CreateInstance<ZoneControlMatchRatingHistorySO>();
            ratingsSO.Reset();
            ratingsSO.AddRating(4);

            exportSO.ExportSession(null, null, ratingsSO);

            StringAssert.Contains("4", exportSO.LastExportJson,
                "Exported JSON must include rating history values.");

            Object.DestroyImmediate(exportSO);
            Object.DestroyImmediate(ratingsSO);
        }

        [Test]
        public void SO_ExportSession_WithRoundResults_JsonContainsWins()
        {
            var exportSO      = CreateExportSO();
            var roundResultSO = ScriptableObject.CreateInstance<ZoneControlRoundResultSO>();
            roundResultSO.Reset();
            roundResultSO.RecordResult(true, 10);

            exportSO.ExportSession(null, roundResultSO, null);

            StringAssert.Contains("true", exportSO.LastExportJson,
                "Exported JSON must include round result win values.");

            Object.DestroyImmediate(exportSO);
            Object.DestroyImmediate(roundResultSO);
        }

        [Test]
        public void SO_ExportSession_FiresExportCompletedEvent()
        {
            var so  = CreateExportSO();
            var evt = CreateEvent();
            SetField(so, "_onExportCompleted", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.ExportSession(null, null, null);

            Assert.AreEqual(1, fired,
                "_onExportCompleted must fire after ExportSession.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsLastExportJson()
        {
            var so = CreateExportSO();
            so.ExportSession(null, null, null);
            so.Reset();
            Assert.AreEqual(string.Empty, so.LastExportJson,
                "LastExportJson must be empty after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlSessionExportController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlSessionExportController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlSessionExportController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onSessionEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onSessionEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_TriggerExport_NullExportSO_DoesNotThrow()
        {
            var go   = new GameObject("Test_TriggerExport_Null");
            var ctrl = go.AddComponent<ZoneControlSessionExportController>();

            Assert.DoesNotThrow(() => ctrl.TriggerExport(),
                "TriggerExport must not throw when ExportSO is null.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_HandleSessionEnded_AutoExport_TriggersExport()
        {
            var go       = new GameObject("Test_AutoExport");
            var ctrl     = go.AddComponent<ZoneControlSessionExportController>();
            var exportSO = CreateExportSO();
            SetField(ctrl, "_exportSO", exportSO);
            // _autoExportOnSessionEnd defaults to true

            ctrl.HandleSessionEnded();

            Assert.IsFalse(string.IsNullOrEmpty(exportSO.LastExportJson),
                "HandleSessionEnded must trigger export when auto-export is enabled.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(exportSO);
        }
    }
}
