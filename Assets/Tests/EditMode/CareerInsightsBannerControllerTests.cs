using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T213:
    ///   <see cref="CareerInsightsBannerController"/>.
    ///
    /// CareerInsightsBannerControllerTests (12):
    ///   FreshInstance_WinStreakNull                                        ×1
    ///   FreshInstance_PrestigeSystemNull                                   ×1
    ///   FreshInstance_MasteryNull                                          ×1
    ///   DefaultDuration_Three                                              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters                                              ×1
    ///   OnDisable_HidesPanelAndResetsTimer                                 ×1
    ///   OnMatchEnded_NullAll_NoThrow                                       ×1
    ///   OnMatchEnded_NewBestStreak_ShowsBanner                             ×1
    ///   Tick_AdvancesTimer                                                 ×1
    ///   Tick_Expires_HidesPanel                                            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class CareerInsightsBannerControllerTests
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

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static void InvokeWithFloat(object target, string method, float arg)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found.");
            mi.Invoke(target, new object[] { arg });
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static CareerInsightsBannerController CreateController() =>
            new GameObject("CareerInsights_Test").AddComponent<CareerInsightsBannerController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_WinStreakNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.WinStreak);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_PrestigeSystemNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PrestigeSystem);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_MasteryNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Mastery);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void DefaultDuration_Three()
        {
            var ctrl = CreateController();
            Assert.AreEqual(3f, ctrl.DisplayDuration, 0.001f);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
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

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "After OnDisable only the manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnDisable_HidesPanelAndResetsTimer()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("banner");
            panel.SetActive(true);
            SetField(ctrl, "_bannerPanel",    panel);
            SetField(ctrl, "_displayTimer",   5f);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf, "OnDisable must hide the panel.");
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.001f, "OnDisable must reset the timer.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnMatchEnded_NullAll_NoThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            // All data SOs null — must not throw
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnMatchEnded"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnMatchEnded_NewBestStreak_ShowsBanner()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("banner");
            panel.SetActive(false);
            var lbl   = new GameObject("label").AddComponent<Text>();
            SetField(ctrl, "_bannerPanel",  panel);
            SetField(ctrl, "_insightLabel", lbl);

            // Wire a WinStreakSO with BestStreak = 3
            var streak = ScriptableObject.CreateInstance<WinStreakSO>();
            streak.RecordWin(); streak.RecordWin(); streak.RecordWin(); // BestStreak = 3
            SetField(ctrl, "_winStreak", streak);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");   // snapshots prevBestStreak = 3
            streak.RecordWin();                // BestStreak now = 4
            InvokePrivate(ctrl, "OnMatchEnded");

            Assert.IsTrue(panel.activeSelf, "Panel should activate on new best streak.");
            StringAssert.Contains("4", lbl.text, "Label should mention new best streak count.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(lbl.gameObject);
            Object.DestroyImmediate(streak);
        }

        [Test]
        public void Tick_AdvancesTimer()
        {
            var ctrl = CreateController();
            SetField(ctrl, "_displayTimer", 2f);
            InvokeWithFloat(ctrl, "Tick", 0.5f);
            Assert.AreEqual(1.5f, ctrl.DisplayTimer, 0.001f);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_Expires_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("banner");
            panel.SetActive(true);
            SetField(ctrl, "_bannerPanel",  panel);
            SetField(ctrl, "_displayTimer", 0.1f);
            InvokeWithFloat(ctrl, "Tick", 0.5f); // overshoots

            Assert.IsFalse(panel.activeSelf, "Panel must hide when timer expires.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
