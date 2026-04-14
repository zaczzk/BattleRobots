using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T218:
    ///   <see cref="WeaponCooldownSO"/> and <see cref="WeaponCooldownHUDController"/>.
    ///
    /// WeaponCooldownSOTests (8):
    ///   FreshInstance_DefaultMaxCooldown_Is2                      ×1
    ///   FreshInstance_IsOnCooldown_False                          ×1
    ///   StartCooldown_SetsRemainingToMax                          ×1
    ///   Tick_Running_DecrementsRemaining                          ×1
    ///   Tick_NotOnCooldown_NoChange                               ×1
    ///   Tick_Fires_OnCooldownChanged                              ×1
    ///   Tick_Reaches_Zero_FiresOnCooldownComplete                 ×1
    ///   CooldownRatio_CorrectMidCooldown                          ×1
    ///
    /// WeaponCooldownHUDControllerTests (8):
    ///   FreshInstance_CooldownSONull                              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                            ×1
    ///   OnDisable_Unregisters                                     ×1
    ///   Refresh_NullSO_HidesOverlay                               ×1
    ///   Refresh_OnCooldown_ShowsOverlay                           ×1
    ///   Refresh_NotOnCooldown_ShowsReadyLabel                     ×1
    ///   Refresh_SetsBarValue                                      ×1
    ///   Refresh_SetsCountdownLabel                                ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class WeaponCooldownTests
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

        private static WeaponCooldownSO CreateSO(float maxCooldown = 2f)
        {
            var so = ScriptableObject.CreateInstance<WeaponCooldownSO>();
            SetField(so, "_maxCooldown", maxCooldown);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static WeaponCooldownHUDController CreateController() =>
            new GameObject("WeaponCooldownHUD_Test").AddComponent<WeaponCooldownHUDController>();

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        private static Slider AddSlider(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Slider>();
        }

        // ── WeaponCooldownSOTests ──────────────────────────────────────────────

        [Test]
        public void FreshInstance_DefaultMaxCooldown_Is2()
        {
            var so = ScriptableObject.CreateInstance<WeaponCooldownSO>();
            Assert.AreEqual(2f, so.MaxCooldown, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_IsOnCooldown_False()
        {
            var so = CreateSO();
            Assert.IsFalse(so.IsOnCooldown);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void StartCooldown_SetsRemainingToMax()
        {
            var so = CreateSO(3f);
            so.StartCooldown();
            Assert.AreEqual(3f, so.RemainingCooldown, 0.001f);
            Assert.IsTrue(so.IsOnCooldown);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_Running_DecrementsRemaining()
        {
            var so = CreateSO(4f);
            so.StartCooldown();
            so.Tick(1f);
            Assert.AreEqual(3f, so.RemainingCooldown, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_NotOnCooldown_NoChange()
        {
            var so = CreateSO(4f);
            // Not started — should be no-op.
            so.Tick(1f);
            Assert.AreEqual(0f, so.RemainingCooldown, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_Fires_OnCooldownChanged()
        {
            var so  = CreateSO(5f);
            var evt = CreateVoidEvent();
            SetField(so, "_onCooldownChanged", evt);

            int fireCount = 0;
            evt.RegisterCallback(() => fireCount++);

            so.StartCooldown();
            int afterStart = fireCount;   // 1 from StartCooldown
            so.Tick(1f);

            Assert.Greater(fireCount, afterStart, "_onCooldownChanged should fire on Tick.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Tick_Reaches_Zero_FiresOnCooldownComplete()
        {
            var so       = CreateSO(2f);
            var complete = CreateVoidEvent();
            SetField(so, "_onCooldownComplete", complete);

            int firedCount = 0;
            complete.RegisterCallback(() => firedCount++);

            so.StartCooldown();
            so.Tick(10f);   // overshoot to zero

            Assert.AreEqual(1, firedCount, "_onCooldownComplete should fire exactly once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(complete);
        }

        [Test]
        public void CooldownRatio_CorrectMidCooldown()
        {
            var so = CreateSO(4f);
            so.StartCooldown();
            so.Tick(1f);   // 3s remaining out of 4s → ratio = 0.75

            Assert.AreEqual(0.75f, so.CooldownRatio, 0.001f);
            Object.DestroyImmediate(so);
        }

        // ── WeaponCooldownHUDControllerTests ──────────────────────────────────

        [Test]
        public void FreshInstance_CooldownSONull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.CooldownSO);
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
            SetField(ctrl, "_onCooldownChanged", ch);
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
            SetField(ctrl, "_cooldownOverlay", overlay);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();   // _cooldownSO is null

            Assert.IsFalse(overlay.activeSelf, "Overlay should be hidden when SO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(overlay);
        }

        [Test]
        public void Refresh_OnCooldown_ShowsOverlay()
        {
            var ctrl    = CreateController();
            var overlay = new GameObject("overlay");
            overlay.SetActive(false);
            var so      = CreateSO(3f);
            so.StartCooldown();

            SetField(ctrl, "_cooldownSO",      so);
            SetField(ctrl, "_cooldownOverlay", overlay);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(overlay.activeSelf, "Overlay should show when weapon is on cooldown.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(overlay);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_NotOnCooldown_ShowsReadyLabel()
        {
            var ctrl       = CreateController();
            var readyLabel = AddText(ctrl.gameObject, "ready");
            readyLabel.gameObject.SetActive(false);
            var so         = CreateSO(2f);
            // Not started → not on cooldown

            SetField(ctrl, "_cooldownSO",  so);
            SetField(ctrl, "_readyLabel",  readyLabel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(readyLabel.gameObject.activeSelf,
                "Ready label should be active when not on cooldown.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_SetsBarValue()
        {
            var ctrl   = CreateController();
            var slider = AddSlider(ctrl.gameObject, "bar");
            var so     = CreateSO(4f);
            so.StartCooldown();
            so.Tick(1f);   // 3s remaining → ratio = 0.75

            SetField(ctrl, "_cooldownSO",  so);
            SetField(ctrl, "_cooldownBar", slider);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(0.75f, slider.value, 0.001f,
                "Slider value should match CooldownRatio.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_SetsCountdownLabel()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "countdown");
            var so    = CreateSO(2f);
            so.StartCooldown();   // 2.0s remaining

            SetField(ctrl, "_cooldownSO",    so);
            SetField(ctrl, "_cooldownLabel", label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            StringAssert.EndsWith("s", label.text,
                "Cooldown label should end with 's' when on cooldown.");
            Assert.IsNotEmpty(label.text, "Cooldown label should not be empty when on cooldown.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }
    }
}
