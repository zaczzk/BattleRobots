using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T198:
    ///   <see cref="WinStreakDisplayController"/>.
    ///
    /// WinStreakDisplayControllerTests (12):
    ///   FreshInstance_WinStreakIsNull                   ×1
    ///   FreshInstance_DefaultThreshold_IsThree         ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow              ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow             ×1
    ///   OnDisable_Unregisters                          ×1
    ///   Refresh_NullWinStreak_ShowsDashes              ×1
    ///   Refresh_NullWinStreak_HidesBadge               ×1
    ///   Refresh_WithStreak_CurrentStreakLabel          ×1
    ///   Refresh_WithStreak_BestStreakLabel             ×1
    ///   Refresh_BelowThreshold_BadgeHidden             ×1
    ///   Refresh_AtThreshold_BadgeShown                 ×1
    ///   OnStreakChanged_TriggersRefresh                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class WinStreakDisplayControllerTests
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

        private static WinStreakSO CreateWinStreak() =>
            ScriptableObject.CreateInstance<WinStreakSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static WinStreakDisplayController CreateController()
        {
            var go = new GameObject("WinStreakDisplayCtrl_Test");
            return go.AddComponent<WinStreakDisplayController>();
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
        public void Ctrl_FreshInstance_DefaultThresholdIsThree()
        {
            var ctrl = CreateController();
            Assert.AreEqual(3, ctrl.NotableStreakThreshold);
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
            SetField(ctrl, "_onStreakChanged", ch);
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
        public void Ctrl_Refresh_NullWinStreak_ShowsDashes()
        {
            var ctrl    = CreateController();
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();
            SetField(ctrl, "_currentStreakText", label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("\u2014", label.text,
                "Null WinStreakSO must show an em-dash in the current streak label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Ctrl_Refresh_NullWinStreak_HidesBadge()
        {
            var ctrl  = CreateController();
            var badge = new GameObject("Badge");
            badge.SetActive(true);
            SetField(ctrl, "_streakBadge", badge);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(badge.activeSelf,
                "Null WinStreakSO must hide the streak badge.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badge);
        }

        [Test]
        public void Ctrl_Refresh_WithStreak_CurrentStreakLabel()
        {
            var ctrl     = CreateController();
            var streak   = CreateWinStreak();
            var labelGO  = new GameObject("Label");
            var label    = labelGO.AddComponent<Text>();

            streak.RecordWin();
            streak.RecordWin();
            SetField(ctrl, "_winStreak",         streak);
            SetField(ctrl, "_currentStreakText", label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual("Streak: 2", label.text,
                "Current streak label must show 'Streak: N'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(streak);
        }

        [Test]
        public void Ctrl_Refresh_WithStreak_BestStreakLabel()
        {
            var ctrl     = CreateController();
            var streak   = CreateWinStreak();
            var labelGO  = new GameObject("Label");
            var label    = labelGO.AddComponent<Text>();

            streak.RecordWin();
            streak.RecordWin();
            streak.RecordWin();
            streak.RecordLoss(); // current resets but best stays at 3
            SetField(ctrl, "_winStreak",    streak);
            SetField(ctrl, "_bestStreakText", label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual("Best: 3", label.text,
                "Best streak label must show 'Best: N'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(streak);
        }

        [Test]
        public void Ctrl_Refresh_BelowThreshold_BadgeHidden()
        {
            var ctrl   = CreateController();
            var streak = CreateWinStreak();
            var badge  = new GameObject("Badge");
            badge.SetActive(true);

            streak.RecordWin(); // streak = 1, threshold = 3
            SetField(ctrl, "_winStreak",   streak);
            SetField(ctrl, "_streakBadge", badge);
            SetField(ctrl, "_notableStreakThreshold", 3);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(badge.activeSelf,
                "Badge must be hidden when CurrentStreak is below the threshold.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(streak);
        }

        [Test]
        public void Ctrl_Refresh_AtThreshold_BadgeShown()
        {
            var ctrl   = CreateController();
            var streak = CreateWinStreak();
            var badge  = new GameObject("Badge");
            badge.SetActive(false);

            streak.RecordWin();
            streak.RecordWin();
            streak.RecordWin(); // streak = 3, threshold = 3
            SetField(ctrl, "_winStreak",   streak);
            SetField(ctrl, "_streakBadge", badge);
            SetField(ctrl, "_notableStreakThreshold", 3);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(badge.activeSelf,
                "Badge must be shown when CurrentStreak equals the threshold.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(streak);
        }

        [Test]
        public void Ctrl_OnStreakChanged_TriggersRefresh()
        {
            var ctrl     = CreateController();
            var streak   = CreateWinStreak();
            var ch       = CreateEvent();
            var labelGO  = new GameObject("Label");
            var label    = labelGO.AddComponent<Text>();

            SetField(ctrl, "_winStreak",         streak);
            SetField(ctrl, "_onStreakChanged",    ch);
            SetField(ctrl, "_currentStreakText", label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable"); // Refresh called: streak = 0

            streak.RecordWin(); // internal; but event fires via WinStreakSO channel
            // Simulate the event raise directly since the SO's internal channel may differ.
            ch.Raise();

            Assert.AreEqual("Streak: 1", label.text,
                "OnStreakChanged must trigger Refresh(), updating the current streak label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(streak);
            Object.DestroyImmediate(ch);
        }
    }
}
