using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T222:
    ///   <see cref="InMatchObjectiveSO"/> and <see cref="InMatchObjectiveController"/>.
    ///
    /// InMatchObjectiveSOTests (8):
    ///   FreshInstance_DefaultTargetCount_Is1              ×1
    ///   FreshInstance_CurrentCount_IsZero                 ×1
    ///   FreshInstance_IsComplete_False                    ×1
    ///   Increment_IncreasesCurrentCount                   ×1
    ///   Increment_Fires_OnProgressChanged                 ×1
    ///   Increment_WhenComplete_Fires_OnObjectiveComplete  ×1
    ///   Increment_AfterComplete_NoOp                      ×1
    ///   Reset_SetsCurrentCountToZero                      ×1
    ///
    /// InMatchObjectiveControllerTests (6):
    ///   FreshInstance_ObjectiveNull                       ×1
    ///   OnEnable_NullRefs_DoesNotThrow                    ×1
    ///   OnDisable_Unregisters                             ×1
    ///   Refresh_NullObjective_HidesPanel                  ×1
    ///   Refresh_SetsProgressLabel                         ×1
    ///   Refresh_Complete_ShowsCompleteLabel               ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class InMatchObjectiveTests
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

        private static InMatchObjectiveSO CreateObjectiveSO(int targetCount = 1,
                                                              string title = "Objective")
        {
            var so = ScriptableObject.CreateInstance<InMatchObjectiveSO>();
            SetField(so, "_targetCount",    targetCount);
            SetField(so, "_objectiveTitle", title);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static InMatchObjectiveController CreateController() =>
            new GameObject("ObjectiveCtrl_Test").AddComponent<InMatchObjectiveController>();

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

        // ── InMatchObjectiveSOTests ───────────────────────────────────────────

        [Test]
        public void FreshInstance_DefaultTargetCount_Is1()
        {
            var so = ScriptableObject.CreateInstance<InMatchObjectiveSO>();
            Assert.AreEqual(1, so.TargetCount);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_CurrentCount_IsZero()
        {
            var so = CreateObjectiveSO();
            Assert.AreEqual(0, so.CurrentCount);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_IsComplete_False()
        {
            var so = CreateObjectiveSO(targetCount: 3);
            Assert.IsFalse(so.IsComplete);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Increment_IncreasesCurrentCount()
        {
            var so = CreateObjectiveSO(targetCount: 5);
            so.Increment();
            Assert.AreEqual(1, so.CurrentCount);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Increment_Fires_OnProgressChanged()
        {
            var so  = CreateObjectiveSO(targetCount: 5);
            var evt = CreateVoidEvent();
            SetField(so, "_onProgressChanged", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.Increment();

            Assert.AreEqual(1, count, "_onProgressChanged should fire on Increment.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Increment_WhenComplete_Fires_OnObjectiveComplete()
        {
            var so      = CreateObjectiveSO(targetCount: 1);
            var compEvt = CreateVoidEvent();
            SetField(so, "_onObjectiveComplete", compEvt);

            int count = 0;
            compEvt.RegisterCallback(() => count++);
            so.Increment(); // CurrentCount == TargetCount → complete

            Assert.AreEqual(1, count, "_onObjectiveComplete should fire once on completion.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(compEvt);
        }

        [Test]
        public void Increment_AfterComplete_NoOp()
        {
            var so = CreateObjectiveSO(targetCount: 1);
            so.Increment(); // completes
            so.Increment(); // should be no-op

            Assert.AreEqual(1, so.CurrentCount,
                "Increment after completion should not increase CurrentCount.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Reset_SetsCurrentCountToZero()
        {
            var so = CreateObjectiveSO(targetCount: 5);
            so.Increment();
            so.Increment();
            so.Reset();

            Assert.AreEqual(0, so.CurrentCount, "Reset should set CurrentCount to zero.");
            Object.DestroyImmediate(so);
        }

        // ── InMatchObjectiveControllerTests ───────────────────────────────────

        [Test]
        public void FreshInstance_ObjectiveNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Objective);
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
            SetField(ctrl, "_onProgressChanged", ch);
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
        public void Refresh_NullObjective_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_objectivePanel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh(); // _objective is null

            Assert.IsFalse(panel.activeSelf,
                "Objective panel should be hidden when no objective is assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_SetsProgressLabel()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "progress");
            var so    = CreateObjectiveSO(targetCount: 5);
            so.Increment();
            so.Increment(); // CurrentCount = 2

            SetField(ctrl, "_objective",      so);
            SetField(ctrl, "_progressLabel",  label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            StringAssert.Contains("2", label.text, "Progress label should contain the current count.");
            StringAssert.Contains("5", label.text, "Progress label should contain the target count.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_Complete_ShowsCompleteLabel()
        {
            var ctrl          = CreateController();
            var completeLabel = AddText(ctrl.gameObject, "complete");
            completeLabel.gameObject.SetActive(false);
            var so = CreateObjectiveSO(targetCount: 1);
            so.Increment(); // complete

            SetField(ctrl, "_objective",     so);
            SetField(ctrl, "_completeLabel", completeLabel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(completeLabel.gameObject.activeSelf,
                "Complete label should be shown when the objective is complete.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }
    }
}
