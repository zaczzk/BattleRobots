using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T349: <see cref="ZoneControlVictoryCelebrationConfig"/> and
    /// <see cref="ZoneControlVictoryCelebrationController"/>.
    ///
    /// ZoneControlVictoryCelebrationTests (12):
    ///   Config_GetBannerText_FirstToCaptures_ReturnsExpectedString           ×1
    ///   Config_GetBannerText_MostZonesHeld_ReturnsExpectedString             ×1
    ///   Config_Duration_DefaultGreaterThanZero                               ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                        ×1
    ///   Controller_OnDisable_Unregisters_Channels                            ×1
    ///   Controller_HandleVictoryAchieved_NullConfig_NoThrow                  ×1
    ///   Controller_StartCelebration_NullConfig_NoThrow                       ×1
    ///   Controller_StartCelebration_SetsIsRunning_True                       ×1
    ///   Controller_StartCelebration_SetsTimerToDuration                      ×1
    ///   Controller_StopCelebration_SetsIsRunning_False                       ×1
    ///   Controller_HandleMatchStarted_StopsCelebration                       ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlVictoryCelebrationTests
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

        private static ZoneControlVictoryCelebrationConfig CreateConfig(
            float duration = 3f,
            string firstCaptures = "Victory FC!",
            string mostZones = "Victory MZ!")
        {
            var cfg = ScriptableObject.CreateInstance<ZoneControlVictoryCelebrationConfig>();
            SetField(cfg, "_celebrationDuration",    duration);
            SetField(cfg, "_firstToCapturesBanner",  firstCaptures);
            SetField(cfg, "_mostZonesHeldBanner",    mostZones);
            return cfg;
        }

        // ── Config Tests ──────────────────────────────────────────────────────

        [Test]
        public void Config_GetBannerText_FirstToCaptures_ReturnsExpectedString()
        {
            var cfg = CreateConfig(firstCaptures: "First Captures!");
            Assert.AreEqual("First Captures!", cfg.GetBannerText(ZoneControlVictoryType.FirstToCaptures),
                "GetBannerText must return the FirstToCaptures banner string.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetBannerText_MostZonesHeld_ReturnsExpectedString()
        {
            var cfg = CreateConfig(mostZones: "Most Zones!");
            Assert.AreEqual("Most Zones!", cfg.GetBannerText(ZoneControlVictoryType.MostZonesHeld),
                "GetBannerText must return the MostZonesHeld banner string.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_Duration_DefaultGreaterThanZero()
        {
            var cfg = ScriptableObject.CreateInstance<ZoneControlVictoryCelebrationConfig>();
            Assert.Greater(cfg.Duration, 0f,
                "Duration must be greater than 0 on a default instance.");
            Object.DestroyImmediate(cfg);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_VictoryCelebration_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlVictoryCelebrationController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_VictoryCelebration_OnDisable_Null");
            go.AddComponent<ZoneControlVictoryCelebrationController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_VictoryCelebration_Unregister");
            var ctrl = go.AddComponent<ZoneControlVictoryCelebrationController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onVictoryAchieved", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onVictoryAchieved must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleVictoryAchieved_NullConfig_NoThrow()
        {
            var go   = new GameObject("Test_VictoryCelebration_NullConfig");
            var ctrl = go.AddComponent<ZoneControlVictoryCelebrationController>();
            // _config is null by default
            Assert.DoesNotThrow(() => ctrl.HandleVictoryAchieved(),
                "HandleVictoryAchieved with null config must not throw.");
            Assert.IsFalse(ctrl.IsRunning,
                "IsRunning must remain false when config is null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_StartCelebration_NullConfig_NoThrow()
        {
            var go   = new GameObject("Test_VictoryCelebration_StartNull");
            var ctrl = go.AddComponent<ZoneControlVictoryCelebrationController>();
            Assert.DoesNotThrow(
                () => ctrl.StartCelebration(ZoneControlVictoryType.FirstToCaptures),
                "StartCelebration with null config must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_StartCelebration_SetsIsRunning_True()
        {
            var go   = new GameObject("Test_VictoryCelebration_IsRunning");
            var ctrl = go.AddComponent<ZoneControlVictoryCelebrationController>();
            var cfg  = CreateConfig(duration: 3f);
            SetField(ctrl, "_config", cfg);

            ctrl.StartCelebration(ZoneControlVictoryType.FirstToCaptures);

            Assert.IsTrue(ctrl.IsRunning,
                "IsRunning must be true after StartCelebration.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Controller_StartCelebration_SetsTimerToDuration()
        {
            var go   = new GameObject("Test_VictoryCelebration_Timer");
            var ctrl = go.AddComponent<ZoneControlVictoryCelebrationController>();
            var cfg  = CreateConfig(duration: 5f);
            SetField(ctrl, "_config", cfg);

            ctrl.StartCelebration(ZoneControlVictoryType.MostZonesHeld);

            Assert.AreEqual(5f, ctrl.Timer, 0.001f,
                "Timer must equal Duration after StartCelebration.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Controller_StopCelebration_SetsIsRunning_False()
        {
            var go   = new GameObject("Test_VictoryCelebration_Stop");
            var ctrl = go.AddComponent<ZoneControlVictoryCelebrationController>();
            var cfg  = CreateConfig();
            SetField(ctrl, "_config", cfg);

            ctrl.StartCelebration(ZoneControlVictoryType.FirstToCaptures);
            ctrl.StopCelebration();

            Assert.IsFalse(ctrl.IsRunning,
                "IsRunning must be false after StopCelebration.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Controller_HandleMatchStarted_StopsCelebration()
        {
            var go   = new GameObject("Test_VictoryCelebration_MatchStarted");
            var ctrl = go.AddComponent<ZoneControlVictoryCelebrationController>();
            var cfg  = CreateConfig();
            SetField(ctrl, "_config", cfg);

            ctrl.StartCelebration(ZoneControlVictoryType.FirstToCaptures);
            ctrl.HandleMatchStarted();

            Assert.IsFalse(ctrl.IsRunning,
                "IsRunning must be false after HandleMatchStarted.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }
    }
}
