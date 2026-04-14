using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T236: <see cref="ObjectiveCompletionNotificationController"/>.
    ///
    /// ObjectiveCompletionNotificationControllerTests (14):
    ///   FreshInstance_BonusObjectiveNull                        ×1
    ///   OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   OnEnable_HidesPanel                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                         ×1
    ///   OnDisable_Unregisters                                   ×1
    ///   Tick_DecrementsTimer                                    ×1
    ///   Tick_HidesPanelWhenExpired                              ×1
    ///   Tick_ZeroTimer_NoOp                                     ×1
    ///   ShowNotification_ActivatesPanel                         ×1
    ///   ShowNotification_SetsMessageLabel                       ×1
    ///   ShowNotification_SetsRewardLabel                        ×1
    ///   ShowNotification_NullBonusObjective_EmptyRewardLabel    ×1
    ///   OnObjectiveCompleted_TriggersShowNotification           ×1
    ///   NotificationQueue_ForwardedOnCompletion                 ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ObjectiveCompletionNotificationControllerTests
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

        private static ObjectiveCompletionNotificationController CreateController() =>
            new GameObject("ObjCompleteNotif_Test")
                .AddComponent<ObjectiveCompletionNotificationController>();

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchBonusObjectiveSO CreateBonusSO(int reward = 100)
        {
            var so = ScriptableObject.CreateInstance<MatchBonusObjectiveSO>();
            SetField(so, "_bonusReward", reward);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static NotificationQueueSO CreateQueueSO() =>
            ScriptableObject.CreateInstance<NotificationQueueSO>();

        // ── Fresh-instance tests ──────────────────────────────────────────────

        [Test]
        public void FreshInstance_BonusObjectiveNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.BonusObjective,
                "BonusObjective must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle null-safety ─────────────────────────────────────────────

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            Assert.IsFalse(panel.activeSelf,
                "OnEnable must hide the notification panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onObjectiveCompleted", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback must fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── Tick tests ────────────────────────────────────────────────────────

        [Test]
        public void Tick_DecrementsTimer()
        {
            var ctrl = CreateController();
            SetField(ctrl, "_displayTimer", 3f);

            ctrl.Tick(1f);

            Assert.AreEqual(2f, ctrl.DisplayTimer, 0.001f,
                "Tick must decrement the display timer by the supplied dt.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_HidesPanelWhenExpired()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);
            SetField(ctrl, "_displayTimer",      1f);

            ctrl.Tick(2f); // 2f > 1f → timer reaches ≤0

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden once the display timer expires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Tick_ZeroTimer_NoOp()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            SetField(ctrl, "_notificationPanel", panel);
            SetField(ctrl, "_displayTimer",      0f);

            ctrl.Tick(1f); // timer is 0 → no-op

            Assert.IsFalse(panel.activeSelf,
                "Tick with a zero timer must not activate the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        // ── ShowNotification functional tests ─────────────────────────────────

        [Test]
        public void ShowNotification_ActivatesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            var bonus = CreateBonusSO(reward: 50);
            var ch    = CreateVoidEvent();
            SetField(ctrl, "_notificationPanel",    panel);
            SetField(ctrl, "_bonusObjective",       bonus);
            SetField(ctrl, "_onObjectiveCompleted", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise(); // triggers ShowNotification via OnObjectiveCompleted

            Assert.IsTrue(panel.activeSelf,
                "Notification panel must be active after ShowNotification.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void ShowNotification_SetsMessageLabel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            var go    = new GameObject("Label");
            var label = go.AddComponent<Text>();
            var ch    = CreateVoidEvent();
            SetField(ctrl, "_notificationPanel",    panel);
            SetField(ctrl, "_messageLabel",         label);
            SetField(ctrl, "_onObjectiveCompleted", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.AreEqual("Objective Complete!", label.text,
                "Message label must display 'Objective Complete!' on notification.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void ShowNotification_SetsRewardLabel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            var go    = new GameObject("Label");
            var label = go.AddComponent<Text>();
            var bonus = CreateBonusSO(reward: 75);
            var ch    = CreateVoidEvent();
            SetField(ctrl, "_notificationPanel",    panel);
            SetField(ctrl, "_rewardLabel",          label);
            SetField(ctrl, "_bonusObjective",       bonus);
            SetField(ctrl, "_onObjectiveCompleted", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.AreEqual("+75 credits", label.text,
                "Reward label must display '+N credits' matching BonusReward.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(bonus);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void ShowNotification_NullBonusObjective_EmptyRewardLabel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            var go    = new GameObject("Label");
            var label = go.AddComponent<Text>();
            var ch    = CreateVoidEvent();
            SetField(ctrl, "_notificationPanel",    panel);
            SetField(ctrl, "_rewardLabel",          label);
            // _bonusObjective remains null
            SetField(ctrl, "_onObjectiveCompleted", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.AreEqual(string.Empty, label.text,
                "Reward label must be empty when BonusObjective is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnObjectiveCompleted_TriggersShowNotification()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            var ch = CreateVoidEvent();
            SetField(ctrl, "_notificationPanel",    panel);
            SetField(ctrl, "_onObjectiveCompleted", ch);
            SetField(ctrl, "_displayDuration",      2f);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.IsTrue(panel.activeSelf,
                "Raising _onObjectiveCompleted must activate the notification panel.");
            Assert.Greater(ctrl.DisplayTimer, 0f,
                "DisplayTimer must be positive after the notification is triggered.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void NotificationQueue_ForwardedOnCompletion()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            var queue = CreateQueueSO();
            var ch    = CreateVoidEvent();
            SetField(ctrl, "_notificationPanel",    panel);
            SetField(ctrl, "_notificationQueue",    queue);
            SetField(ctrl, "_onObjectiveCompleted", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.AreEqual(1, queue.Count,
                "Raising _onObjectiveCompleted must enqueue one notification in the queue.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(queue);
            Object.DestroyImmediate(ch);
        }
    }
}
