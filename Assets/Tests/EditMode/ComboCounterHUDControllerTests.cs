using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T220: <see cref="ComboCounterHUDController"/>.
    ///
    /// ComboCounterHUDControllerTests (12):
    ///   FreshInstance_ComboCounterNull                    ×1
    ///   OnEnable_NullRefs_DoesNotThrow                    ×1
    ///   OnDisable_Unregisters                             ×1
    ///   OnDisable_HidesComboPanelIfPresent                ×1
    ///   Refresh_NullSO_HidesPanelIfPresent                ×1
    ///   Refresh_InactiveCombo_HidesPanel                  ×1
    ///   Refresh_ActiveCombo_ShowsPanel                    ×1
    ///   Refresh_ActiveCombo_SetsComboCountLabel           ×1
    ///   Refresh_SetsMultiplierLabel                       ×1
    ///   Refresh_SetsMaxComboLabel                         ×1
    ///   Refresh_SetsWindowBarValue                        ×1
    ///   Refresh_InactiveCombo_SetsEmDashCountLabel        ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ComboCounterHUDControllerTests
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

        private static ComboCounterSO CreateComboSO(float windowSeconds = 3f)
        {
            var so = ScriptableObject.CreateInstance<ComboCounterSO>();
            SetField(so, "_comboWindowSeconds", windowSeconds);
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ComboCounterHUDController CreateController() =>
            new GameObject("ComboHUD_Test").AddComponent<ComboCounterHUDController>();

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

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ComboCounterNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ComboCounter);
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
            SetField(ctrl, "_onComboChanged", ch);
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
        public void OnDisable_HidesComboPanelIfPresent()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_comboPanel", panel);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf, "OnDisable should hide the combo panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullSO_HidesPanelIfPresent()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_comboPanel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Panel should be hidden when SO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_InactiveCombo_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            var so = CreateComboSO();
            // No RecordHit → IsComboActive == false

            SetField(ctrl, "_comboCounter", so);
            SetField(ctrl, "_comboPanel",   panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Panel should be hidden when combo is not active.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ActiveCombo_ShowsPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            var so = CreateComboSO();
            so.RecordHit(); // IsComboActive = true

            SetField(ctrl, "_comboCounter", so);
            SetField(ctrl, "_comboPanel",   panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf, "Panel should be shown when combo is active.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ActiveCombo_SetsComboCountLabel()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "count");
            var so    = CreateComboSO();
            so.RecordHit();
            so.RecordHit();
            so.RecordHit(); // HitCount = 3

            SetField(ctrl, "_comboCounter",    so);
            SetField(ctrl, "_comboCountLabel", label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            StringAssert.Contains("3", label.text, "Label should show the current hit count.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_SetsMultiplierLabel()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "mult");
            var so    = CreateComboSO();
            // 5 hits → multiplier = 1.1
            for (int i = 0; i < 5; i++) so.RecordHit();

            SetField(ctrl, "_comboCounter",    so);
            SetField(ctrl, "_multiplierLabel", label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            StringAssert.Contains("1.1", label.text, "Label should reflect the current multiplier.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_SetsMaxComboLabel()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "max");
            var so    = CreateComboSO();
            so.RecordHit();
            so.RecordHit(); // MaxCombo = 2

            SetField(ctrl, "_comboCounter",  so);
            SetField(ctrl, "_maxComboLabel", label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            StringAssert.Contains("2", label.text, "Max combo label should contain the max hit count.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_SetsWindowBarValue()
        {
            var ctrl   = CreateController();
            var slider = AddSlider(ctrl.gameObject, "bar");
            var so     = CreateComboSO(4f); // 4s window
            so.RecordHit();               // timer = 4s (ratio = 1.0)

            SetField(ctrl, "_comboCounter", so);
            SetField(ctrl, "_windowBar",    slider);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(1f, slider.value, 0.001f,
                "Slider should equal 1.0 immediately after a hit.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_InactiveCombo_SetsEmDashCountLabel()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "count");
            // No SO assigned → inactive

            SetField(ctrl, "_comboCountLabel", label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("\u2014", label.text,
                "Label should show em-dash when combo is inactive.");
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
