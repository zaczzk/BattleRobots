using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T338: <see cref="ZoneControlCountdownHUDController"/>.
    ///
    /// ZoneControlCountdownHUDTests (12):
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_Refresh_NullCountdownSO_HidesPanel                 ×1
    ///   Controller_Refresh_ActiveCountdown_ShowsPanel                 ×1
    ///   Controller_Refresh_ActiveCountdown_SetsProgressBarValue       ×1
    ///   Controller_Refresh_ActiveCountdown_SetsStatusLabel_ReadyIn    ×1
    ///   Controller_Refresh_InactiveCountdown_SetsStatusLabel_Available ×1
    ///   Controller_HandleExpired_SetsIsExpired_True                   ×1
    ///   Controller_OnEnable_ResetsExpiredFlag                         ×1
    ///   Controller_Tick_NullCountdownSO_DoesNotThrow                  ×1
    ///   Controller_CountdownSO_Property_ReturnsAssignedSO             ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlCountdownHUDTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlZoneCountdownSO CreateCountdownSO(float duration = 5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneCountdownSO>();
            SetField(so, "_duration", duration);
            so.Reset();
            return so;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlCountdownHUDController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onCountdownExpired", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onCountdownExpired must be unregistered after OnDisable " +
                "(only the external callback should fire).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullCountdownSO_HidesPanel()
        {
            var go    = new GameObject("Test_NullSO");
            var ctrl  = go.AddComponent<ZoneControlCountdownHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _countdownSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_ActiveCountdown_ShowsPanel()
        {
            var go    = new GameObject("Test_ShowPanel");
            var ctrl  = go.AddComponent<ZoneControlCountdownHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            var so = CreateCountdownSO(5f);
            so.StartCountdown();
            SetField(ctrl, "_countdownSO", so);
            SetField(ctrl, "_panel", panel);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when countdown SO is assigned and active.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_ActiveCountdown_SetsProgressBarValue()
        {
            var go    = new GameObject("Test_ProgressBar");
            var ctrl  = go.AddComponent<ZoneControlCountdownHUDController>();
            var so    = CreateCountdownSO(10f);
            so.StartCountdown();
            so.Tick(2f); // Progress = 0.8

            var sliderGo = new GameObject("Slider");
            var slider   = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            SetField(ctrl, "_countdownSO", so);
            SetField(ctrl, "_progressBar", slider);

            ctrl.Refresh();

            Assert.AreEqual(so.Progress, slider.value, 0.001f,
                "Progress bar value must equal ZoneControlZoneCountdownSO.Progress.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(sliderGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_ActiveCountdown_SetsStatusLabel_ReadyIn()
        {
            var go   = new GameObject("Test_LabelActive");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();
            var so   = CreateCountdownSO(10f);
            so.StartCountdown();
            so.Tick(5f); // Progress = 0.5, remaining = 5s

            var textGo = new GameObject("Text");
            var text   = textGo.AddComponent<Text>();
            SetField(ctrl, "_countdownSO", so);
            SetField(ctrl, "_statusLabel", text);

            ctrl.Refresh();

            StringAssert.StartsWith("Ready in",
                text.text,
                "Status label must start with 'Ready in' while countdown is active.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_InactiveCountdown_SetsStatusLabel_Available()
        {
            var go   = new GameObject("Test_LabelInactive");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();
            var so   = CreateCountdownSO(1f);
            // Do not start — IsActive is false

            var textGo = new GameObject("Text");
            var text   = textGo.AddComponent<Text>();
            SetField(ctrl, "_countdownSO", so);
            SetField(ctrl, "_statusLabel", text);

            ctrl.Refresh();

            Assert.AreEqual("Available", text.text,
                "Status label must be 'Available' when countdown is not active.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleExpired_SetsIsExpired_True()
        {
            var go   = new GameObject("Test_HandleExpired");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();

            ctrl.HandleExpired();

            Assert.IsTrue(ctrl.IsExpired,
                "IsExpired must be true after HandleExpired is called.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_ResetsExpiredFlag()
        {
            var go   = new GameObject("Test_ResetExpired");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();

            ctrl.HandleExpired();
            Assert.IsTrue(ctrl.IsExpired);

            go.SetActive(false);
            go.SetActive(true);

            Assert.IsFalse(ctrl.IsExpired,
                "IsExpired must be reset to false on OnEnable.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_Tick_NullCountdownSO_DoesNotThrow()
        {
            var go   = new GameObject("Test_Tick_NullSO");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();

            Assert.DoesNotThrow(() => ctrl.Tick(),
                "Tick must not throw when _countdownSO is null.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_CountdownSO_Property_ReturnsAssignedSO()
        {
            var go   = new GameObject("Test_Property");
            var ctrl = go.AddComponent<ZoneControlCountdownHUDController>();
            var so   = CreateCountdownSO();
            SetField(ctrl, "_countdownSO", so);

            Assert.AreSame(so, ctrl.CountdownSO,
                "CountdownSO property must return the assigned SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
