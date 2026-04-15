using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T254: <see cref="MatchPaceHUDController"/>.
    ///
    /// MatchPaceHUDControllerTests (12):
    ///   FreshInstance_PaceSO_Null                                   ×1
    ///   FreshInstance_FastTimer_Zero                                 ×1
    ///   FreshInstance_SlowTimer_Zero                                 ×1
    ///   FreshInstance_FastIndicatorDuration_Default_Three            ×1
    ///   OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   OnDisable_Unregisters_FastChannel                            ×1
    ///   OnDisable_Unregisters_SlowChannel                            ×1
    ///   Refresh_NullSO_HidesPanel                                    ×1
    ///   OnFastPace_SetsFastTimer                                     ×1
    ///   OnSlowPace_SetsSlowTimer                                     ×1
    ///   Tick_FastTimerExpires_HidesFastIndicator                     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class MatchPaceHUDControllerTests
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

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchPaceSO CreatePaceSO() =>
            ScriptableObject.CreateInstance<MatchPaceSO>();

        private static MatchPaceHUDController CreateController() =>
            new GameObject("MatchPaceHUD_Test").AddComponent<MatchPaceHUDController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_PaceSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PaceSO,
                "PaceSO must be null on a fresh MatchPaceHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_FastTimer_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0f, ctrl.FastTimer, 0.001f,
                "FastTimer must be 0 on a fresh MatchPaceHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_SlowTimer_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0f, ctrl.SlowTimer, 0.001f,
                "SlowTimer must be 0 on a fresh MatchPaceHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_FastIndicatorDuration_Default_Three()
        {
            var ctrl = CreateController();
            Assert.AreEqual(3f, ctrl.FastIndicatorDuration, 0.001f,
                "FastIndicatorDuration must default to 3 seconds.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_FastChannel()
        {
            var ctrl     = CreateController();
            var fastEvt  = CreateEvent();
            SetField(ctrl, "_onFastPace", fastEvt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After disable, raising the fast-pace event must NOT set FastTimer.
            fastEvt.Raise();

            Assert.AreEqual(0f, ctrl.FastTimer, 0.001f,
                "After OnDisable, _onFastPace must not update FastTimer.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(fastEvt);
        }

        [Test]
        public void OnDisable_Unregisters_SlowChannel()
        {
            var ctrl    = CreateController();
            var slowEvt = CreateEvent();
            SetField(ctrl, "_onSlowPace", slowEvt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            slowEvt.Raise();

            Assert.AreEqual(0f, ctrl.SlowTimer, 0.001f,
                "After OnDisable, _onSlowPace must not update SlowTimer.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(slowEvt);
        }

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when PaceSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnFastPace_SetsFastTimer()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            ctrl.OnFastPace();

            Assert.AreEqual(ctrl.FastIndicatorDuration, ctrl.FastTimer, 0.001f,
                "OnFastPace must set FastTimer to FastIndicatorDuration.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnSlowPace_SetsSlowTimer()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            ctrl.OnSlowPace();

            Assert.AreEqual(ctrl.SlowIndicatorDuration, ctrl.SlowTimer, 0.001f,
                "OnSlowPace must set SlowTimer to SlowIndicatorDuration.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_FastTimerExpires_HidesFastIndicator()
        {
            var ctrl      = CreateController();
            var indicator = new GameObject("FastIndicator");
            indicator.SetActive(true);
            SetField(ctrl, "_fastIndicator", indicator);

            InvokePrivate(ctrl, "Awake");
            ctrl.OnFastPace(); // sets FastTimer = 3f, activates indicator

            Assert.IsTrue(indicator.activeSelf, "Pre-condition: indicator must be active.");

            // Advance past the timer duration.
            ctrl.Tick(4f);

            Assert.IsFalse(indicator.activeSelf,
                "After Tick exceeds FastIndicatorDuration, the fast indicator must be hidden.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(indicator);
        }
    }
}
