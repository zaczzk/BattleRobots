using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T187:
    ///   <see cref="PrestigeLeaderboardController"/>.
    ///
    /// PrestigeLeaderboardControllerTests (14):
    ///   FreshInstance_LeaderboardIsNull ×1
    ///   FreshInstance_PrestigeSystemIsNull ×1
    ///   OnEnable_NullRefs_DoesNotThrow ×1
    ///   OnDisable_NullRefs_DoesNotThrow ×1
    ///   OnDisable_Unregisters_LeaderboardChannel ×1
    ///   OnDisable_Unregisters_PrestigeChannel ×1
    ///   Refresh_NullLeaderboard_ShowsEmptyLabel ×1
    ///   Refresh_NullPrestige_RankLabelShowsDash ×1
    ///   Refresh_IsMaxPrestige_LegendBadgeActive ×1
    ///   Refresh_NotMaxPrestige_LegendBadgeInactive ×1
    ///   Refresh_HasEntries_BuildsRows ×1
    ///   Refresh_PrestigeRankLabel_ShowsCorrectLabel ×1
    ///   Refresh_NullContainer_DoesNotThrow ×1
    ///   Refresh_NullAllUI_DoesNotThrow ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class PrestigeLeaderboardControllerTests
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

        private static PrestigeSystemSO CreatePrestige(int count = 0)
        {
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(count);
            return so;
        }

        private static MatchLeaderboardSO CreateLeaderboard()
        {
            return ScriptableObject.CreateInstance<MatchLeaderboardSO>();
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static PrestigeLeaderboardController CreateController()
        {
            var go = new GameObject("PrestigeLeaderboardCtrl_Test");
            return go.AddComponent<PrestigeLeaderboardController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_LeaderboardIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Leaderboard);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PrestigeSystem);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_LeaderboardChannel()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onLeaderboardUpdated", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "OnDisable must unregister from _onLeaderboardUpdated.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_PrestigeChannel()
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
                "OnDisable must unregister from _onPrestige.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_Refresh_NullLeaderboard_ShowsEmptyLabel()
        {
            var ctrl  = CreateController();
            var empty = new GameObject("EmptyLabel");
            empty.SetActive(false);
            SetField(ctrl, "_emptyLabel", empty);
            // _leaderboard is null

            ctrl.Refresh();

            Assert.IsTrue(empty.activeSelf,
                "Refresh with null leaderboard must activate the empty label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(empty);
        }

        [Test]
        public void Ctrl_Refresh_NullPrestige_RankLabelShowsDash()
        {
            var ctrl    = CreateController();
            var labelGO = new GameObject("PrestigeRankLabel");
            var label   = labelGO.AddComponent<Text>();
            SetField(ctrl, "_prestigeRankLabel", label);
            // _prestigeSystem is null

            ctrl.Refresh();

            Assert.AreEqual("—", label.text,
                "Refresh with null prestige system must show '—' in the rank label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Ctrl_Refresh_IsMaxPrestige_LegendBadgeActive()
        {
            var ctrl    = CreateController();
            var prestige = CreatePrestige(10); // 10 = max = Legend
            var badge   = new GameObject("LegendBadge");
            badge.SetActive(false);

            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_legendBadge",    badge);

            ctrl.Refresh();

            Assert.IsTrue(badge.activeSelf,
                "Refresh must activate the legend badge when IsMaxPrestige is true.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_NotMaxPrestige_LegendBadgeInactive()
        {
            var ctrl    = CreateController();
            var prestige = CreatePrestige(3); // not max
            var badge   = new GameObject("LegendBadge");
            badge.SetActive(true);

            SetField(ctrl, "_prestigeSystem", prestige);
            SetField(ctrl, "_legendBadge",    badge);

            ctrl.Refresh();

            Assert.IsFalse(badge.activeSelf,
                "Refresh must hide the legend badge when IsMaxPrestige is false.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_HasEntries_BuildsRows()
        {
            var ctrl       = CreateController();
            var leaderboard = CreateLeaderboard();

            // Submit a result to populate the leaderboard.
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(true, 30f, 100, 100, 100f, 20f, 0);
            leaderboard.Submit(result, "TestBot", 0);

            // Create a row prefab with 4 Text children.
            var prefab = new GameObject("RowPrefab");
            for (int i = 0; i < 4; i++)
            {
                var child = new GameObject($"Text{i}");
                child.transform.SetParent(prefab.transform);
                child.AddComponent<Text>();
            }

            var container = new GameObject("Container");

            SetField(ctrl, "_leaderboard",   leaderboard);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);

            ctrl.Refresh();

            Assert.AreEqual(1, container.transform.childCount,
                "Refresh must instantiate one row per leaderboard entry.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(leaderboard);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Ctrl_Refresh_PrestigeRankLabel_ShowsCorrectLabel()
        {
            var ctrl     = CreateController();
            var prestige = CreatePrestige(5); // Silver II
            var labelGO  = new GameObject("PrestigeRankLabel");
            var label    = labelGO.AddComponent<Text>();

            SetField(ctrl, "_prestigeSystem",    prestige);
            SetField(ctrl, "_prestigeRankLabel", label);

            ctrl.Refresh();

            Assert.AreEqual("Silver II", label.text,
                "Refresh must show the correct prestige rank label from PrestigeSystemSO.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Ctrl_Refresh_NullContainer_DoesNotThrow()
        {
            var ctrl       = CreateController();
            var leaderboard = CreateLeaderboard();
            SetField(ctrl, "_leaderboard",   leaderboard);
            // _listContainer is null

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _listContainer is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(leaderboard);
        }

        [Test]
        public void Ctrl_Refresh_NullAllUI_DoesNotThrow()
        {
            var ctrl = CreateController();
            // All UI refs null, no data refs either
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when all refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
