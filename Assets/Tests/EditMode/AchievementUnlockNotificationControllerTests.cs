using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T208:
    ///   <see cref="AchievementUnlockNotificationController"/>.
    ///
    /// AchievementUnlockNotificationControllerTests (14):
    ///   FreshInstance_PlayerAchievementsIsNull                          ×1
    ///   FreshInstance_CatalogIsNull                                     ×1
    ///   FreshInstance_DisplayTimer_IsZero                               ×1
    ///   FreshInstance_DisplayDuration_IsThree                           ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                               ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                              ×1
    ///   OnDisable_Unregisters                                           ×1
    ///   OnDisable_HidesPanelAndResetsTimer                              ×1
    ///   OnAchievementUnlocked_NullPlayerAchievements_DoesNotShow        ×1
    ///   OnAchievementUnlocked_NullCatalog_ShowsEmptyName               ×1
    ///   OnAchievementUnlocked_FoundDef_ShowsName                       ×1
    ///   OnAchievementUnlocked_FoundDef_ShowsReward                     ×1
    ///   Tick_AdvancesAndHidesPanel                                      ×1
    ///   Tick_ZeroTimer_NoOp                                             ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class AchievementUnlockNotificationControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static AchievementDefinitionSO CreateDef(string id, string displayName, int reward = 50)
        {
            var so = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            SetField(so, "_id",             id);
            SetField(so, "_displayName",    displayName);
            SetField(so, "_rewardCredits",  reward);
            SetField(so, "_targetCount",    1);
            return so;
        }

        private static AchievementCatalogSO CreateCatalog(params AchievementDefinitionSO[] defs)
        {
            var cat  = ScriptableObject.CreateInstance<AchievementCatalogSO>();
            var list = new List<AchievementDefinitionSO>(defs);
            SetField(cat, "_achievements", list);
            return cat;
        }

        private static PlayerAchievementsSO CreatePlayerAchievements()
        {
            return ScriptableObject.CreateInstance<PlayerAchievementsSO>();
        }

        private static AchievementUnlockNotificationController CreateController()
        {
            var go = new GameObject("AchievementUnlockNotif_Test");
            return go.AddComponent<AchievementUnlockNotificationController>();
        }

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_PlayerAchievementsIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PlayerAchievements);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_CatalogIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_DisplayTimer_IsZero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.0001f);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_DisplayDuration_IsThree()
        {
            var ctrl = CreateController();
            Assert.AreEqual(3f, ctrl.DisplayDuration, 0.0001f);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onAchievementUnlocked", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnDisable_HidesPanelAndResetsTimer()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Force timer to non-zero value then disable
            ctrl.ShowNotification(null);
            Assert.Greater(ctrl.DisplayTimer, 0f, "Timer should be set after ShowNotification.");

            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf, "OnDisable must hide the panel.");
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.0001f, "OnDisable must reset the timer.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnAchievementUnlocked_NullPlayerAchievements_DoesNotShow()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            SetField(ctrl, "_notificationPanel", panel);
            InvokePrivate(ctrl, "Awake");

            // No _playerAchievements set — fire delegate manually
            InvokePrivate(ctrl, "OnAchievementUnlocked");

            Assert.IsFalse(panel.activeSelf, "No panel shown when playerAchievements is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnAchievementUnlocked_NullCatalog_ShowsEmptyName()
        {
            var ctrl    = CreateController();
            var nameLbl = AddText(ctrl.gameObject, "name");
            var panel   = new GameObject("panel");

            var pa = CreatePlayerAchievements();
            pa.Unlock("ach1");  // LastUnlockedId = "ach1"

            SetField(ctrl, "_playerAchievements", pa);
            SetField(ctrl, "_notificationPanel",  panel);
            SetField(ctrl, "_achievementNameLabel", nameLbl);
            // No catalog assigned
            InvokePrivate(ctrl, "Awake");

            InvokePrivate(ctrl, "OnAchievementUnlocked");

            Assert.IsTrue(panel.activeSelf, "Panel should show even when catalog is null.");
            Assert.AreEqual(string.Empty, nameLbl.text,
                "Name label should be empty when definition not found.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void OnAchievementUnlocked_FoundDef_ShowsName()
        {
            var ctrl    = CreateController();
            var nameLbl = AddText(ctrl.gameObject, "name");

            var def     = CreateDef("ach1", "First Victory", 100);
            var catalog = CreateCatalog(def);
            var pa      = CreatePlayerAchievements();
            pa.Unlock("ach1");

            SetField(ctrl, "_playerAchievements",   pa);
            SetField(ctrl, "_catalog",              catalog);
            SetField(ctrl, "_achievementNameLabel", nameLbl);
            InvokePrivate(ctrl, "Awake");

            InvokePrivate(ctrl, "OnAchievementUnlocked");

            Assert.AreEqual("First Victory", nameLbl.text);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void OnAchievementUnlocked_FoundDef_ShowsReward()
        {
            var ctrl       = CreateController();
            var rewardLbl  = AddText(ctrl.gameObject, "reward");

            var def     = CreateDef("ach2", "Win Streak", 200);
            var catalog = CreateCatalog(def);
            var pa      = CreatePlayerAchievements();
            pa.Unlock("ach2");

            SetField(ctrl, "_playerAchievements", pa);
            SetField(ctrl, "_catalog",            catalog);
            SetField(ctrl, "_rewardLabel",        rewardLbl);
            InvokePrivate(ctrl, "Awake");

            InvokePrivate(ctrl, "OnAchievementUnlocked");

            Assert.AreEqual("+200 credits", rewardLbl.text);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void Tick_AdvancesAndHidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            SetField(ctrl, "_notificationPanel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.ShowNotification(null); // starts timer at 3f, activates panel
            Assert.IsTrue(panel.activeSelf, "Panel should be active after ShowNotification.");

            ctrl.Tick(2f);
            Assert.IsTrue(panel.activeSelf, "Panel should still be visible with 1s remaining.");

            ctrl.Tick(1.5f);
            Assert.IsFalse(panel.activeSelf, "Panel should be hidden after timer expires.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Tick_ZeroTimer_NoOp()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            // Timer is already 0 — Tick should not throw and should not change anything
            Assert.DoesNotThrow(() => ctrl.Tick(1f));
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.0001f);

            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
