using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T270:
    ///   <see cref="ZoneControlDifficultyScalerSO"/>,
    ///   <see cref="ZoneControlDifficultyScaler"/> (MB), and
    ///   <see cref="ControlZoneController"/> <c>SetCaptureTimeScale</c> patch.
    ///
    /// ZoneControlDifficultyScalerSOTests (4):
    ///   FreshInstance_ScaleCount_Three                                  ×1
    ///   GetCaptureTimeScale_ValidIndex_ReturnsCorrectScale              ×1
    ///   GetCaptureTimeScale_OutOfRange_ReturnsOne                       ×1
    ///   GetCaptureTimeScale_EmptyArray_ReturnsOne                       ×1
    ///
    /// ZoneControlDifficultyScalerTests (6):
    ///   FreshInstance_ScalerSO_Null                                     ×1
    ///   FreshInstance_DifficultyIndex_Zero                              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channel                                   ×1
    ///   Apply_NullScalerSO_NoThrow                                      ×1
    ///
    /// ControlZoneController_CaptureTimeScale_Patch (4):
    ///   FreshInstance_CaptureTimeScale_IsOne                            ×1
    ///   SetCaptureTimeScale_SetsValue                                   ×1
    ///   SetCaptureTimeScale_ClampsToMinimum                             ×1
    ///   Apply_WithController_SetsCaptureTimeScale                       ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlDifficultyScalerTests
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

        private static ZoneControlDifficultyScalerSO CreateScalerSO() =>
            ScriptableObject.CreateInstance<ZoneControlDifficultyScalerSO>();

        private static ZoneControlDifficultyScaler CreateScaler() =>
            new GameObject("ZoneDiffScaler_Test").AddComponent<ZoneControlDifficultyScaler>();

        private static ControlZoneController CreateZoneController() =>
            new GameObject("ZoneCtrl_Test").AddComponent<ControlZoneController>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── ZoneControlDifficultyScalerSO tests ───────────────────────────────

        [Test]
        public void FreshInstance_ScaleCount_Three()
        {
            var so = CreateScalerSO();
            Assert.AreEqual(3, so.ScaleCount,
                "Default ScaleCount must be 3 (Easy/Normal/Hard).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetCaptureTimeScale_ValidIndex_ReturnsCorrectScale()
        {
            var so = CreateScalerSO();
            // Default scales: index 0 = 0.5, index 1 = 1.0, index 2 = 2.0
            Assert.AreEqual(1.0f, so.GetCaptureTimeScale(1), 0.001f,
                "GetCaptureTimeScale(1) must return 1.0 (normal difficulty default).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetCaptureTimeScale_OutOfRange_ReturnsOne()
        {
            var so = CreateScalerSO();
            Assert.AreEqual(1f, so.GetCaptureTimeScale(99), 0.001f,
                "Out-of-range index must return 1.0 (neutral scale).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetCaptureTimeScale_EmptyArray_ReturnsOne()
        {
            var so = CreateScalerSO();
            SetField(so, "_captureTimeScales", new float[0]);
            Assert.AreEqual(1f, so.GetCaptureTimeScale(0), 0.001f,
                "Empty scale array must return 1.0.");
            Object.DestroyImmediate(so);
        }

        // ── ZoneControlDifficultyScaler (MB) tests ────────────────────────────

        [Test]
        public void FreshInstance_ScalerSO_Null()
        {
            var scaler = CreateScaler();
            Assert.IsNull(scaler.ScalerSO,
                "ScalerSO must be null on a fresh ZoneControlDifficultyScaler.");
            Object.DestroyImmediate(scaler.gameObject);
        }

        [Test]
        public void FreshInstance_DifficultyIndex_Zero()
        {
            var scaler = CreateScaler();
            Assert.AreEqual(0, scaler.DifficultyIndex,
                "DifficultyIndex must default to 0.");
            Object.DestroyImmediate(scaler.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var scaler = CreateScaler();
            InvokePrivate(scaler, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(scaler, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(scaler.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var scaler = CreateScaler();
            InvokePrivate(scaler, "Awake");
            InvokePrivate(scaler, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(scaler, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(scaler.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var scaler   = CreateScaler();
            var scalerSO = CreateScalerSO();
            var ctrl     = CreateZoneController();
            var evtStart = CreateEvent();

            SetField(scaler, "_scalerSO",         scalerSO);
            SetField(scaler, "_zoneControllers",  new ControlZoneController[] { ctrl });
            SetField(scaler, "_onMatchStarted",   evtStart);
            SetField(scaler, "_difficultyIndex",  2); // scale = 2.0

            InvokePrivate(ctrl,   "Awake");
            InvokePrivate(scaler, "Awake");
            InvokePrivate(scaler, "OnEnable");
            InvokePrivate(scaler, "OnDisable");

            // Scale should still be 1 (not yet applied) — raise event after disable.
            float scaleBefore = ctrl.CaptureTimeScale;
            evtStart.Raise(); // must NOT call Apply
            Assert.AreEqual(scaleBefore, ctrl.CaptureTimeScale,
                "After OnDisable, match-started event must not call Apply.");

            Object.DestroyImmediate(scaler.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(scalerSO);
            Object.DestroyImmediate(evtStart);
        }

        [Test]
        public void Apply_NullScalerSO_NoThrow()
        {
            var scaler = CreateScaler();
            InvokePrivate(scaler, "Awake");
            Assert.DoesNotThrow(() => scaler.Apply(),
                "Apply with null _scalerSO must not throw.");
            Object.DestroyImmediate(scaler.gameObject);
        }

        // ── ControlZoneController CaptureTimeScale patch tests ────────────────

        [Test]
        public void FreshInstance_CaptureTimeScale_IsOne()
        {
            var ctrl = CreateZoneController();
            Assert.AreEqual(1f, ctrl.CaptureTimeScale, 0.001f,
                "CaptureTimeScale must default to 1.0 on a fresh ControlZoneController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void SetCaptureTimeScale_SetsValue()
        {
            var ctrl = CreateZoneController();
            ctrl.SetCaptureTimeScale(2.0f);
            Assert.AreEqual(2.0f, ctrl.CaptureTimeScale, 0.001f,
                "SetCaptureTimeScale(2.0) must store 2.0.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void SetCaptureTimeScale_ClampsToMinimum()
        {
            var ctrl = CreateZoneController();
            ctrl.SetCaptureTimeScale(0f);
            Assert.AreEqual(0.1f, ctrl.CaptureTimeScale, 0.001f,
                "SetCaptureTimeScale(0) must be clamped to 0.1.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Apply_WithController_SetsCaptureTimeScale()
        {
            var scaler   = CreateScaler();
            var scalerSO = CreateScalerSO();
            var ctrl     = CreateZoneController();

            // Default scale array: index 0 = 0.5, index 1 = 1.0, index 2 = 2.0
            SetField(scaler, "_scalerSO",        scalerSO);
            SetField(scaler, "_zoneControllers", new ControlZoneController[] { ctrl });
            SetField(scaler, "_difficultyIndex", 2); // scale = 2.0

            InvokePrivate(ctrl,   "Awake");
            InvokePrivate(scaler, "Awake");
            scaler.Apply();

            Assert.AreEqual(2.0f, ctrl.CaptureTimeScale, 0.001f,
                "Apply must set CaptureTimeScale on the target controller to the SO value.");

            Object.DestroyImmediate(scaler.gameObject);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(scalerSO);
        }
    }
}
