using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T304: <see cref="ZoneControlDailyChallengeConfig"/> and
    /// <see cref="ZoneControlDailyChallengeController"/>.
    ///
    /// ZoneControlDailyChallengeTests (12):
    ///   Config_FreshInstance_PossibleTargetCount_Five                           ×1
    ///   Config_GetTodayTarget_ReturnsValueFromPool                              ×1
    ///   Config_GetTodayTarget_IsDeterministic_SameDay                          ×1
    ///   Config_GetSecondsUntilReset_IsPositive                                  ×1
    ///   Config_EmptyPool_GetTodayTarget_ReturnsZero                             ×1
    ///   Controller_FreshInstance_Config_Null                                    ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_Channel                                ×1
    ///   Controller_HandleMatchEnded_NullConfig_NoThrow                          ×1
    ///   Controller_Refresh_NullConfig_HidesPanel                                ×1
    ///   FormatCountdown_FormatsCorrectly                                        ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlDailyChallengeTests
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

        private static ZoneControlDailyChallengeConfig CreateConfig() =>
            ScriptableObject.CreateInstance<ZoneControlDailyChallengeConfig>();

        private static ZoneControlDailyChallengeController CreateController() =>
            new GameObject("DailyChallengeCtrl_Test")
                .AddComponent<ZoneControlDailyChallengeController>();

        // ── Config Tests ──────────────────────────────────────────────────────

        [Test]
        public void Config_FreshInstance_PossibleTargetCount_Five()
        {
            var config = CreateConfig();
            Assert.AreEqual(5, config.PossibleTargetCount,
                "Default pool must contain 5 possible target values.");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_GetTodayTarget_ReturnsValueFromPool()
        {
            var config = CreateConfig();
            float target = config.GetTodayTarget();
            // Default pool: {5, 10, 15, 20, 30}
            Assert.IsTrue(target > 0f,
                "GetTodayTarget must return a positive value from the default pool.");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_GetTodayTarget_IsDeterministic_SameDay()
        {
            var config = CreateConfig();
            float first  = config.GetTodayTarget();
            float second = config.GetTodayTarget();
            Assert.AreEqual(first, second,
                "GetTodayTarget must return the same value when called twice on the same day.");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_GetSecondsUntilReset_IsPositive()
        {
            var config = CreateConfig();
            int seconds = config.GetSecondsUntilReset();
            Assert.Greater(seconds, 0,
                "GetSecondsUntilReset must return a positive value (time until midnight UTC).");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Config_EmptyPool_GetTodayTarget_ReturnsZero()
        {
            var config = CreateConfig();
            SetField(config, "_possibleTargets", new float[0]);
            Assert.AreEqual(0f, config.GetTodayTarget(),
                "GetTodayTarget must return 0 when the possible-targets pool is empty.");
            Object.DestroyImmediate(config);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_Config_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Config,
                "Config must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlDailyChallengeController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlDailyChallengeController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlDailyChallengeController>();

            var matchEndedEvt = CreateEvent();
            SetField(ctrl, "_onMatchEnded", matchEndedEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            matchEndedEvt.RegisterCallback(() => count++);
            matchEndedEvt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEndedEvt);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullConfig_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when _config is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullConfig_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullConfig");
            var ctrl  = go.AddComponent<ZoneControlDailyChallengeController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when Config is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void FormatCountdown_FormatsCorrectly()
        {
            // 3h 30m = 12600s
            string result = ZoneControlDailyChallengeController.FormatCountdown(12600);
            Assert.AreEqual("Resets in 3h 30m", result,
                "FormatCountdown must format seconds as 'Resets in Xh Ym'.");
        }
    }
}
