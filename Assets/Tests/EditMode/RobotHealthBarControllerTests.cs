using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T217:
    ///   <see cref="RobotHealthBarController"/>.
    ///
    /// RobotHealthBarControllerTests (14):
    ///   FreshInstance_HealthSONull                                ×1
    ///   FreshInstance_CriticalThreshold_Is025                    ×1
    ///   OnEnable_NullRefs_DoesNotThrow                            ×1
    ///   OnDisable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_Unregisters                                     ×1
    ///   Refresh_NullHealthSO_HidesPanel                           ×1
    ///   Refresh_SetsSliderValue                                   ×1
    ///   Refresh_FormatsHealthLabel                                ×1
    ///   Refresh_CriticalOverlay_ShowsWhenBelowThreshold           ×1
    ///   Refresh_CriticalOverlay_HidesWhenAboveThreshold           ×1
    ///   Refresh_NullSlider_DoesNotThrow                           ×1
    ///   Refresh_NullLabel_DoesNotThrow                            ×1
    ///   OnHealthChanged_TriggersRefresh                           ×1
    ///   Refresh_RobotNameLabel_ShowsName                          ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class RobotHealthBarControllerTests
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

        private static RobotHealthBarController CreateController() =>
            new GameObject("RobotHealthBarController_Test").AddComponent<RobotHealthBarController>();

        private static FloatGameEvent CreateFloatEvent() =>
            ScriptableObject.CreateInstance<FloatGameEvent>();

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

        private static HealthSO CreateHealth(float max, float current)
        {
            var health = ScriptableObject.CreateInstance<HealthSO>();
            // InitForMatch sets runtime max, Reset sets current to max.
            health.InitForMatch(max);
            health.Reset();
            // Apply damage to reach desired current health.
            float damage = max - current;
            if (damage > 0f) health.ApplyDamage(damage);
            return health;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_HealthSONull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HealthSO);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_CriticalThreshold_Is025()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0.25f, ctrl.CriticalThreshold, 0.001f);
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
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateFloatEvent();
            SetField(ctrl, "_onHealthChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback((f) => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise(50f);

            Assert.AreEqual(1, count, "After OnDisable only the manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullHealthSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_healthPanel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();   // _healthSO is null

            Assert.IsFalse(panel.activeSelf, "Panel should be hidden when HealthSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_SetsSliderValue()
        {
            var ctrl   = CreateController();
            var slider = AddSlider(ctrl.gameObject, "slider");
            var health = CreateHealth(100f, 75f);
            SetField(ctrl, "_healthSO",  health);
            SetField(ctrl, "_healthBar", slider);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(0.75f, slider.value, 0.001f, "Slider value should equal health ratio.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void Refresh_FormatsHealthLabel()
        {
            var ctrl   = CreateController();
            var label  = AddText(ctrl.gameObject, "label");
            var health = CreateHealth(100f, 60f);
            SetField(ctrl, "_healthSO",     health);
            SetField(ctrl, "_healthLabel",  label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("60/100", label.text, "Label should show 'current/max' format.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void Refresh_CriticalOverlay_ShowsWhenBelowThreshold()
        {
            var ctrl    = CreateController();
            var overlay = new GameObject("overlay");
            overlay.SetActive(false);
            var health  = CreateHealth(100f, 20f);   // 0.20 ratio, below 0.25 threshold
            SetField(ctrl, "_healthSO",         health);
            SetField(ctrl, "_criticalOverlay",  overlay);
            SetField(ctrl, "_criticalThreshold", 0.25f);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(overlay.activeSelf, "Critical overlay should show when health ratio ≤ threshold.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(overlay);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void Refresh_CriticalOverlay_HidesWhenAboveThreshold()
        {
            var ctrl    = CreateController();
            var overlay = new GameObject("overlay");
            overlay.SetActive(true);
            var health  = CreateHealth(100f, 80f);   // 0.80 ratio, above 0.25 threshold
            SetField(ctrl, "_healthSO",         health);
            SetField(ctrl, "_criticalOverlay",  overlay);
            SetField(ctrl, "_criticalThreshold", 0.25f);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(overlay.activeSelf, "Critical overlay should hide when health ratio > threshold.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(overlay);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void Refresh_NullSlider_DoesNotThrow()
        {
            var ctrl   = CreateController();
            var health = CreateHealth(100f, 50f);
            SetField(ctrl, "_healthSO", health);
            // _healthBar left null
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void Refresh_NullLabel_DoesNotThrow()
        {
            var ctrl   = CreateController();
            var health = CreateHealth(100f, 50f);
            SetField(ctrl, "_healthSO", health);
            // _healthLabel left null
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void OnHealthChanged_TriggersRefresh()
        {
            var ctrl   = CreateController();
            var slider = AddSlider(ctrl.gameObject, "slider");
            var ch     = CreateFloatEvent();
            var health = CreateHealth(100f, 100f);

            SetField(ctrl, "_healthSO",        health);
            SetField(ctrl, "_healthBar",       slider);
            SetField(ctrl, "_onHealthChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Damage health then raise event manually to trigger controller Refresh.
            health.ApplyDamage(50f);
            ch.Raise(health.CurrentHealth);

            Assert.AreEqual(0.5f, slider.value, 0.001f,
                "Slider should update when _onHealthChanged fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void Refresh_RobotNameLabel_ShowsName()
        {
            var ctrl      = CreateController();
            var nameLabel = AddText(ctrl.gameObject, "nameLabel");
            var health    = CreateHealth(100f, 100f);

            SetField(ctrl, "_healthSO",        health);
            SetField(ctrl, "_robotNameLabel",  nameLabel);
            SetField(ctrl, "_robotName",       "PlayerBot");
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("PlayerBot", nameLabel.text, "Name label should show the robot name.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(health);
        }
    }
}
