using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T244: <see cref="RespawnProtectionHUDController"/>.
    ///
    /// RespawnProtectionHUDTests (14):
    ///   FreshInstance_ProtectionSO_Null                          ×1
    ///   FreshInstance_IsProtected_False                          ×1
    ///   OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_NullRefs_DoesNotThrow                          ×1
    ///   OnDisable_Unregisters_Channel                            ×1
    ///   HandleRespawnReady_NullSO_DoesNotThrow_DoesNotSetProtected ×1
    ///   HandleRespawnReady_SOAssigned_SetsIsProtected_True       ×1
    ///   HandleRespawnReady_ResetsElapsed                         ×1
    ///   HandleRespawnReady_ShowsPanel                            ×1
    ///   Tick_NotProtected_NoOp                                   ×1
    ///   Tick_NullSO_DoesNotThrow                                 ×1
    ///   Tick_BelowDuration_StaysProtected                        ×1
    ///   Tick_ExceedsDuration_EndsProtection                      ×1
    ///   Tick_UpdatesShieldBar_Ratio                              ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class RespawnProtectionHUDTests
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

        private static RespawnProtectionHUDController CreateController() =>
            new GameObject("RespawnProtHUD_Test").AddComponent<RespawnProtectionHUDController>();

        private static RespawnProtectionSO CreateProtectionSO() =>
            ScriptableObject.CreateInstance<RespawnProtectionSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static Slider CreateSlider()
        {
            var go = new GameObject("Slider_Test");
            return go.AddComponent<Slider>();
        }

        // ── Fresh-instance tests ──────────────────────────────────────────────

        [Test]
        public void FreshInstance_ProtectionSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ProtectionSO,
                "ProtectionSO must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_IsProtected_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must be false on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle tests ───────────────────────────────────────────────────

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
        public void OnDisable_Unregisters_Channel()
        {
            var ctrl = CreateController();
            var evt  = CreateEvent();
            SetField(ctrl, "_onRespawnReady", evt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable, only external callbacks fire on _onRespawnReady.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(evt);
        }

        // ── HandleRespawnReady tests ──────────────────────────────────────────

        [Test]
        public void HandleRespawnReady_NullSO_DoesNotThrow_DoesNotSetProtected()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleRespawnReady(),
                "HandleRespawnReady with null _protectionSO must not throw.");
            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must remain false when _protectionSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleRespawnReady_SOAssigned_SetsIsProtected_True()
        {
            var ctrl = CreateController();
            var so   = CreateProtectionSO();
            SetField(ctrl, "_protectionSO", so);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();

            Assert.IsTrue(ctrl.IsProtected,
                "IsProtected must be true after HandleRespawnReady with SO assigned.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void HandleRespawnReady_ResetsElapsed()
        {
            var ctrl = CreateController();
            var so   = CreateProtectionSO();
            SetField(ctrl, "_protectionSO", so);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();
            ctrl.Tick(1f);           // advance elapsed
            ctrl.HandleRespawnReady(); // second call resets

            Assert.AreEqual(0f, ctrl.Elapsed,
                "Elapsed must be reset to 0 on HandleRespawnReady.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void HandleRespawnReady_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateProtectionSO();
            var panel = new GameObject("Panel_Test");
            panel.SetActive(false);
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();

            Assert.IsTrue(panel.activeSelf,
                "_panel must be shown after HandleRespawnReady.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        // ── Tick tests ────────────────────────────────────────────────────────

        [Test]
        public void Tick_NotProtected_NoOp()
        {
            var ctrl   = CreateController();
            var so     = CreateProtectionSO();
            var slider = CreateSlider();
            slider.value = 0.5f;
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_shieldBar", slider);
            InvokePrivate(ctrl, "Awake");

            // Not protected — Tick should be a no-op.
            ctrl.Tick(10f);

            Assert.AreEqual(0.5f, slider.value,
                "Slider value must not change when not protected.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(slider.gameObject);
        }

        [Test]
        public void Tick_NullSO_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            // Force _isProtected=true via reflection, then tick with null SO.
            SetField(ctrl, "_isProtected", true);
            Assert.DoesNotThrow(() => ctrl.Tick(1f),
                "Tick with null _protectionSO while protected must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_BelowDuration_StaysProtected()
        {
            var ctrl = CreateController();
            var so   = CreateProtectionSO();   // ProtectionDuration = 3f
            SetField(ctrl, "_protectionSO", so);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();
            ctrl.Tick(2f);   // below 3f

            Assert.IsTrue(ctrl.IsProtected,
                "IsProtected must remain true before ProtectionDuration elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_ExceedsDuration_EndsProtection()
        {
            var ctrl = CreateController();
            var so   = CreateProtectionSO();   // ProtectionDuration = 3f
            SetField(ctrl, "_protectionSO", so);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();
            ctrl.Tick(4f);   // exceeds 3f

            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must be false once ProtectionDuration elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_UpdatesShieldBar_Ratio()
        {
            var ctrl   = CreateController();
            var so     = CreateProtectionSO();   // ProtectionDuration = 3f
            var slider = CreateSlider();
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_shieldBar", slider);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();
            ctrl.Tick(1f);   // 2f remaining of 3f → ratio = 2/3 ≈ 0.667

            float expectedRatio = 2f / 3f;
            Assert.AreEqual(expectedRatio, slider.value, 0.001f,
                "Shield bar value must reflect remaining/duration ratio.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(slider.gameObject);
        }
    }
}
