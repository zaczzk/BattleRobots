using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T221:
    ///   <see cref="AbilityChargeSO"/> and <see cref="AbilityChargeHUDController"/>.
    ///
    /// AbilityChargeSOTests (8):
    ///   FreshInstance_DefaultMaxCharge_Is100              ×1
    ///   FreshInstance_CurrentCharge_IsZero                ×1
    ///   AddCharge_IncreasesCurrentCharge                  ×1
    ///   AddCharge_ClampsToMaxCharge                       ×1
    ///   AddCharge_Fires_OnChargeChanged                   ×1
    ///   AddCharge_WhenFull_Fires_OnFullyCharged           ×1
    ///   Activate_WhenFull_ResetsCharge                    ×1
    ///   Activate_WhenNotFull_NoOp                         ×1
    ///
    /// AbilityChargeHUDControllerTests (6):
    ///   FreshInstance_ChargeSO_Null                       ×1
    ///   OnEnable_NullRefs_DoesNotThrow                    ×1
    ///   OnDisable_Unregisters                             ×1
    ///   Refresh_NullSO_HidesOverlay                       ×1
    ///   Refresh_FullyCharged_ShowsReadyOverlay            ×1
    ///   Refresh_SetsBarValue                              ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class AbilityChargeTests
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

        private static AbilityChargeSO CreateChargeSO(float maxCharge = 100f, float chargePerDamage = 1f)
        {
            var so = ScriptableObject.CreateInstance<AbilityChargeSO>();
            SetField(so, "_maxCharge",       maxCharge);
            SetField(so, "_chargePerDamage", chargePerDamage);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static AbilityChargeHUDController CreateController() =>
            new GameObject("AbilityChargeHUD_Test").AddComponent<AbilityChargeHUDController>();

        private static Slider AddSlider(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Slider>();
        }

        // ── AbilityChargeSOTests ──────────────────────────────────────────────

        [Test]
        public void FreshInstance_DefaultMaxCharge_Is100()
        {
            var so = ScriptableObject.CreateInstance<AbilityChargeSO>();
            Assert.AreEqual(100f, so.MaxCharge, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_CurrentCharge_IsZero()
        {
            var so = CreateChargeSO();
            Assert.AreEqual(0f, so.CurrentCharge, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddCharge_IncreasesCurrentCharge()
        {
            var so = CreateChargeSO(100f, 1f);
            so.AddCharge(25f);
            Assert.AreEqual(25f, so.CurrentCharge, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddCharge_ClampsToMaxCharge()
        {
            var so = CreateChargeSO(100f, 1f);
            so.AddCharge(999f);
            Assert.AreEqual(100f, so.CurrentCharge, 0.001f,
                "CurrentCharge must not exceed MaxCharge.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddCharge_Fires_OnChargeChanged()
        {
            var so  = CreateChargeSO();
            var evt = CreateVoidEvent();
            SetField(so, "_onChargeChanged", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.AddCharge(10f);

            Assert.AreEqual(1, count, "_onChargeChanged should fire on AddCharge.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void AddCharge_WhenFull_Fires_OnFullyCharged()
        {
            var so      = CreateChargeSO(50f, 1f);
            var fullEvt = CreateVoidEvent();
            SetField(so, "_onFullyCharged", fullEvt);

            int count = 0;
            fullEvt.RegisterCallback(() => count++);
            so.AddCharge(50f); // exactly fills to max

            Assert.AreEqual(1, count, "_onFullyCharged should fire once when bar fills.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(fullEvt);
        }

        [Test]
        public void Activate_WhenFull_ResetsCharge()
        {
            var so = CreateChargeSO(50f, 1f);
            so.AddCharge(50f);
            Assert.IsTrue(so.IsFullyCharged);

            so.Activate();

            Assert.AreEqual(0f, so.CurrentCharge, 0.001f,
                "Activate should reset charge to zero.");
            Assert.IsFalse(so.IsFullyCharged);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Activate_WhenNotFull_NoOp()
        {
            var so = CreateChargeSO(100f, 1f);
            so.AddCharge(50f);
            float before = so.CurrentCharge;

            so.Activate(); // not fully charged → no-op

            Assert.AreEqual(before, so.CurrentCharge, 0.001f,
                "Activate should be a no-op when the bar is not full.");
            Object.DestroyImmediate(so);
        }

        // ── AbilityChargeHUDControllerTests ───────────────────────────────────

        [Test]
        public void FreshInstance_ChargeSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ChargeSO);
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
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onChargeChanged", ch);
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
        public void Refresh_NullSO_HidesOverlay()
        {
            var ctrl    = CreateController();
            var overlay = new GameObject("overlay");
            overlay.SetActive(true);
            SetField(ctrl, "_readyOverlay", overlay);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh(); // _chargeSO is null

            Assert.IsFalse(overlay.activeSelf, "Overlay should be hidden when SO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(overlay);
        }

        [Test]
        public void Refresh_FullyCharged_ShowsReadyOverlay()
        {
            var ctrl    = CreateController();
            var overlay = new GameObject("overlay");
            overlay.SetActive(false);
            var so      = CreateChargeSO(50f, 1f);
            so.AddCharge(50f); // fully charged

            SetField(ctrl, "_chargeSO",      so);
            SetField(ctrl, "_readyOverlay",  overlay);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(overlay.activeSelf, "Overlay should show when ability is fully charged.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(overlay);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_SetsBarValue()
        {
            var ctrl   = CreateController();
            var slider = AddSlider(ctrl.gameObject, "bar");
            var so     = CreateChargeSO(100f, 1f);
            so.AddCharge(50f); // ratio = 0.5

            SetField(ctrl, "_chargeSO",  so);
            SetField(ctrl, "_chargeBar", slider);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(0.5f, slider.value, 0.001f,
                "Slider value should match ChargeRatio.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }
    }
}
