using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T301:
    ///   <see cref="ZoneControlReplayExportSO"/> and
    ///   <see cref="ZoneControlReplayExportController"/>.
    ///
    /// ZoneControlReplayExportTests (12):
    ///   ExportSO_FreshInstance_HasExport_False                   ×1
    ///   ExportSO_ExportToJson_NullReplay_ReturnsEmpty            ×1
    ///   ExportSO_ExportToJson_EmptyReplay_ReturnsEmpty           ×1
    ///   ExportSO_ExportToJson_WithSnapshots_ReturnsJson          ×1
    ///   ExportSO_HasExport_True_AfterSuccessfulExport            ×1
    ///   ExportSO_ParsePayload_NullJson_ReturnsNull               ×1
    ///   ExportSO_ParsePayload_ValidJson_ReturnsPayload           ×1
    ///   ExportSO_Reset_ClearsExport                              ×1
    ///   Controller_FreshInstance_ExportSO_Null                   ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlReplayExportTests
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

        private static ZoneControlReplayExportSO CreateExportSO() =>
            ScriptableObject.CreateInstance<ZoneControlReplayExportSO>();

        private static ZoneControlReplaySO CreateReplaySO(int maxSnapshots = 10)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlReplaySO>();
            SetField(so, "_maxSnapshots", maxSnapshots);
            return so;
        }

        private static ZoneControlReplayExportController CreateController() =>
            new GameObject("ReplayExport_Test").AddComponent<ZoneControlReplayExportController>();

        // ── Export SO Tests ───────────────────────────────────────────────────

        [Test]
        public void ExportSO_FreshInstance_HasExport_False()
        {
            var so = CreateExportSO();
            Assert.IsFalse(so.HasExport,
                "HasExport must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ExportSO_ExportToJson_NullReplay_ReturnsEmpty()
        {
            var so   = CreateExportSO();
            string json = so.ExportToJson(null);
            Assert.AreEqual(string.Empty, json,
                "ExportToJson must return empty string when replay is null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ExportSO_ExportToJson_EmptyReplay_ReturnsEmpty()
        {
            var so      = CreateExportSO();
            var replay  = CreateReplaySO();
            // No snapshots added — Count == 0
            string json = so.ExportToJson(replay);
            Assert.AreEqual(string.Empty, json,
                "ExportToJson must return empty string when replay buffer is empty.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(replay);
        }

        [Test]
        public void ExportSO_ExportToJson_WithSnapshots_ReturnsJson()
        {
            var so     = CreateExportSO();
            var replay = CreateReplaySO();
            replay.AddSnapshot(1.0f, new bool[] { true, false });
            replay.AddSnapshot(2.0f, new bool[] { false, true });

            string json = so.ExportToJson(replay);

            Assert.IsFalse(string.IsNullOrEmpty(json),
                "ExportToJson must return a non-empty JSON string when replay has snapshots.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(replay);
        }

        [Test]
        public void ExportSO_HasExport_True_AfterSuccessfulExport()
        {
            var so     = CreateExportSO();
            var replay = CreateReplaySO();
            replay.AddSnapshot(1.0f, new bool[] { true });

            so.ExportToJson(replay);

            Assert.IsTrue(so.HasExport,
                "HasExport must be true after a successful export.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(replay);
        }

        [Test]
        public void ExportSO_ParsePayload_NullJson_ReturnsNull()
        {
            var so = CreateExportSO();
            var result = so.ParsePayload(null);
            Assert.IsNull(result,
                "ParsePayload must return null when given null input.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ExportSO_ParsePayload_ValidJson_ReturnsPayload()
        {
            var so     = CreateExportSO();
            var replay = CreateReplaySO();
            replay.AddSnapshot(0.5f, new bool[] { true, false, true });

            string json = so.ExportToJson(replay);
            var payload = so.ParsePayload(json);

            Assert.IsNotNull(payload,
                "ParsePayload must return a non-null payload for valid JSON.");
            Assert.AreEqual(1, payload.Count,
                "Parsed payload must contain the same number of snapshots as exported.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(replay);
        }

        [Test]
        public void ExportSO_Reset_ClearsExport()
        {
            var so     = CreateExportSO();
            var replay = CreateReplaySO();
            replay.AddSnapshot(1.0f, new bool[] { true });

            so.ExportToJson(replay);
            so.Reset();

            Assert.IsFalse(so.HasExport,
                "Reset must clear the cached export JSON.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(replay);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ExportSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ExportSO,
                "ExportSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlReplayExportController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlReplayExportController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlReplayExportController>();

            var evt = CreateEvent();
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
    }
}
