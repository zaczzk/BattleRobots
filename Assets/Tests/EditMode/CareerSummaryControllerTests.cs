using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T196 — <see cref="CareerSummaryController"/>.
    ///
    /// CareerSummaryControllerTests (14):
    ///   Ctrl_FreshInstance_MasteryIsNull                              ×1
    ///   Ctrl_FreshInstance_PrestigeSystemIsNull                       ×1
    ///   Ctrl_FreshInstance_CombinedBonusIsNull                        ×1
    ///   Ctrl_FreshInstance_HistoryIsNull                              ×1
    ///   Ctrl_OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   Ctrl_OnDisable_NullRefs_DoesNotThrow                          ×1
    ///   Ctrl_OnDisable_Unregisters_PrestigeChannel                    ×1
    ///   Ctrl_OnDisable_Unregisters_MasteryChannel                     ×1
    ///   Ctrl_Refresh_NullPrestigeSystem_RankShowsNone                 ×1
    ///   Ctrl_Refresh_NullMastery_MasteredCountZero                    ×1
    ///   Ctrl_Refresh_NullCombinedBonus_MultiplierShowsOne             ×1
    ///   Ctrl_Refresh_NullLabels_DoesNotThrow                          ×1
    ///   Ctrl_Refresh_NullPanel_DoesNotThrow                           ×1
    ///   Ctrl_Refresh_NullHistory_EventsShowsZero                      ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class CareerSummaryControllerTests
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

        private static CareerSummaryController CreateCtrl()
        {
            var go = new GameObject("CareerSummary_Test");
            return go.AddComponent<CareerSummaryController>();
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static Text CreateText()
        {
            var go = new GameObject("Text");
            return go.AddComponent<Text>();
        }

        // ── FreshInstance ─────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_MasteryIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.Mastery,
                "Fresh controller must have null Mastery.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.PrestigeSystem,
                "Fresh controller must have null PrestigeSystem.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_CombinedBonusIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.CombinedBonus,
                "Fresh controller must have null CombinedBonus.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_HistoryIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.History,
                "Fresh controller must have null History.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle null safety ─────────────────────────────────────────────

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_PrestigeChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onPrestige", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onPrestige.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_MasteryChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMasteryUnlocked", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onMasteryUnlocked.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── Refresh null-SO defaults ──────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullPrestigeSystem_RankShowsNone()
        {
            var ctrl  = CreateCtrl();
            var label = CreateText();
            SetField(ctrl, "_prestigeRankLabel", label);
            // _prestigeSystem is null

            ctrl.Refresh();

            Assert.AreEqual("None", label.text,
                "Prestige rank label must show 'None' when _prestigeSystem is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullMastery_MasteredCountZero()
        {
            var ctrl  = CreateCtrl();
            var label = CreateText();
            SetField(ctrl, "_masteredTypesLabel", label);
            // _mastery is null

            ctrl.Refresh();

            Assert.AreEqual("0/4", label.text,
                "Mastered types label must show '0/4' when _mastery is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullCombinedBonus_MultiplierShowsOne()
        {
            var ctrl  = CreateCtrl();
            var label = CreateText();
            SetField(ctrl, "_bonusMultiplierLabel", label);
            // _combinedBonus is null

            ctrl.Refresh();

            StringAssert.Contains("1.00", label.text,
                "Bonus multiplier label must contain '1.00' when _combinedBonus is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullHistory_EventsShowsZero()
        {
            var ctrl  = CreateCtrl();
            var label = CreateText();
            SetField(ctrl, "_prestigeEventsLabel", label);
            // _history is null

            ctrl.Refresh();

            Assert.AreEqual("0", label.text,
                "Prestige events label must show '0' when _history is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        // ── Refresh UI null safety ────────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullLabels_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            // All labels are null by default
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null text labels must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullPanel_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            // _panel is null by default
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _panel must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
