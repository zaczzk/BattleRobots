using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T209:
    ///   <see cref="PrestigeProgressHUDController"/>.
    ///
    /// PrestigeProgressHUDControllerTests (14):
    ///   FreshInstance_ProgressionIsNull                                 ×1
    ///   FreshInstance_PrestigeSystemIsNull                              ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                               ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                              ×1
    ///   OnDisable_Unregisters_LevelUp                                   ×1
    ///   OnDisable_Unregisters_Prestige                                  ×1
    ///   Refresh_NullProgression_HidesPanel                              ×1
    ///   Refresh_NullPrestigeSystem_HidesPanel                           ×1
    ///   Refresh_BothData_ShowsPanel                                     ×1
    ///   Refresh_LevelLabel_ShowsCurrentAndMax                           ×1
    ///   Refresh_XpProgressBar_SetsFraction                              ×1
    ///   Refresh_StatusLabel_NormalLevel_Empty                           ×1
    ///   Refresh_StatusLabel_MaxLevel_NotMaxPrestige_PrestigeReady       ×1
    ///   Refresh_StatusLabel_MaxPrestige_Legend                          ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class PrestigeProgressHUDControllerTests
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

        private static PlayerProgressionSO CreateProgression(int totalXP = 0, int level = 1, int maxLevel = 10)
        {
            var so = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            SetField(so, "_maxLevel", maxLevel);
            so.LoadSnapshot(totalXP, level);
            return so;
        }

        private static PrestigeSystemSO CreatePrestige(int count = 0, int maxRank = 10)
        {
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            SetField(so, "_maxPrestigeRank", maxRank);
            so.LoadSnapshot(count);
            return so;
        }

        private static PrestigeProgressHUDController CreateController()
        {
            var go = new GameObject("PrestigeProgressHUD_Test");
            return go.AddComponent<PrestigeProgressHUDController>();
        }

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        private static Slider AddSlider(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Slider>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ProgressionIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Progression);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PrestigeSystem);
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
        public void OnDisable_Unregisters_LevelUp()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onLevelUp", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "Only the manually registered callback should fire after OnDisable.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnDisable_Unregisters_Prestige()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onPrestige", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "Prestige channel must be unregistered on OnDisable.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullProgression_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel",          panel);
            SetField(ctrl, "_prestigeSystem", CreatePrestige());
            // _progression left null
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Null progression must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullPrestigeSystem_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel",       panel);
            SetField(ctrl, "_progression", CreateProgression());
            // _prestigeSystem left null
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Null prestige system must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_BothData_ShowsPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            SetField(ctrl, "_panel",          panel);
            SetField(ctrl, "_progression",    CreateProgression(level: 5));
            SetField(ctrl, "_prestigeSystem", CreatePrestige());
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf, "Both data present — panel must be shown.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_LevelLabel_ShowsCurrentAndMax()
        {
            var ctrl     = CreateController();
            var levelLbl = AddText(ctrl.gameObject, "level");
            SetField(ctrl, "_levelLabel",     levelLbl);
            SetField(ctrl, "_progression",    CreateProgression(level: 3, maxLevel: 10));
            SetField(ctrl, "_prestigeSystem", CreatePrestige());
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Level 3 / 10", levelLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_XpProgressBar_SetsFraction()
        {
            var ctrl   = CreateController();
            var slider = AddSlider(ctrl.gameObject, "xp");

            // At level 1, 0 XP → XpProgressFraction = 0f (no progress toward level 2)
            var prog = CreateProgression(totalXP: 0, level: 1, maxLevel: 10);
            SetField(ctrl, "_xpProgressBar",  slider);
            SetField(ctrl, "_progression",    prog);
            SetField(ctrl, "_prestigeSystem", CreatePrestige());
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            float expected = prog.XpProgressFraction;
            Assert.AreEqual(expected, slider.value, 0.001f,
                "Slider must reflect XpProgressFraction.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(prog);
        }

        [Test]
        public void Refresh_StatusLabel_NormalLevel_Empty()
        {
            var ctrl      = CreateController();
            var statusLbl = AddText(ctrl.gameObject, "status");
            SetField(ctrl, "_statusLabel",    statusLbl);
            SetField(ctrl, "_progression",    CreateProgression(level: 5, maxLevel: 10));
            SetField(ctrl, "_prestigeSystem", CreatePrestige(count: 0, maxRank: 10));
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(string.Empty, statusLbl.text,
                "Status should be empty when not at max level and not at max prestige.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_StatusLabel_MaxLevel_NotMaxPrestige_PrestigeReady()
        {
            var ctrl      = CreateController();
            var statusLbl = AddText(ctrl.gameObject, "status");

            // Level = max (10), prestige count = 0 (not max)
            SetField(ctrl, "_statusLabel",    statusLbl);
            SetField(ctrl, "_progression",    CreateProgression(level: 10, maxLevel: 10));
            SetField(ctrl, "_prestigeSystem", CreatePrestige(count: 0, maxRank: 10));
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Prestige Ready!", statusLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_StatusLabel_MaxPrestige_Legend()
        {
            var ctrl      = CreateController();
            var statusLbl = AddText(ctrl.gameObject, "status");

            // prestige count = max (10)
            SetField(ctrl, "_statusLabel",    statusLbl);
            SetField(ctrl, "_progression",    CreateProgression(level: 10, maxLevel: 10));
            SetField(ctrl, "_prestigeSystem", CreatePrestige(count: 10, maxRank: 10));
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Legend", statusLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
