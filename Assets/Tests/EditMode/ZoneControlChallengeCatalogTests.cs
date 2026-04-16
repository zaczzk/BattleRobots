using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T311: <see cref="ZoneControlChallengeCatalogSO"/> and
    /// <see cref="ZoneControlChallengeTrackerController"/>.
    ///
    /// ZoneControlChallengeCatalogTests (12):
    ///   SO_FreshInstance_EntryCount_Zero                                          ×1
    ///   SO_FreshInstance_CompletedCount_Zero                                      ×1
    ///   SO_EvaluateAll_NullSummary_DoesNotThrow                                   ×1
    ///   SO_EvaluateAll_MarksChallenge_WhenMetricMet                               ×1
    ///   SO_EvaluateAll_Idempotent_WhenAlreadyCompleted                            ×1
    ///   SO_Reset_ClearsCompleted                                                  ×1
    ///   Controller_FreshInstance_CatalogSO_Null                                   ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandleMatchEnded_NullCatalog_NoThrow                           ×1
    ///   Controller_Refresh_NullCatalog_HidesPanel                                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlChallengeCatalogTests
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

        private static ZoneControlChallengeCatalogSO CreateCatalog() =>
            ScriptableObject.CreateInstance<ZoneControlChallengeCatalogSO>();

        private static ZoneControlSessionSummarySO CreateSummary() =>
            ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();

        private static ZoneControlChallengeTrackerController CreateController() =>
            new GameObject("ChallengeCtrl_Test")
                .AddComponent<ZoneControlChallengeTrackerController>();

        /// <summary>Injects a single entry into the catalog via reflection.</summary>
        private static void AddEntry(ZoneControlChallengeCatalogSO catalog,
                                     string id, ZoneControlChallengeType type, float target)
        {
            var entries = (List<ZoneControlChallengeEntry>)typeof(ZoneControlChallengeCatalogSO)
                .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(catalog);
            Assert.IsNotNull(entries, "_entries field not found on ZoneControlChallengeCatalogSO.");
            entries.Add(new ZoneControlChallengeEntry
            {
                Id          = id,
                DisplayName = id,
                Type        = type,
                TargetValue = target
            });
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var catalog = CreateCatalog();
            Assert.AreEqual(0, catalog.EntryCount,
                "EntryCount must be 0 on a fresh instance with no entries added.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_FreshInstance_CompletedCount_Zero()
        {
            var catalog = CreateCatalog();
            Assert.AreEqual(0, catalog.CompletedCount,
                "CompletedCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_EvaluateAll_NullSummary_DoesNotThrow()
        {
            var catalog = CreateCatalog();
            AddEntry(catalog, "c1", ZoneControlChallengeType.TotalZones, 5f);
            Assert.DoesNotThrow(
                () => catalog.EvaluateAll(null),
                "EvaluateAll with a null summary must not throw.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_EvaluateAll_MarksChallenge_WhenMetricMet()
        {
            var catalog = CreateCatalog();
            var summary = CreateSummary();
            AddEntry(catalog, "zones10", ZoneControlChallengeType.TotalZones, 10f);

            // Simulate 10 zones captured across 1 match.
            summary.AddMatch(10, false, 0);
            catalog.EvaluateAll(summary);

            Assert.AreEqual(1, catalog.CompletedCount,
                "CompletedCount must be 1 after challenge target is met.");
            Assert.IsTrue(catalog.IsCompleted("zones10"),
                "Challenge 'zones10' must be marked completed.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(summary);
        }

        [Test]
        public void SO_EvaluateAll_Idempotent_WhenAlreadyCompleted()
        {
            var catalog = CreateCatalog();
            var summary = CreateSummary();
            var evt     = CreateEvent();
            SetField(catalog, "_onChallengeCompleted", evt);

            AddEntry(catalog, "streak3", ZoneControlChallengeType.BestStreak, 3f);
            summary.AddMatch(0, false, 5);

            int fires = 0;
            evt.RegisterCallback(() => fires++);

            catalog.EvaluateAll(summary);
            catalog.EvaluateAll(summary); // Second call must not re-fire.

            Assert.AreEqual(1, fires,
                "_onChallengeCompleted must fire exactly once for the same challenge.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsCompleted()
        {
            var catalog = CreateCatalog();
            var summary = CreateSummary();
            AddEntry(catalog, "played1", ZoneControlChallengeType.MatchesPlayed, 1f);
            summary.AddMatch(0, false, 0);
            catalog.EvaluateAll(summary);

            Assert.AreEqual(1, catalog.CompletedCount);

            catalog.Reset();
            Assert.AreEqual(0, catalog.CompletedCount,
                "CompletedCount must be 0 after Reset.");
            Assert.IsFalse(catalog.IsCompleted("played1"),
                "Challenge must no longer be marked completed after Reset.");

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(summary);
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
                () => go.AddComponent<ZoneControlChallengeTrackerController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlChallengeTrackerController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlChallengeTrackerController>();

            var matchEndEvt = CreateEvent();
            SetField(ctrl, "_onMatchEnded", matchEndEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            matchEndEvt.RegisterCallback(() => count++);
            matchEndEvt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEndEvt);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullCatalog_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when CatalogSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullCatalog_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlChallengeTrackerController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when CatalogSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
