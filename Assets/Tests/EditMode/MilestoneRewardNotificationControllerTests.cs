using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T197:
    ///   <see cref="MilestoneRewardNotificationController"/>.
    ///
    /// MilestoneRewardNotificationControllerTests (14):
    ///   FreshInstance_CatalogIsNull                  ×1
    ///   FreshInstance_QueueIsNull                    ×1
    ///   FreshInstance_DefaultDurationIsTwo           ×1
    ///   FreshInstance_DisplayTimerIsZero             ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow            ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow           ×1
    ///   OnDisable_HidesPanelAndResetsTimer           ×1
    ///   OnDisable_Unregisters                        ×1
    ///   OnRewardGranted_NullPanel_DoesNotThrow       ×1
    ///   OnRewardGranted_ShowsPanel                   ×1
    ///   OnRewardGranted_LabelContainsCatalogLabel    ×1
    ///   OnRewardGranted_NullCatalog_FallbackLabel    ×1
    ///   OnRewardGranted_ForwardsToQueue              ×1
    ///   Tick_ExpiredTimer_HidesPanel                 ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class MilestoneRewardNotificationControllerTests
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

        private static MilestoneRewardCatalogSO CreateCatalog(string label = "Test Reward")
        {
            var so = ScriptableObject.CreateInstance<MilestoneRewardCatalogSO>();
            SetField(so, "_rewardLabel", label);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static NotificationQueueSO CreateQueue() =>
            ScriptableObject.CreateInstance<NotificationQueueSO>();

        private static MilestoneRewardNotificationController CreateController()
        {
            var go = new GameObject("MilestoneRewardNotifCtrl_Test");
            return go.AddComponent<MilestoneRewardNotificationController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_CatalogIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_QueueIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.NotificationQueue);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_DefaultDurationIsTwo()
        {
            var ctrl = CreateController();
            Assert.AreEqual(2f, ctrl.DisplayDuration, 0.001f);
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
            SetField(ctrl, "_displayTimer",      1.5f);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf,   "OnDisable must hide the panel.");
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.001f,
                "OnDisable must reset the display timer.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onRewardGranted", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnRewardGranted_NullPanel_DoesNotThrow()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onRewardGranted", ch);
            // _notificationPanel intentionally left null

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            Assert.DoesNotThrow(() => ch.Raise(),
                "Raising the event with a null panel must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnRewardGranted_ShowsPanel()
        {
            var ctrl  = CreateController();
            var ch    = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            SetField(ctrl, "_onRewardGranted",   ch);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.IsTrue(panel.activeSelf,
                "Notification panel must be active after the reward event fires.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnRewardGranted_LabelContainsCatalogLabel()
        {
            var ctrl     = CreateController();
            var ch       = CreateEvent();
            var catalog  = CreateCatalog("Mastery Milestone");
            var labelGO  = new GameObject("Label");
            var labelTxt = labelGO.AddComponent<Text>();

            SetField(ctrl, "_onRewardGranted", ch);
            SetField(ctrl, "_catalog",         catalog);
            SetField(ctrl, "_rewardLabel",     labelTxt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            StringAssert.Contains("Mastery Milestone", labelTxt.text,
                "Reward label must contain the catalog's RewardLabel string.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnRewardGranted_NullCatalog_FallbackLabel()
        {
            var ctrl     = CreateController();
            var ch       = CreateEvent();
            var labelGO  = new GameObject("Label");
            var labelTxt = labelGO.AddComponent<Text>();

            SetField(ctrl, "_onRewardGranted", ch);
            // _catalog intentionally null
            SetField(ctrl, "_rewardLabel", labelTxt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            StringAssert.Contains("Milestone Reward", labelTxt.text,
                "Null catalog must produce the fallback label 'Milestone Reward'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnRewardGranted_ForwardsToQueue()
        {
            var ctrl    = CreateController();
            var ch      = CreateEvent();
            var queue   = CreateQueue();
            SetField(ctrl, "_onRewardGranted",    ch);
            SetField(ctrl, "_notificationQueue",  queue);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.AreEqual(1, queue.Count,
                "Firing the reward event must enqueue one notification in the queue.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(queue);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_Tick_ExpiredTimer_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);
            SetField(ctrl, "_displayTimer",      1f);

            ctrl.Tick(2f); // advance past duration

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden after the display timer expires.");
            Assert.LessOrEqual(ctrl.DisplayTimer, 0f,
                "DisplayTimer must be ≤ 0 after expiry.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
