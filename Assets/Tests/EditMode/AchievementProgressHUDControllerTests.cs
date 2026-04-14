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
    /// EditMode tests for T205:
    ///   <see cref="AchievementProgressHUDController"/>.
    ///
    /// AchievementProgressHUDControllerTests (14):
    ///   FreshInstance_CatalogIsNull                                    ×1
    ///   FreshInstance_PlayerAchievementsIsNull                         ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                              ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                             ×1
    ///   OnDisable_Unregisters                                          ×1
    ///   Refresh_NullCatalog_HidesPanel                                 ×1
    ///   Refresh_NullPlayerAchievements_HidesPanel                      ×1
    ///   Refresh_AllUnlocked_HidesPanelAndShowsAllComplete              ×1
    ///   Refresh_FirstIncomplete_ShowsPanel                             ×1
    ///   Refresh_FirstIncomplete_ShowsName                              ×1
    ///   Refresh_FirstIncomplete_SetsProgressLabel                      ×1
    ///   Refresh_FirstIncomplete_SetsProgressBar                        ×1
    ///   GetCurrentProgress_MatchWonTrigger_ReturnsTotalWins            ×1
    ///   GetCurrentProgress_WinStreakTrigger_ReturnsBestStreak          ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class AchievementProgressHUDControllerTests
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

        private static AchievementDefinitionSO CreateDef(
            string id, string name, AchievementTrigger trigger, int target = 5)
        {
            var so = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            SetField(so, "_id",           id);
            SetField(so, "_displayName",  name);
            SetField(so, "_triggerType",  trigger);
            SetField(so, "_targetCount",  target);
            return so;
        }

        private static AchievementCatalogSO CreateCatalog(params AchievementDefinitionSO[] defs)
        {
            var cat = ScriptableObject.CreateInstance<AchievementCatalogSO>();
            var list = new List<AchievementDefinitionSO>(defs);
            SetField(cat, "_achievements", list);
            return cat;
        }

        private static PlayerAchievementsSO CreatePlayerAchievements(
            int matchesPlayed = 0, int wins = 0, string[] unlockedIds = null)
        {
            var so = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            so.LoadSnapshot(matchesPlayed, wins, unlockedIds != null
                ? new List<string>(unlockedIds) : new List<string>());
            return so;
        }

        private static AchievementProgressHUDController CreateController()
        {
            var go = new GameObject("AchievementProgressHUD_Test");
            return go.AddComponent<AchievementProgressHUDController>();
        }

        private static Text AddText(GameObject go, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(go.transform);
            return child.AddComponent<Text>();
        }

        private static Slider AddSlider(GameObject go, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(go.transform);
            return child.AddComponent<Slider>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CatalogIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_PlayerAchievementsIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PlayerAchievements);
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
            SetField(ctrl, "_onMatchEnded", ch);
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
        public void Refresh_NullCatalog_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            SetField(ctrl, "_playerAchievements",
                CreatePlayerAchievements());
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Null catalog must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullPlayerAchievements_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            var def = CreateDef("a1", "First Win", AchievementTrigger.MatchWon, 1);
            SetField(ctrl, "_catalog", CreateCatalog(def));
            SetField(ctrl, "_panel",   panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Null playerAchievements must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void Refresh_AllUnlocked_HidesPanelAndShowsAllComplete()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            var nameLbl = AddText(ctrl.gameObject, "name");
            var def = CreateDef("a1", "First Win", AchievementTrigger.MatchWon, 1);
            var catalog = CreateCatalog(def);
            var pa  = CreatePlayerAchievements(unlockedIds: new[] { "a1" });

            SetField(ctrl, "_catalog",              catalog);
            SetField(ctrl, "_playerAchievements",   pa);
            SetField(ctrl, "_panel",                panel);
            SetField(ctrl, "_achievementNameLabel", nameLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "All unlocked must hide the panel.");
            Assert.AreEqual("All Complete!", nameLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void Refresh_FirstIncomplete_ShowsPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            var def     = CreateDef("a1", "First Win", AchievementTrigger.MatchWon, 1);
            var catalog = CreateCatalog(def);
            var pa      = CreatePlayerAchievements(); // nothing unlocked

            SetField(ctrl, "_catalog",            catalog);
            SetField(ctrl, "_playerAchievements", pa);
            SetField(ctrl, "_panel",              panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf, "Incomplete achievement must show the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void Refresh_FirstIncomplete_ShowsName()
        {
            var ctrl    = CreateController();
            var nameLbl = AddText(ctrl.gameObject, "name");
            var def     = CreateDef("a1", "Win Champion", AchievementTrigger.MatchWon, 10);
            var catalog = CreateCatalog(def);
            var pa      = CreatePlayerAchievements();

            SetField(ctrl, "_catalog",              catalog);
            SetField(ctrl, "_playerAchievements",   pa);
            SetField(ctrl, "_achievementNameLabel", nameLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Win Champion", nameLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void Refresh_FirstIncomplete_SetsProgressLabel()
        {
            var ctrl       = CreateController();
            var progressLbl = AddText(ctrl.gameObject, "progress");
            // 3 wins, target = 5
            var def     = CreateDef("a1", "Five Wins", AchievementTrigger.MatchWon, 5);
            var catalog = CreateCatalog(def);
            var pa      = CreatePlayerAchievements(matchesPlayed: 3, wins: 3);

            SetField(ctrl, "_catalog",            catalog);
            SetField(ctrl, "_playerAchievements", pa);
            SetField(ctrl, "_progressLabel",      progressLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("3 / 5", progressLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void Refresh_FirstIncomplete_SetsProgressBar()
        {
            var ctrl    = CreateController();
            var slider  = AddSlider(ctrl.gameObject, "bar");
            // 2 wins out of 4 → 0.5
            var def     = CreateDef("a1", "Four Wins", AchievementTrigger.MatchWon, 4);
            var catalog = CreateCatalog(def);
            var pa      = CreatePlayerAchievements(matchesPlayed: 2, wins: 2);

            SetField(ctrl, "_catalog",            catalog);
            SetField(ctrl, "_playerAchievements", pa);
            SetField(ctrl, "_progressBar",        slider);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(0.5f, slider.value, 0.001f,
                "2 / 4 wins should produce a progress bar value of 0.5.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void GetCurrentProgress_MatchWonTrigger_ReturnsTotalWins()
        {
            var ctrl = CreateController();
            var def  = CreateDef("a1", "Wins", AchievementTrigger.MatchWon, 10);
            var pa   = CreatePlayerAchievements(matchesPlayed: 7, wins: 5);

            SetField(ctrl, "_playerAchievements", pa);
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(5, ctrl.GetCurrentProgress(def));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(pa);
        }

        [Test]
        public void GetCurrentProgress_WinStreakTrigger_ReturnsBestStreak()
        {
            var ctrl   = CreateController();
            var def    = CreateDef("a2", "Streak5", AchievementTrigger.WinStreak, 5);
            var streak = ScriptableObject.CreateInstance<WinStreakSO>();
            streak.RecordWin();
            streak.RecordWin();
            streak.RecordWin(); // best = 3
            var pa = CreatePlayerAchievements();

            SetField(ctrl, "_winStreak",          streak);
            SetField(ctrl, "_playerAchievements", pa);
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(3, ctrl.GetCurrentProgress(def),
                "WinStreak trigger should return BestStreak.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(streak);
            Object.DestroyImmediate(pa);
        }
    }
}
