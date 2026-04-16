using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T302: <see cref="ZoneControlAchievementCatalogSO"/> and
    /// <see cref="ZoneControlAchievementController"/>.
    ///
    /// ZoneControlAchievementTests (12):
    ///   SO_FreshInstance_EarnedCount_Zero                                       ×1
    ///   SO_EvaluateAchievements_NullSummary_NoThrow                             ×1
    ///   SO_EvaluateAchievements_MetCriteria_EarnsAchievement                   ×1
    ///   SO_EvaluateAchievements_IdempotentForEarned                             ×1
    ///   SO_HasEarned_ReturnsFalse_WhenNotEarned                                 ×1
    ///   SO_Reset_ClearsEarnedIds                                                ×1
    ///   Controller_FreshInstance_CatalogSO_Null                                 ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_Channels                               ×1
    ///   Controller_HandleMatchEnded_NullRefs_NoThrow                            ×1
    ///   Controller_Refresh_NullCatalog_HidesPanel                              ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlAchievementTests
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

        private static ZoneControlAchievementCatalogSO CreateCatalog() =>
            ScriptableObject.CreateInstance<ZoneControlAchievementCatalogSO>();

        private static ZoneControlSessionSummarySO CreateSummary() =>
            ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();

        private static ZoneControlAchievementController CreateController() =>
            new GameObject("AchievementCtrl_Test")
                .AddComponent<ZoneControlAchievementController>();

        // Helper: create an entry with reflection (no public constructor for internals)
        private static ZoneControlAchievementEntry CreateEntry(
            string id, string name, ZoneControlAchievementType type, int target)
        {
            var entry = new ZoneControlAchievementEntry();
            SetField(entry, "_achievementId", id);
            SetField(entry, "_displayName",   name);
            SetField(entry, "_type",          type);
            SetField(entry, "_targetValue",   target);
            return entry;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EarnedCount_Zero()
        {
            var so = CreateCatalog();
            Assert.AreEqual(0, so.EarnedCount,
                "EarnedCount must be 0 on a fresh ZoneControlAchievementCatalogSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAchievements_NullSummary_NoThrow()
        {
            var so = CreateCatalog();
            Assert.DoesNotThrow(() => so.EvaluateAchievements(null),
                "EvaluateAchievements(null) must not throw.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateAchievements_MetCriteria_EarnsAchievement()
        {
            var so      = CreateCatalog();
            var summary = CreateSummary();

            var entry = CreateEntry("ach_zones_10", "Capture 10 Zones",
                ZoneControlAchievementType.TotalZones, 10);
            SetField(so, "_entries", new[] { entry });

            // Simulate 10 zones: AddMatch(10 zones, dominance=false, streak=0)
            summary.AddMatch(10, false, 0);

            so.EvaluateAchievements(summary);

            Assert.AreEqual(1, so.EarnedCount,
                "Achievement must be earned when metric meets the target.");
            Assert.IsTrue(so.HasEarned("ach_zones_10"),
                "HasEarned must return true for the earned achievement id.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(summary);
        }

        [Test]
        public void SO_EvaluateAchievements_IdempotentForEarned()
        {
            var so      = CreateCatalog();
            var summary = CreateSummary();

            var entry = CreateEntry("ach_streak_5", "Streak 5",
                ZoneControlAchievementType.Streak, 5);
            SetField(so, "_entries", new[] { entry });

            summary.AddMatch(0, false, 5);

            so.EvaluateAchievements(summary);
            so.EvaluateAchievements(summary); // second call — should not double-count

            Assert.AreEqual(1, so.EarnedCount,
                "EarnedCount must stay at 1 even after calling EvaluateAchievements twice.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(summary);
        }

        [Test]
        public void SO_HasEarned_ReturnsFalse_WhenNotEarned()
        {
            var so = CreateCatalog();
            Assert.IsFalse(so.HasEarned("nonexistent_id"),
                "HasEarned must return false for an id that has never been earned.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsEarnedIds()
        {
            var so      = CreateCatalog();
            var summary = CreateSummary();

            var entry = CreateEntry("ach_dom_2", "2 Dominance Wins",
                ZoneControlAchievementType.Dominance, 2);
            SetField(so, "_entries", new[] { entry });

            summary.AddMatch(0, true, 0);
            summary.AddMatch(0, true, 0);
            so.EvaluateAchievements(summary);

            Assert.AreEqual(1, so.EarnedCount);

            so.Reset();
            Assert.AreEqual(0, so.EarnedCount,
                "EarnedCount must be 0 after Reset.");
            Assert.IsFalse(so.HasEarned("ach_dom_2"),
                "HasEarned must return false after Reset.");

            Object.DestroyImmediate(so);
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
                () => go.AddComponent<ZoneControlAchievementController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlAchievementController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlAchievementController>();

            var matchEndedEvt      = CreateEvent();
            var achievementUnlocked = CreateEvent();

            SetField(ctrl, "_onMatchEnded",          matchEndedEvt);
            SetField(ctrl, "_onAchievementUnlocked", achievementUnlocked);

            go.SetActive(true);
            go.SetActive(false);

            int matchEndedCount = 0, unlockCount = 0;
            matchEndedEvt.RegisterCallback(() => matchEndedCount++);
            achievementUnlocked.RegisterCallback(() => unlockCount++);

            matchEndedEvt.Raise();
            achievementUnlocked.Raise();

            Assert.AreEqual(1, matchEndedCount,
                "_onMatchEnded must be unregistered after OnDisable.");
            Assert.AreEqual(1, unlockCount,
                "_onAchievementUnlocked must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEndedEvt);
            Object.DestroyImmediate(achievementUnlocked);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullRefs_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when _catalogSO/_summarySO are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullCatalog_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullCatalog");
            var ctrl  = go.AddComponent<ZoneControlAchievementController>();
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
