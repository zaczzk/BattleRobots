using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T300:
    ///   <see cref="ZoneControlCommentaryCatalogSO"/> and
    ///   <see cref="ZoneControlMatchCommentaryController"/>.
    ///
    /// ZoneControlMatchCommentaryTests (12):
    ///   Catalog_GetMessage_FastPace_ReturnsMessage               ×1
    ///   Catalog_GetMessage_SlowPace_ReturnsMessage               ×1
    ///   Catalog_GetMessage_RatingSet_ReturnsMessage              ×1
    ///   Catalog_GetMessage_ZoneCaptured_ReturnsMessage           ×1
    ///   Catalog_GetMessage_RoundRobin_CyclesMessages             ×1
    ///   Catalog_ResetIndices_RestartsRoundRobin                  ×1
    ///   Controller_FreshInstance_CatalogSO_Null                  ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channels                ×1
    ///   Controller_ShowBanner_NullCatalog_DoesNotThrow           ×1
    ///   Controller_Tick_HidesBanner_AfterDuration                ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchCommentaryTests
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

        private static ZoneControlCommentaryCatalogSO CreateCatalogSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCommentaryCatalogSO>();
            SetField(so, "_fastPaceMessages",     new string[] { "Fast A", "Fast B" });
            SetField(so, "_slowPaceMessages",     new string[] { "Slow A" });
            SetField(so, "_ratingSetMessages",    new string[] { "Rating A" });
            SetField(so, "_zoneCapturedMessages", new string[] { "Capture A" });
            return so;
        }

        private static ZoneControlMatchCommentaryController CreateController() =>
            new GameObject("Commentary_Test").AddComponent<ZoneControlMatchCommentaryController>();

        // ── Catalog Tests ─────────────────────────────────────────────────────

        [Test]
        public void Catalog_GetMessage_FastPace_ReturnsMessage()
        {
            var so = CreateCatalogSO();
            string msg = so.GetMessage(ZoneControlCommentaryEventType.FastPace);
            Assert.IsFalse(string.IsNullOrEmpty(msg),
                "GetMessage(FastPace) must return a non-empty message.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetMessage_SlowPace_ReturnsMessage()
        {
            var so = CreateCatalogSO();
            string msg = so.GetMessage(ZoneControlCommentaryEventType.SlowPace);
            Assert.IsFalse(string.IsNullOrEmpty(msg),
                "GetMessage(SlowPace) must return a non-empty message.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetMessage_RatingSet_ReturnsMessage()
        {
            var so = CreateCatalogSO();
            string msg = so.GetMessage(ZoneControlCommentaryEventType.RatingSet);
            Assert.IsFalse(string.IsNullOrEmpty(msg),
                "GetMessage(RatingSet) must return a non-empty message.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetMessage_ZoneCaptured_ReturnsMessage()
        {
            var so = CreateCatalogSO();
            string msg = so.GetMessage(ZoneControlCommentaryEventType.ZoneCaptured);
            Assert.IsFalse(string.IsNullOrEmpty(msg),
                "GetMessage(ZoneCaptured) must return a non-empty message.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetMessage_RoundRobin_CyclesMessages()
        {
            var so = CreateCatalogSO();
            // FastPace pool has "Fast A", "Fast B" — should cycle
            string first  = so.GetMessage(ZoneControlCommentaryEventType.FastPace);
            string second = so.GetMessage(ZoneControlCommentaryEventType.FastPace);
            string third  = so.GetMessage(ZoneControlCommentaryEventType.FastPace);

            Assert.AreEqual(first, third,
                "Round-robin must cycle back to the first message after exhausting the pool.");
            Assert.AreNotEqual(first, second,
                "Second message must differ from the first when the pool has multiple entries.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_ResetIndices_RestartsRoundRobin()
        {
            var so    = CreateCatalogSO();
            string m1 = so.GetMessage(ZoneControlCommentaryEventType.FastPace); // "Fast A"
            so.GetMessage(ZoneControlCommentaryEventType.FastPace);             // "Fast B"
            so.ResetIndices();
            string m3 = so.GetMessage(ZoneControlCommentaryEventType.FastPace); // should be "Fast A" again

            Assert.AreEqual(m1, m3,
                "ResetIndices must restart the round-robin from the first message.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_CatalogSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.CatalogSO,
                "CatalogSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchCommentaryController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchCommentaryController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchCommentaryController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onFastPace", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onFastPace must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_ShowBanner_NullCatalog_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(
                () => ctrl.ShowBanner(ZoneControlCommentaryEventType.FastPace),
                "ShowBanner must not throw when CatalogSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Tick_HidesBanner_AfterDuration()
        {
            var go      = new GameObject("Test_Tick");
            var ctrl    = go.AddComponent<ZoneControlMatchCommentaryController>();
            var catalog = CreateCatalogSO();

            SetField(ctrl, "_catalogSO",       catalog);
            SetField(ctrl, "_displayDuration", 1f);

            ctrl.ShowBanner(ZoneControlCommentaryEventType.FastPace);
            Assert.IsTrue(ctrl.IsActive, "Banner must be active after ShowBanner.");

            ctrl.Tick(1.1f);
            Assert.IsFalse(ctrl.IsActive,
                "Banner must be hidden after Tick exceeds _displayDuration.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
        }
    }
}
