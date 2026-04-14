using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T183:
    ///   <see cref="MasteryNotificationController"/>.
    ///
    /// MasteryNotificationControllerTests (12):
    ///   FreshInstance_MasteryIsNull ×1
    ///   FreshInstance_DefaultDuration_IsThree ×1
    ///   FreshInstance_DisplayTimerIsZero ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow ×1
    ///   OnDisable_HidesPanelAndResetsTimer ×1
    ///   OnDisable_Unregisters ×1
    ///   OnMasteryUnlocked_NullMastery_NoShow ×1
    ///   OnMasteryUnlocked_TypeMastered_ShowsBanner ×1
    ///   OnMasteryUnlocked_BannerTextContainsTypeName ×1
    ///   Tick_AdvancesTimerAndHidesPanel ×1
    ///   Tick_ZeroTimer_DoesNothing ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class MasteryNotificationControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static DamageTypeMasteryConfig CreateConfig(float threshold = 100f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            SetField(cfg, "_physicalThreshold", threshold);
            SetField(cfg, "_energyThreshold",   threshold);
            SetField(cfg, "_thermalThreshold",  threshold);
            SetField(cfg, "_shockThreshold",    threshold);
            return cfg;
        }

        private static DamageTypeMasterySO CreateMastery(DamageTypeMasteryConfig cfg = null)
        {
            var so = ScriptableObject.CreateInstance<DamageTypeMasterySO>();
            if (cfg != null)
                SetField(so, "_config", cfg);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MasteryNotificationController CreateController()
        {
            var go = new GameObject("MasteryNotifCtrl_Test");
            return go.AddComponent<MasteryNotificationController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_MasteryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Mastery);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_DefaultDurationIsThree()
        {
            var ctrl = CreateController();
            Assert.AreEqual(3f, ctrl.NotificationDuration, 0.001f);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_DisplayTimerIsZero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.001f);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_HidesPanelAndResetsTimer()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            // Simulate a timer being active.
            SetField(ctrl, "_displayTimer", 2f);
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf, "OnDisable must hide the panel.");
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.001f, "OnDisable must reset the display timer.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMasteryUnlocked", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable, only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMasteryUnlocked_NullMastery_NoShow()
        {
            var ctrl  = CreateController();
            var ch    = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_onMasteryUnlocked",   ch);
            SetField(ctrl, "_notificationPanel",   panel);
            // _mastery is null

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.IsFalse(panel.activeSelf,
                "Null mastery must prevent the notification banner from showing.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMasteryUnlocked_TypeMastered_ShowsBanner()
        {
            var ctrl  = CreateController();
            var cfg   = CreateConfig(50f);
            var ch    = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            // Create mastery SO with a channel so we can fire the event externally.
            var mastery = CreateMastery(cfg);
            SetField(mastery, "_onMasteryUnlocked", ch);

            SetField(ctrl, "_mastery",             mastery);
            SetField(ctrl, "_onMasteryUnlocked",   ch);
            SetField(ctrl, "_notificationPanel",   panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable"); // snapshots: all false

            // Master Physical (raises the shared event channel).
            mastery.AddDealt(50f, DamageType.Physical);

            Assert.IsTrue(panel.activeSelf,
                "Notification banner must appear when a type is first mastered.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMasteryUnlocked_BannerTextContainsTypeName()
        {
            var ctrl      = CreateController();
            var cfg       = CreateConfig(10f);
            var ch        = CreateEvent();
            var labelGO   = new GameObject("Label");
            var labelText = labelGO.AddComponent<Text>();

            var mastery = CreateMastery(cfg);
            SetField(mastery, "_onMasteryUnlocked", ch);

            SetField(ctrl, "_mastery",            mastery);
            SetField(ctrl, "_onMasteryUnlocked",  ch);
            SetField(ctrl, "_notificationLabel",  labelText);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            mastery.AddDealt(10f, DamageType.Energy); // masters Energy

            StringAssert.Contains("Energy", labelText.text,
                "Notification label must include the mastered type name.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_Tick_AdvancesTimerAndHidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);
            SetField(ctrl, "_displayTimer",      2f);

            // Advance past duration.
            ctrl.Tick(3f);

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden after timer expires.");
            Assert.LessOrEqual(ctrl.DisplayTimer, 0f,
                "DisplayTimer must be ≤ 0 after expiry.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_Tick_ZeroTimer_DoesNothing()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            SetField(ctrl, "_notificationPanel", panel);
            // _displayTimer defaults to 0

            ctrl.Tick(5f); // large dt, but timer is 0 — should no-op

            Assert.IsFalse(panel.activeSelf,
                "Tick with zero timer must not activate the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
