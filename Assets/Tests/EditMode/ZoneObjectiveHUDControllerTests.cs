using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T265: <see cref="ZoneObjectiveHUDController"/>.
    ///
    /// ZoneObjectiveHUDControllerTests (12):
    ///   FreshInstance_ObjectiveSO_Null                                  ×1
    ///   FreshInstance_DominanceSO_Null                                  ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channels                                  ×1
    ///   Refresh_NullObjectiveSO_HidesPanel                              ×1
    ///   Refresh_NullDominanceSO_HidesPanel                              ×1
    ///   Refresh_BothSOs_ShowsPanel                                      ×1
    ///   Refresh_ObjectiveLabel_ContainsRequired                         ×1
    ///   Refresh_ProgressLabel_ContainsHeldCount                         ×1
    ///   Refresh_IsComplete_ShowsBadge                                   ×1
    ///   ObjectiveComplete_Event_ShowsBadge                              ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneObjectiveHUDControllerTests
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

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneObjectiveSO CreateObjectiveSO() =>
            ScriptableObject.CreateInstance<ZoneObjectiveSO>();

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static ZoneObjectiveHUDController CreateController() =>
            new GameObject("ZoneObjHUD_Test").AddComponent<ZoneObjectiveHUDController>();

        private static GameObject CreatePanel() => new GameObject("Panel_Test");

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ObjectiveSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ObjectiveSO,
                "ObjectiveSO must be null on a fresh ZoneObjectiveHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_DominanceSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.DominanceSO,
                "DominanceSO must be null on a fresh ZoneObjectiveHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_Channels()
        {
            var ctrl        = CreateController();
            var objSO       = CreateObjectiveSO();
            var domSO       = CreateDominanceSO();
            var panel       = CreatePanel();
            var evtDom      = CreateEvent();
            var evtComplete = CreateEvent();

            SetField(ctrl, "_objectiveSO",         objSO);
            SetField(ctrl, "_dominanceSO",         domSO);
            SetField(ctrl, "_onDominanceChanged",  evtDom);
            SetField(ctrl, "_onObjectiveComplete", evtComplete);
            SetField(ctrl, "_panel",               panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Panel must not be shown by event after unsubscribe.
            panel.SetActive(false);
            evtDom.Raise();
            Assert.IsFalse(panel.activeSelf,
                "After OnDisable, _onDominanceChanged must not trigger Refresh.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(evtDom);
            Object.DestroyImmediate(evtComplete);
        }

        [Test]
        public void Refresh_NullObjectiveSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var domSO = CreateDominanceSO();
            var panel = CreatePanel();
            panel.SetActive(true);

            SetField(ctrl, "_dominanceSO", domSO);
            SetField(ctrl, "_panel",       panel);
            // _objectiveSO intentionally null.

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when ObjectiveSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullDominanceSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var objSO = CreateObjectiveSO();
            var panel = CreatePanel();
            panel.SetActive(true);

            SetField(ctrl, "_objectiveSO", objSO);
            SetField(ctrl, "_panel",       panel);
            // _dominanceSO intentionally null.

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when DominanceSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_BothSOs_ShowsPanel()
        {
            var ctrl  = CreateController();
            var objSO = CreateObjectiveSO();
            var domSO = CreateDominanceSO();
            var panel = CreatePanel();
            panel.SetActive(false);

            SetField(ctrl, "_objectiveSO", objSO);
            SetField(ctrl, "_dominanceSO", domSO);
            SetField(ctrl, "_panel",       panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh must show the panel when both SOs are assigned.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ObjectiveLabel_ContainsRequired()
        {
            var ctrl    = CreateController();
            var objSO   = CreateObjectiveSO();  // RequiredZones = 1 by default
            var domSO   = CreateDominanceSO();
            var goLabel = new GameObject("ObjLabel_Test");
            var label   = goLabel.AddComponent<Text>();

            SetField(ctrl, "_objectiveSO",   objSO);
            SetField(ctrl, "_dominanceSO",   domSO);
            SetField(ctrl, "_objectiveLabel", label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("1", label.text,
                "Objective label must contain the required zone count.");
            StringAssert.Contains("win", label.text,
                "Objective label must contain 'win'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(goLabel);
        }

        [Test]
        public void Refresh_ProgressLabel_ContainsHeldCount()
        {
            var ctrl    = CreateController();
            var objSO   = CreateObjectiveSO();
            var domSO   = CreateDominanceSO();
            var goLabel = new GameObject("ProgLabel_Test");
            var label   = goLabel.AddComponent<Text>();

            SetField(ctrl, "_objectiveSO",  objSO);
            SetField(ctrl, "_dominanceSO",  domSO);
            SetField(ctrl, "_progressLabel", label);

            domSO.AddPlayerZone();

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("1", label.text,
                "Progress label must contain the current held zone count.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(goLabel);
        }

        [Test]
        public void Refresh_IsComplete_ShowsBadge()
        {
            var ctrl   = CreateController();
            var objSO  = CreateObjectiveSO();
            var domSO  = CreateDominanceSO();
            var badge  = CreatePanel();
            badge.SetActive(false);

            SetField(ctrl, "_objectiveSO",  objSO);
            SetField(ctrl, "_dominanceSO",  domSO);
            SetField(ctrl, "_completeBadge", badge);

            domSO.AddPlayerZone();
            objSO.Evaluate(domSO.PlayerZoneCount);
            Assert.IsTrue(objSO.IsComplete, "Pre-condition: objective must be complete.");

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(badge.activeSelf,
                "Refresh must show _completeBadge when ZoneObjectiveSO.IsComplete is true.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(badge);
        }

        [Test]
        public void ObjectiveComplete_Event_ShowsBadge()
        {
            var ctrl        = CreateController();
            var objSO       = CreateObjectiveSO();
            var domSO       = CreateDominanceSO();
            var badge       = CreatePanel();
            var evtComplete = CreateEvent();

            badge.SetActive(false);
            SetField(ctrl, "_objectiveSO",         objSO);
            SetField(ctrl, "_dominanceSO",         domSO);
            SetField(ctrl, "_onObjectiveComplete", evtComplete);
            SetField(ctrl, "_completeBadge",       badge);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            evtComplete.Raise();

            Assert.IsTrue(badge.activeSelf,
                "_onObjectiveComplete event must activate _completeBadge.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objSO);
            Object.DestroyImmediate(domSO);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(evtComplete);
        }
    }
}
