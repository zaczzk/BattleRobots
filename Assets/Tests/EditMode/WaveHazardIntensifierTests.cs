using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T250: <see cref="WaveHazardIntensifierSO"/> and
    /// <see cref="WaveHazardIntensifierController"/>.
    ///
    /// WaveHazardIntensifierTests (14):
    ///   SO_FreshInstance_BaseInterval_Default                        ×1
    ///   SO_FreshInstance_MinimumInterval_Default                     ×1
    ///   SO_GetIntervalForWave_Wave0_ReturnsBaseInterval              ×1
    ///   SO_GetIntervalForWave_ReducesPerWave                         ×1
    ///   SO_GetIntervalForWave_ClampsToMinimum                        ×1
    ///   Controller_FreshInstance_IntensifierSO_Null                  ×1
    ///   Controller_FreshInstance_WaveManager_Null                    ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_Unregisters_WaveStarted                 ×1
    ///   Controller_HandleWaveStarted_NullIntensifier_NoThrow         ×1
    ///   Controller_HandleWaveStarted_NullWaveManager_NoThrow         ×1
    ///   Controller_HandleWaveStarted_SetsInterval_OnToggleControllers ×1
    ///   Controller_HandleWaveStarted_NullToggleEntry_Skipped         ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class WaveHazardIntensifierTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static WaveHazardIntensifierSO CreateIntensifierSO() =>
            ScriptableObject.CreateInstance<WaveHazardIntensifierSO>();

        private static WaveManagerSO CreateWaveManagerSO() =>
            ScriptableObject.CreateInstance<WaveManagerSO>();

        private static WaveConfigSO CreateWaveConfigSO() =>
            ScriptableObject.CreateInstance<WaveConfigSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static WaveHazardIntensifierController CreateController() =>
            new GameObject("WaveIntensifier_Test").AddComponent<WaveHazardIntensifierController>();

        private static HazardZoneGroupToggleController CreateToggleController() =>
            new GameObject("HazardToggle_Test").AddComponent<HazardZoneGroupToggleController>();

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_BaseInterval_Default()
        {
            var so = CreateIntensifierSO();
            Assert.AreEqual(5f, so.BaseInterval, 0.001f,
                "BaseInterval must default to 5 seconds.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MinimumInterval_Default()
        {
            var so = CreateIntensifierSO();
            Assert.AreEqual(1f, so.MinimumInterval, 0.001f,
                "MinimumInterval must default to 1 second.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetIntervalForWave_Wave0_ReturnsBaseInterval()
        {
            var so = CreateIntensifierSO();
            // Base=5, reduction=0.5, min=1 → wave 0 → 5 - 0*0.5 = 5
            float result = so.GetIntervalForWave(0);
            Assert.AreEqual(5f, result, 0.001f,
                "GetIntervalForWave(0) must return BaseInterval when reduction is applied.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetIntervalForWave_ReducesPerWave()
        {
            var so = CreateIntensifierSO();
            // Default: base=5, reduction=0.5, min=1
            // Wave 4 → 5 - 4*0.5 = 3
            float result = so.GetIntervalForWave(4);
            Assert.AreEqual(3f, result, 0.001f,
                "GetIntervalForWave must reduce the interval by IntensityReduction per wave.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetIntervalForWave_ClampsToMinimum()
        {
            var so = CreateIntensifierSO();
            // Default: base=5, reduction=0.5, min=1
            // Wave 100 → raw = 5 - 50 = -45 → clamped to 1
            float result = so.GetIntervalForWave(100);
            Assert.AreEqual(1f, result, 0.001f,
                "GetIntervalForWave must not go below MinimumInterval.");
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_IntensifierSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.IntensifierSO,
                "IntensifierSO must be null on a fresh WaveHazardIntensifierController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_WaveManager_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.WaveManager,
                "WaveManager must be null on a fresh WaveHazardIntensifierController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_WaveStarted()
        {
            var ctrl      = CreateController();
            var waveEvt   = CreateEvent();
            var waveMan   = CreateWaveManagerSO();
            var intensSO  = CreateIntensifierSO();
            var toggleCtrl = CreateToggleController();

            SetField(ctrl, "_onWaveStarted",      waveEvt);
            SetField(ctrl, "_waveManager",        waveMan);
            SetField(ctrl, "_intensifierSO",      intensSO);
            SetField(ctrl, "_toggleControllers",
                new HazardZoneGroupToggleController[] { toggleCtrl });

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            float originalInterval = toggleCtrl.ToggleInterval;
            waveEvt.Raise();   // must NOT call HandleWaveStarted

            Assert.AreEqual(originalInterval, toggleCtrl.ToggleInterval, 0.001f,
                "After OnDisable, raising _onWaveStarted must NOT update toggle controllers.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(toggleCtrl.gameObject);
            Object.DestroyImmediate(waveEvt);
            Object.DestroyImmediate(waveMan);
            Object.DestroyImmediate(intensSO);
        }

        [Test]
        public void Controller_HandleWaveStarted_NullIntensifier_NoThrow()
        {
            var ctrl    = CreateController();
            var waveMan = CreateWaveManagerSO();
            SetField(ctrl, "_waveManager", waveMan);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleWaveStarted(),
                "HandleWaveStarted with null IntensifierSO must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(waveMan);
        }

        [Test]
        public void Controller_HandleWaveStarted_NullWaveManager_NoThrow()
        {
            var ctrl     = CreateController();
            var intensSO = CreateIntensifierSO();
            SetField(ctrl, "_intensifierSO", intensSO);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleWaveStarted(),
                "HandleWaveStarted with null WaveManager must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(intensSO);
        }

        [Test]
        public void Controller_HandleWaveStarted_SetsInterval_OnToggleControllers()
        {
            var ctrl       = CreateController();
            var waveMan    = CreateWaveManagerSO();
            var intensSO   = CreateIntensifierSO();
            var toggleCtrl = CreateToggleController();
            var waveCfg    = CreateWaveConfigSO();

            // Start survival so CurrentWave is 1.
            waveMan.StartSurvival(waveCfg);

            SetField(ctrl, "_waveManager",       waveMan);
            SetField(ctrl, "_intensifierSO",     intensSO);
            SetField(ctrl, "_toggleControllers",
                new HazardZoneGroupToggleController[] { toggleCtrl });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleWaveStarted();

            // wave=1 → 5 - 1*0.5 = 4.5
            float expected = intensSO.GetIntervalForWave(waveMan.CurrentWave);
            Assert.AreEqual(expected, toggleCtrl.ToggleInterval, 0.001f,
                "HandleWaveStarted must apply the wave-scaled interval to all toggle controllers.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(toggleCtrl.gameObject);
            Object.DestroyImmediate(waveMan);
            Object.DestroyImmediate(intensSO);
            Object.DestroyImmediate(waveCfg);
        }

        [Test]
        public void Controller_HandleWaveStarted_NullToggleEntry_Skipped()
        {
            var ctrl     = CreateController();
            var waveMan  = CreateWaveManagerSO();
            var intensSO = CreateIntensifierSO();
            var waveCfg  = CreateWaveConfigSO();
            waveMan.StartSurvival(waveCfg);

            SetField(ctrl, "_waveManager",       waveMan);
            SetField(ctrl, "_intensifierSO",     intensSO);
            SetField(ctrl, "_toggleControllers",
                new HazardZoneGroupToggleController[] { null });
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleWaveStarted(),
                "A null entry in _toggleControllers must be skipped without throwing.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(waveMan);
            Object.DestroyImmediate(intensSO);
            Object.DestroyImmediate(waveCfg);
        }
    }
}
