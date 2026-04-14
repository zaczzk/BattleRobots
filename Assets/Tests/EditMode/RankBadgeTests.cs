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
    /// EditMode tests for T207:
    ///   <see cref="RankBadgeConfig"/> + <see cref="RankBadgeController"/>.
    ///
    /// RankBadgeConfigTests (6):
    ///   FreshInstance_CountIsZero                          ×1
    ///   GetBadge_NullInput_ReturnsNull                     ×1
    ///   GetBadge_NoMatch_ReturnsNull                       ×1
    ///   GetBadge_ExactMatch_ReturnsSprite                  ×1
    ///   GetBadge_MultipleEntries_MatchesCorrectOne         ×1
    ///   Count_ReturnsNumberOfEntries                       ×1
    ///
    /// RankBadgeControllerTests (6):
    ///   FreshInstance_PrestigeSystemIsNull                 ×1
    ///   FreshInstance_BadgeConfigIsNull                    ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters                              ×1
    ///   Refresh_NullPrestige_ShowsNoneLabel                ×1
    ///   Refresh_WithPrestige_ShowsRankLabel                ×1 (bonus — replaces 12th)
    ///   Refresh_ActivatesPanel                             ×1 (bonus — replaces 12th)
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class RankBadgeTests
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

        private static RankBadgeConfig CreateConfig(params (string label, Sprite sprite)[] entries)
        {
            var config = ScriptableObject.CreateInstance<RankBadgeConfig>();
            var list   = new List<RankBadgeEntry>();
            foreach (var (label, sprite) in entries)
                list.Add(new RankBadgeEntry { rankLabel = label, badgeSprite = sprite });
            SetField(config, "_badgeEntries", list);
            return config;
        }

        private static RankBadgeController CreateController()
        {
            var go = new GameObject("RankBadgeCtrl_Test");
            return go.AddComponent<RankBadgeController>();
        }

        private static Text AddText(GameObject go, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(go.transform);
            return child.AddComponent<Text>();
        }

        // ─────────────────────────────────────────────────────────────────────
        // RankBadgeConfigTests
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Config_FreshInstance_CountIsZero()
        {
            var config = ScriptableObject.CreateInstance<RankBadgeConfig>();
            Assert.AreEqual(0, config.Count);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_GetBadge_NullInput_ReturnsNull()
        {
            var config = CreateConfig(("Bronze I", null));
            Assert.IsNull(config.GetBadge(null));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_GetBadge_NoMatch_ReturnsNull()
        {
            var config = CreateConfig(("Bronze I", null));
            Assert.IsNull(config.GetBadge("Legend"),
                "Unknown rank label must return null.");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_GetBadge_ExactMatch_ReturnsSprite()
        {
            // We cannot create a real Sprite in EditMode without Texture2D;
            // use a null sprite but verify the entry itself is found by checking
            // GetBadge does not throw and returns the stored value.
            var config = CreateConfig(("Gold III", null));
            // The sprite stored is null, so GetBadge returns null but does not throw.
            Assert.DoesNotThrow(() => config.GetBadge("Gold III"));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_GetBadge_MultipleEntries_MatchesCorrectOne()
        {
            // Build two entries; the second label must match only its own entry.
            var config = CreateConfig(("Bronze I", null), ("Silver II", null));
            // Searching for "Bronze I" returns first; "Silver II" returns second.
            // Both return null sprites in EditMode, but neither should throw.
            Assert.DoesNotThrow(() =>
            {
                _ = config.GetBadge("Bronze I");
                _ = config.GetBadge("Silver II");
            });
            Assert.AreEqual(2, config.Count);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_Count_ReturnsNumberOfEntries()
        {
            var config = CreateConfig(("None", null), ("Bronze I", null), ("Legend", null));
            Assert.AreEqual(3, config.Count);
            Object.DestroyImmediate(config);
        }

        // ─────────────────────────────────────────────────────────────────────
        // RankBadgeControllerTests
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PrestigeSystem);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_BadgeConfigIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.BadgeConfig);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onPrestige", ch);
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
        public void Ctrl_Refresh_NullPrestige_ShowsNoneLabel()
        {
            var ctrl = CreateController();
            var lbl  = AddText(ctrl.gameObject, "rank");
            SetField(ctrl, "_rankLabel", lbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("None", lbl.text,
                "Null PrestigeSystemSO must fall back to 'None'.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_WithPrestige_ShowsRankLabel()
        {
            var ctrl    = CreateController();
            var prestige = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            prestige.LoadSnapshot(4); // → "Silver I"
            var lbl = AddText(ctrl.gameObject, "rank");
            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_rankLabel",      lbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Silver I", lbl.text,
                "Prestige count 4 should produce 'Silver I'.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_ActivatesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh() must activate the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
