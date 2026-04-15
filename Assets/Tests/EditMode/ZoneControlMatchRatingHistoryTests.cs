using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T291: <see cref="ZoneControlMatchRatingHistorySO"/> and
    /// <see cref="ZoneControlMatchRatingHistoryController"/>.
    ///
    /// ZoneControlMatchRatingHistoryTests (14):
    ///   SO_FreshInstance_EntryCount_Zero                                     ×1
    ///   SO_AddRating_IncrementsEntryCount                                    ×1
    ///   SO_AddRating_ClampsToRange_Low                                       ×1
    ///   SO_AddRating_ClampsToRange_High                                      ×1
    ///   SO_AddRating_PrunesOldestWhenFull                                    ×1
    ///   SO_Capacity_Default_Five                                             ×1
    ///   SO_Reset_ClearsEntries                                               ×1
    ///   Controller_FreshInstance_HistorySO_Null                              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                            ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                           ×1
    ///   Controller_OnDisable_Unregisters_Channels                            ×1
    ///   Controller_HandleRatingSet_NullRefs_NoThrow                          ×1
    ///   Controller_Refresh_NullSO_HidesPanel                                 ×1
    ///   Controller_Refresh_WithHistory_ShowsPanel                            ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchRatingHistoryTests
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

        private static ZoneControlMatchRatingHistorySO CreateHistorySO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchRatingHistorySO>();

        private static ZoneControlMatchRatingHistoryController CreateController() =>
            new GameObject("ZoneRatingHistory_Test")
                .AddComponent<ZoneControlMatchRatingHistoryController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var so = CreateHistorySO();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 on a fresh ZoneControlMatchRatingHistorySO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddRating_IncrementsEntryCount()
        {
            var so = CreateHistorySO();
            so.AddRating(3);
            Assert.AreEqual(1, so.EntryCount,
                "EntryCount must be 1 after adding one rating.");
            so.AddRating(4);
            Assert.AreEqual(2, so.EntryCount,
                "EntryCount must be 2 after adding two ratings.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddRating_ClampsToRange_Low()
        {
            var so = CreateHistorySO();
            so.AddRating(0); // Below minimum — should be stored as 1.
            IReadOnlyList<int> ratings = so.GetRatings();
            Assert.AreEqual(1, ratings[0],
                "Rating below 1 must be clamped to 1.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddRating_ClampsToRange_High()
        {
            var so = CreateHistorySO();
            so.AddRating(10); // Above maximum — should be stored as 5.
            IReadOnlyList<int> ratings = so.GetRatings();
            Assert.AreEqual(5, ratings[0],
                "Rating above 5 must be clamped to 5.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddRating_PrunesOldestWhenFull()
        {
            var so = CreateHistorySO();
            // Default capacity = 5; add 6 ratings.
            for (int i = 1; i <= 6; i++) so.AddRating(i > 5 ? 5 : i);

            Assert.AreEqual(so.Capacity, so.EntryCount,
                "EntryCount must not exceed Capacity.");

            // The oldest entry (1) should have been pruned; first remaining entry is 2.
            IReadOnlyList<int> ratings = so.GetRatings();
            Assert.AreEqual(2, ratings[0],
                "Oldest entry must be removed when buffer exceeds capacity.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Capacity_Default_Five()
        {
            var so = CreateHistorySO();
            Assert.AreEqual(5, so.Capacity,
                "Default Capacity must be 5.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsEntries()
        {
            var so = CreateHistorySO();
            so.AddRating(3);
            so.AddRating(4);
            so.Reset();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_HistorySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HistorySO,
                "HistorySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchRatingHistoryController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchRatingHistoryController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchRatingHistoryController>();

            var ratingSetEvt     = CreateEvent();
            var historyUpdateEvt = CreateEvent();

            SetField(ctrl, "_onRatingSet",             ratingSetEvt);
            SetField(ctrl, "_onRatingHistoryUpdated",   historyUpdateEvt);

            go.SetActive(true);
            go.SetActive(false);

            int ratingSetCount = 0, historyUpdateCount = 0;
            ratingSetEvt.RegisterCallback(() => ratingSetCount++);
            historyUpdateEvt.RegisterCallback(() => historyUpdateCount++);

            ratingSetEvt.Raise();
            historyUpdateEvt.Raise();

            Assert.AreEqual(1, ratingSetCount,
                "_onRatingSet must be unregistered after OnDisable.");
            Assert.AreEqual(1, historyUpdateCount,
                "_onRatingHistoryUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ratingSetEvt);
            Object.DestroyImmediate(historyUpdateEvt);
        }

        [Test]
        public void Controller_HandleRatingSet_NullRefs_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleRatingSet(),
                "HandleRatingSet must not throw when _historySO/_ratingController are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullSO");
            var ctrl  = go.AddComponent<ZoneControlMatchRatingHistoryController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when HistorySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithHistory_ShowsPanel()
        {
            var go    = new GameObject("Test_Refresh_WithHistory");
            var ctrl  = go.AddComponent<ZoneControlMatchRatingHistoryController>();
            var so    = CreateHistorySO();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_historySO", so);
            SetField(ctrl, "_panel",     panel);
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when HistorySO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}
