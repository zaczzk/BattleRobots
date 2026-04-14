using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T228:
    ///   <see cref="MatchBonusObjectiveSO"/> and <see cref="BonusObjectiveHUDController"/>.
    ///
    /// MatchBonusObjectiveSOTests (12):
    ///   FreshInstance_BonusTitle_IsDefault                         ×1
    ///   FreshInstance_IsComplete_IsFalse                           ×1
    ///   FreshInstance_IsExpired_IsFalse                            ×1
    ///   FreshInstance_HasTimeLimit_WhenTimeLimitPositive            ×1
    ///   Complete_SetsIsComplete_AndFiresEvents                     ×1
    ///   Complete_Idempotent                                        ×1
    ///   Expire_SetsIsExpired_AndFiresEvents                        ×1
    ///   Expire_Idempotent                                          ×1
    ///   Complete_BlocksExpire                                      ×1
    ///   Tick_DecrementsTimeRemaining                               ×1
    ///   Tick_AtZero_TriggersExpire                                 ×1
    ///   Reset_ClearsAll                                            ×1
    ///
    /// BonusObjectiveHUDControllerTests (6):
    ///   FreshInstance_BonusObjectiveNull                           ×1
    ///   OnEnable_NullRefs_DoesNotThrow                             ×1
    ///   OnDisable_Unregisters                                      ×1
    ///   Refresh_NullSO_HidesPanel                                  ×1
    ///   Refresh_WhenComplete_ShowsCompleteLabel                    ×1
    ///   Refresh_WhenActive_ShowsActiveLabel                        ×1
    ///
    /// Total: 18 new EditMode tests.
    /// </summary>
    public class MatchBonusObjectiveTests
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

        private static MatchBonusObjectiveSO CreateSO()
        {
            var so = ScriptableObject.CreateInstance<MatchBonusObjectiveSO>();
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static BonusObjectiveHUDController CreateController() =>
            new GameObject("BonusObjectiveHUDCtrl_Test")
                .AddComponent<BonusObjectiveHUDController>();

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── MatchBonusObjectiveSOTests ────────────────────────────────────────

        [Test]
        public void FreshInstance_BonusTitle_IsDefault()
        {
            var so = CreateSO();
            Assert.AreEqual("Bonus Objective", so.BonusTitle,
                "Default BonusTitle must be 'Bonus Objective'.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_IsComplete_IsFalse()
        {
            var so = CreateSO();
            Assert.IsFalse(so.IsComplete,
                "IsComplete must be false on fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_IsExpired_IsFalse()
        {
            var so = CreateSO();
            Assert.IsFalse(so.IsExpired,
                "IsExpired must be false on fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_HasTimeLimit_WhenTimeLimitPositive()
        {
            var so = CreateSO();
            // Default _timeLimit is 60f > 0, so HasTimeLimit must be true
            Assert.IsTrue(so.HasTimeLimit,
                "HasTimeLimit must be true when _timeLimit > 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Complete_SetsIsComplete_AndFiresEvents()
        {
            var so          = CreateSO();
            var onCompleted = CreateVoidEvent();
            var onChanged   = CreateVoidEvent();
            SetField(so, "_onCompleted", onCompleted);
            SetField(so, "_onChanged",   onChanged);

            int completedCount = 0;
            int changedCount   = 0;
            onCompleted.RegisterCallback(() => completedCount++);
            onChanged.RegisterCallback(() => changedCount++);

            so.Complete();

            Assert.IsTrue(so.IsComplete, "IsComplete must be true after Complete().");
            Assert.AreEqual(1, completedCount, "_onCompleted must fire once.");
            Assert.AreEqual(1, changedCount,   "_onChanged must fire once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onCompleted);
            Object.DestroyImmediate(onChanged);
        }

        [Test]
        public void Complete_Idempotent()
        {
            var so        = CreateSO();
            var onChanged = CreateVoidEvent();
            SetField(so, "_onChanged", onChanged);

            int count = 0;
            onChanged.RegisterCallback(() => count++);

            so.Complete();
            so.Complete(); // second call must be no-op

            Assert.AreEqual(1, count,
                "Calling Complete() twice must fire _onChanged only once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onChanged);
        }

        [Test]
        public void Expire_SetsIsExpired_AndFiresEvents()
        {
            var so        = CreateSO();
            var onExpired = CreateVoidEvent();
            var onChanged = CreateVoidEvent();
            SetField(so, "_onExpired", onExpired);
            SetField(so, "_onChanged", onChanged);

            int expiredCount = 0;
            int changedCount = 0;
            onExpired.RegisterCallback(() => expiredCount++);
            onChanged.RegisterCallback(() => changedCount++);

            so.Expire();

            Assert.IsTrue(so.IsExpired, "IsExpired must be true after Expire().");
            Assert.AreEqual(1, expiredCount, "_onExpired must fire once.");
            Assert.AreEqual(1, changedCount, "_onChanged must fire once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onExpired);
            Object.DestroyImmediate(onChanged);
        }

        [Test]
        public void Expire_Idempotent()
        {
            var so        = CreateSO();
            var onChanged = CreateVoidEvent();
            SetField(so, "_onChanged", onChanged);

            int count = 0;
            onChanged.RegisterCallback(() => count++);

            so.Expire();
            so.Expire(); // second call must be no-op

            Assert.AreEqual(1, count,
                "Calling Expire() twice must fire _onChanged only once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onChanged);
        }

        [Test]
        public void Complete_BlocksExpire()
        {
            var so = CreateSO();
            so.Complete();
            so.Expire(); // must be no-op since already complete

            Assert.IsTrue(so.IsComplete, "IsComplete must remain true.");
            Assert.IsFalse(so.IsExpired, "IsExpired must remain false after Expire() on completed objective.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_DecrementsTimeRemaining()
        {
            var so = CreateSO();
            SetField(so, "_timeLimit", 60f);
            InvokePrivate(so, "OnEnable");
            float before = so.TimeRemaining;

            so.Tick(5f);

            Assert.Less(so.TimeRemaining, before,
                "Tick must decrement TimeRemaining.");
            Assert.AreEqual(before - 5f, so.TimeRemaining, 0.001f,
                "TimeRemaining must decrease by exactly the delta.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_AtZero_TriggersExpire()
        {
            var so        = CreateSO();
            var onExpired = CreateVoidEvent();
            SetField(so, "_timeLimit", 3f);
            SetField(so, "_onExpired", onExpired);
            InvokePrivate(so, "OnEnable");

            int expiredCount = 0;
            onExpired.RegisterCallback(() => expiredCount++);

            so.Tick(5f); // tick past 0

            Assert.IsTrue(so.IsExpired, "SO must expire when Tick reaches 0.");
            Assert.AreEqual(1, expiredCount, "_onExpired must fire when timer expires.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onExpired);
        }

        [Test]
        public void Reset_ClearsAll()
        {
            var so = CreateSO();
            SetField(so, "_timeLimit", 10f);
            InvokePrivate(so, "OnEnable");

            so.Complete();           // sets _isComplete = true
            so.Reset();

            Assert.IsFalse(so.IsComplete, "Reset must clear IsComplete.");
            Assert.IsFalse(so.IsExpired,  "Reset must clear IsExpired.");
            Assert.AreEqual(10f, so.TimeRemaining, 0.001f,
                "Reset must restore TimeRemaining to _timeLimit.");
            Object.DestroyImmediate(so);
        }

        // ── BonusObjectiveHUDControllerTests ─────────────────────────────────

        [Test]
        public void FreshInstance_BonusObjectiveNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.BonusObjective,
                "BonusObjective must be null when not assigned.");
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
            SetField(ctrl, "_onChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback should fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when BonusObjective is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WhenComplete_ShowsCompleteLabel()
        {
            var ctrl        = CreateController();
            var so          = CreateSO();
            var statusLabel = AddText(ctrl.gameObject, "StatusLabel");
            SetField(ctrl, "_bonusObjective", so);
            SetField(ctrl, "_statusLabel",    statusLabel);
            InvokePrivate(ctrl, "Awake");

            so.Complete();
            ctrl.Refresh();

            Assert.AreEqual("COMPLETE!", statusLabel.text,
                "Status label must show 'COMPLETE!' when objective is complete.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_WhenActive_ShowsActiveLabel()
        {
            var ctrl        = CreateController();
            var so          = CreateSO();
            var statusLabel = AddText(ctrl.gameObject, "StatusLabel");

            // Set no time limit so status shows "Active"
            SetField(so, "_timeLimit", 0f);
            InvokePrivate(so, "OnEnable");

            SetField(ctrl, "_bonusObjective", so);
            SetField(ctrl, "_statusLabel",    statusLabel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Active", statusLabel.text,
                "Status label must show 'Active' when no time limit and not complete/expired.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }
    }
}
