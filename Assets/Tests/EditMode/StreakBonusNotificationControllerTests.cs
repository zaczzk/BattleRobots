using System;
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
    /// EditMode tests for T201:
    ///   <see cref="StreakBonusNotificationController"/>.
    ///
    /// StreakBonusNotificationControllerTests (12):
    ///   FreshInstance_WinStreakIsNull                            ×1
    ///   FreshInstance_MilestoneConfigIsNull                      ×1
    ///   FreshInstance_DefaultDurationIsTwo                       ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                        ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                       ×1
    ///   OnDisable_HidesPanelResetsTimerAndUnregisters            ×1
    ///   OnStreakChanged_NullWinStreak_NoNotification             ×1
    ///   OnStreakChanged_NullMilestoneConfig_NoNotification       ×1
    ///   OnStreakChanged_NoMilestoneAtStreak_NoNotification       ×1
    ///   OnStreakChanged_MilestoneReached_ShowsPanel              ×1
    ///   OnStreakChanged_MilestoneReached_LabelContainsStreak     ×1
    ///   Tick_ExpiresAndHidesPanel                               ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class StreakBonusNotificationControllerTests
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

        private static WinStreakSO CreateWinStreak(int wins = 0)
        {
            var so = ScriptableObject.CreateInstance<WinStreakSO>();
            for (int i = 0; i < wins; i++) so.RecordWin();
            return so;
        }

        /// <summary>
        /// Creates a WinStreakMilestoneSO with a single milestone at <paramref name="target"/>.
        /// Uses reflection to inject the milestone list because the SO has no public mutator.
        /// </summary>
        private static WinStreakMilestoneSO CreateMilestoneConfig(int target)
        {
            var so = ScriptableObject.CreateInstance<WinStreakMilestoneSO>();

            // Inject one milestone entry via the private backing list.
            FieldInfo fi = typeof(WinStreakMilestoneSO)
                .GetField("_milestones",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "_milestones field not found on WinStreakMilestoneSO.");

            var list = (List<WinStreakMilestoneEntry>)fi.GetValue(so);
            list.Add(new WinStreakMilestoneEntry
            {
                streakTarget   = target,
                rewardCredits  = 50,
                displayName    = $"{target}-Win Streak!"
            });

            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static StreakBonusNotificationController CreateController()
        {
            var go = new GameObject("StreakBonusCtrl_Test");
            return go.AddComponent<StreakBonusNotificationController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_WinStreakIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.WinStreak);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_MilestoneConfigIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.MilestoneConfig);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_DefaultDurationIsTwo()
        {
            var ctrl = CreateController();
            Assert.AreEqual(2f, ctrl.DisplayDuration, 0.001f);
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
        public void Ctrl_OnDisable_HidesPanelResetsTimerAndUnregisters()
        {
            var ctrl  = CreateController();
            var ch    = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_onStreakChanged",    ch);
            SetField(ctrl, "_notificationPanel", panel);
            SetField(ctrl, "_displayTimer",      3f);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.IsFalse(panel.activeSelf, "OnDisable must hide the notification panel.");
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.001f, "OnDisable must reset the timer.");
            Assert.AreEqual(1, callCount,
                "After OnDisable only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnStreakChanged_NullWinStreak_NoNotification()
        {
            var ctrl      = CreateController();
            var ch        = CreateEvent();
            var milestone = CreateMilestoneConfig(3);
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_onStreakChanged",    ch);
            SetField(ctrl, "_milestoneConfig",   milestone);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.IsFalse(panel.activeSelf,
                "Null WinStreakSO must not show the notification panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(milestone);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnStreakChanged_NullMilestoneConfig_NoNotification()
        {
            var ctrl   = CreateController();
            var ch     = CreateEvent();
            var streak = CreateWinStreak(wins: 2);
            var panel  = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_winStreak",         streak);
            SetField(ctrl, "_onStreakChanged",    ch);
            // _milestoneConfig intentionally null
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            streak.RecordWin(); // streak = 3
            ch.Raise();

            Assert.IsFalse(panel.activeSelf,
                "Null WinStreakMilestoneSO must not show the notification panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(streak);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnStreakChanged_NoMilestoneAtStreak_NoNotification()
        {
            var ctrl      = CreateController();
            var ch        = CreateEvent();
            var streak    = CreateWinStreak(wins: 1);
            var milestone = CreateMilestoneConfig(5); // milestone at 5, not 2
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_winStreak",         streak);
            SetField(ctrl, "_onStreakChanged",    ch);
            SetField(ctrl, "_milestoneConfig",   milestone);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            streak.RecordWin(); // streak = 2, no milestone at 2
            ch.Raise();

            Assert.IsFalse(panel.activeSelf,
                "No milestone at current streak must not show the notification panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(streak);
            Object.DestroyImmediate(milestone);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnStreakChanged_MilestoneReached_ShowsPanel()
        {
            var ctrl      = CreateController();
            var ch        = CreateEvent();
            var streak    = CreateWinStreak(wins: 2);
            var milestone = CreateMilestoneConfig(3); // milestone at 3
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_winStreak",         streak);
            SetField(ctrl, "_onStreakChanged",    ch);
            SetField(ctrl, "_milestoneConfig",   milestone);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            streak.RecordWin(); // streak = 3 → milestone hit
            ch.Raise();

            Assert.IsTrue(panel.activeSelf,
                "Reaching a configured milestone must activate the notification panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(streak);
            Object.DestroyImmediate(milestone);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnStreakChanged_MilestoneReached_LabelContainsStreak()
        {
            var ctrl      = CreateController();
            var ch        = CreateEvent();
            var streak    = CreateWinStreak(wins: 2);
            var milestone = CreateMilestoneConfig(3);
            var labelGO   = new GameObject("Label");
            var label     = labelGO.AddComponent<Text>();

            SetField(ctrl, "_winStreak",          streak);
            SetField(ctrl, "_onStreakChanged",     ch);
            SetField(ctrl, "_milestoneConfig",    milestone);
            SetField(ctrl, "_notificationLabel",  label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            streak.RecordWin(); // streak = 3
            ch.Raise();

            StringAssert.Contains("3", label.text,
                "Notification label must contain the streak count.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(streak);
            Object.DestroyImmediate(milestone);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_Tick_ExpiresAndHidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_notificationPanel", panel);
            SetField(ctrl, "_displayTimer",      0.5f);

            InvokePrivate(ctrl, "Awake");
            ctrl.Tick(0.6f); // exceeds timer

            Assert.IsFalse(panel.activeSelf,
                "Tick past the display duration must hide the notification panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
