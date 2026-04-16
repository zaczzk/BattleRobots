using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T315: <see cref="ZoneControlBotDifficultyProfileSO"/> and
    /// <see cref="ZoneControlBotDifficultyController"/>.
    ///
    /// ZoneControlBotDifficultyTests (12):
    ///   SO_FreshInstance_BaseCaptureInterval_Default                              ×1
    ///   SO_GetCaptureInterval_Wave0_ReturnsBase                                   ×1
    ///   SO_GetCaptureInterval_ReducesPerWave                                      ×1
    ///   SO_GetCaptureInterval_ClampsToMinimum                                     ×1
    ///   SO_GetCaptureInterval_NegativeWave_TreatedAsZero                          ×1
    ///   Controller_FreshInstance_ProfileSO_Null                                   ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandleMatchStarted_DisarmsTimer                                ×1
    ///   Controller_HandleMatchEnded_StopsRunning                                  ×1
    ///   Controller_HandleWaveStarted_ArmsTimer                                    ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlBotDifficultyTests
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

        private static ZoneControlBotDifficultyProfileSO CreateProfileSO() =>
            ScriptableObject.CreateInstance<ZoneControlBotDifficultyProfileSO>();

        private static ZoneControlBotDifficultyController CreateController() =>
            new GameObject("BotDiffCtrl_Test")
                .AddComponent<ZoneControlBotDifficultyController>();

        // ── Profile SO Tests ──────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_BaseCaptureInterval_Default()
        {
            var so = CreateProfileSO();
            Assert.AreEqual(10f, so.BaseCaptureInterval, 0.001f,
                "BaseCaptureInterval must default to 10f.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetCaptureInterval_Wave0_ReturnsBase()
        {
            var so = CreateProfileSO();
            // Default: base=10, reduction=0.5, minimum=2
            float interval = so.GetCaptureInterval(0);
            Assert.AreEqual(10f, interval, 0.001f,
                "GetCaptureInterval(0) must return BaseCaptureInterval when no reduction applied.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetCaptureInterval_ReducesPerWave()
        {
            var so = CreateProfileSO();
            // base=10, reduction=0.5 → wave 4 → 10 - 4*0.5 = 8
            float interval = so.GetCaptureInterval(4);
            Assert.AreEqual(8f, interval, 0.001f,
                "GetCaptureInterval(4) must equal base - wave * reduction.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetCaptureInterval_ClampsToMinimum()
        {
            var so = CreateProfileSO();
            // base=10, reduction=0.5, minimum=2 → wave 100 → clamped to 2
            float interval = so.GetCaptureInterval(100);
            Assert.AreEqual(2f, interval, 0.001f,
                "GetCaptureInterval must never return below MinimumInterval.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetCaptureInterval_NegativeWave_TreatedAsZero()
        {
            var so = CreateProfileSO();
            float interval = so.GetCaptureInterval(-5);
            Assert.AreEqual(so.GetCaptureInterval(0), interval, 0.001f,
                "Negative wave must be treated as 0.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ProfileSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ProfileSO,
                "ProfileSO must be null on a freshly added controller.");
            UnityEngine.Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlBotDifficultyController>(),
                "Adding controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlBotDifficultyController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlBotDifficultyController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onWaveStarted", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onWaveStarted must be unregistered after OnDisable.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchStarted_DisarmsTimer()
        {
            var go   = new GameObject("Test_MatchStarted");
            var ctrl = go.AddComponent<ZoneControlBotDifficultyController>();

            // Manually arm the timer.
            SetField(ctrl, "_isRunning", true);
            SetField(ctrl, "_elapsed",   3f);

            ctrl.HandleMatchStarted();

            Assert.IsFalse(ctrl.IsRunning,
                "IsRunning must be false after HandleMatchStarted.");
            Assert.AreEqual(0f, ctrl.Elapsed, 0.001f,
                "Elapsed must be reset to 0 after HandleMatchStarted.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_HandleMatchEnded_StopsRunning()
        {
            var go   = new GameObject("Test_MatchEnded");
            var ctrl = go.AddComponent<ZoneControlBotDifficultyController>();

            SetField(ctrl, "_isRunning", true);

            ctrl.HandleMatchEnded();

            Assert.IsFalse(ctrl.IsRunning,
                "IsRunning must be false after HandleMatchEnded.");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_HandleWaveStarted_ArmsTimer()
        {
            var go      = new GameObject("Test_WaveStarted");
            var ctrl    = go.AddComponent<ZoneControlBotDifficultyController>();
            var profile = CreateProfileSO();

            SetField(ctrl, "_profileSO", profile);
            // No WaveManagerSO → CurrentWave assumed 0.

            ctrl.HandleWaveStarted();

            Assert.IsTrue(ctrl.IsRunning,
                "IsRunning must be true after HandleWaveStarted with a valid profile.");
            Assert.AreEqual(profile.GetCaptureInterval(0), ctrl.CurrentInterval, 0.001f,
                "CurrentInterval must equal profile.GetCaptureInterval(0) when no WaveManagerSO is wired.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}
