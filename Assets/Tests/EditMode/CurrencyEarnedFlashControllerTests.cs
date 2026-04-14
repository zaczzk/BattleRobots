using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T199:
    ///   <see cref="CurrencyEarnedFlashController"/>.
    ///
    /// CurrencyEarnedFlashControllerTests (12):
    ///   FreshInstance_MatchResultIsNull              ×1
    ///   FreshInstance_QueueIsNull                    ×1
    ///   FreshInstance_DefaultDurationIsTwo           ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow            ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow           ×1
    ///   OnDisable_HidesPanelAndResetsTimer           ×1
    ///   OnDisable_Unregisters                        ×1
    ///   OnMatchEnded_ZeroCurrency_NoFlash            ×1
    ///   OnMatchEnded_NullResult_NoFlash              ×1
    ///   OnMatchEnded_PositiveCurrency_ShowsPanel     ×1
    ///   OnMatchEnded_LabelContainsCurrencyAmount     ×1
    ///   OnMatchEnded_ForwardsToQueue                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class CurrencyEarnedFlashControllerTests
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

        private static MatchResultSO CreateResult(int currency = 0)
        {
            var so = ScriptableObject.CreateInstance<MatchResultSO>();
            so.Write(playerWon: true, durationSeconds: 60f,
                     currencyEarned: currency, newWalletBalance: currency);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static NotificationQueueSO CreateQueue() =>
            ScriptableObject.CreateInstance<NotificationQueueSO>();

        private static CurrencyEarnedFlashController CreateController()
        {
            var go = new GameObject("CurrencyEarnedFlashCtrl_Test");
            return go.AddComponent<CurrencyEarnedFlashController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_MatchResultIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.MatchResult);
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
            Assert.AreEqual(2f, ctrl.FlashDuration, 0.001f);
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
            SetField(ctrl, "_flashPanel", panel);
            SetField(ctrl, "_flashTimer", 1.5f);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf, "OnDisable must hide the flash panel.");
            Assert.AreEqual(0f, ctrl.FlashTimer, 0.001f,
                "OnDisable must reset the flash timer.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
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
        public void Ctrl_OnMatchEnded_ZeroCurrency_NoFlash()
        {
            var ctrl   = CreateController();
            var ch     = CreateEvent();
            var result = CreateResult(currency: 0);
            var panel  = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_onMatchEnded", ch);
            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_flashPanel",   panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.IsFalse(panel.activeSelf,
                "Zero currency earned must not activate the flash panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMatchEnded_NullResult_NoFlash()
        {
            var ctrl  = CreateController();
            var ch    = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_onMatchEnded", ch);
            // _matchResult intentionally null
            SetField(ctrl, "_flashPanel",   panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.IsFalse(panel.activeSelf,
                "Null MatchResultSO (implies zero currency) must not activate the flash panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMatchEnded_PositiveCurrency_ShowsPanel()
        {
            var ctrl   = CreateController();
            var ch     = CreateEvent();
            var result = CreateResult(currency: 300);
            var panel  = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_onMatchEnded", ch);
            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_flashPanel",   panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.IsTrue(panel.activeSelf,
                "Positive currency earned must activate the flash panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMatchEnded_LabelContainsCurrencyAmount()
        {
            var ctrl     = CreateController();
            var ch       = CreateEvent();
            var result   = CreateResult(currency: 450);
            var labelGO  = new GameObject("Label");
            var label    = labelGO.AddComponent<Text>();

            SetField(ctrl, "_onMatchEnded",  ch);
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_currencyLabel", label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            StringAssert.Contains("450", label.text,
                "Currency label must contain the earned currency amount.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMatchEnded_ForwardsToQueue()
        {
            var ctrl   = CreateController();
            var ch     = CreateEvent();
            var result = CreateResult(currency: 200);
            var queue  = CreateQueue();

            SetField(ctrl, "_onMatchEnded",       ch);
            SetField(ctrl, "_matchResult",        result);
            SetField(ctrl, "_notificationQueue",  queue);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            ch.Raise();

            Assert.AreEqual(1, queue.Count,
                "Positive currency earned must enqueue one notification in the queue.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(queue);
            Object.DestroyImmediate(ch);
        }
    }
}
