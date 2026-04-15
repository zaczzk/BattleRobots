using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T263: <see cref="ZoneDominanceBonusHUDController"/>.
    ///
    /// ZoneDominanceBonusHUDControllerTests (12):
    ///   FreshInstance_DominanceSO_Null                                  ×1
    ///   FreshInstance_ScoreMultiplier_Null                              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channel                                   ×1
    ///   Refresh_NullSO_HidesPanel                                       ×1
    ///   Refresh_WithSO_ShowsPanel                                       ×1
    ///   Refresh_HasDominance_ShowsBonusPanel                            ×1
    ///   Refresh_NoDominance_HidesBonusPanel                             ×1
    ///   Refresh_StatusLabel_HasDominance                                ×1
    ///   Refresh_StatusLabel_NoDominance                                 ×1
    ///   OnEnable_CallsRefresh                                           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneDominanceBonusHUDControllerTests
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

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static ZoneDominanceBonusHUDController CreateController() =>
            new GameObject("ZoneDomBonusHUD_Test").AddComponent<ZoneDominanceBonusHUDController>();

        private static GameObject CreatePanel() => new GameObject("Panel_Test");

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_DominanceSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.DominanceSO,
                "DominanceSO must be null on a fresh ZoneDominanceBonusHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_ScoreMultiplier_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ScoreMultiplier,
                "ScoreMultiplier must be null on a fresh ZoneDominanceBonusHUDController.");
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
        public void OnDisable_Unregisters_Channel()
        {
            var ctrl  = CreateController();
            var so    = CreateDominanceSO();
            var panel = CreatePanel();
            var evt   = CreateEvent();

            SetField(ctrl, "_dominanceSO",        so);
            SetField(ctrl, "_onDominanceChanged", evt);
            SetField(ctrl, "_panel",              panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Confirm panel is visible after OnEnable.
            Assert.IsTrue(panel.activeSelf, "Pre-condition: panel should be active after OnEnable.");

            InvokePrivate(ctrl, "OnDisable");

            // Event must NOT trigger Refresh after unsubscribe.
            panel.SetActive(false);
            evt.Raise();
            Assert.IsFalse(panel.activeSelf,
                "After OnDisable, _onDominanceChanged must not trigger Refresh.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when DominanceSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateDominanceSO();
            var panel = CreatePanel();
            panel.SetActive(false);
            SetField(ctrl, "_dominanceSO", so);
            SetField(ctrl, "_panel",       panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh must show the panel when DominanceSO is assigned.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_HasDominance_ShowsBonusPanel()
        {
            var ctrl       = CreateController();
            var so         = CreateDominanceSO();
            var bonusPanel = CreatePanel();
            bonusPanel.SetActive(false);

            SetField(ctrl, "_dominanceSO", so);
            SetField(ctrl, "_bonusPanel",  bonusPanel);

            so.AddPlayerZone();
            so.AddPlayerZone();
            Assert.IsTrue(so.HasDominance, "Pre-condition: player must have dominance.");

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(bonusPanel.activeSelf,
                "Refresh must show _bonusPanel when player has zone dominance.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(bonusPanel);
        }

        [Test]
        public void Refresh_NoDominance_HidesBonusPanel()
        {
            var ctrl       = CreateController();
            var so         = CreateDominanceSO();
            var bonusPanel = CreatePanel();
            bonusPanel.SetActive(true);

            SetField(ctrl, "_dominanceSO", so);
            SetField(ctrl, "_bonusPanel",  bonusPanel);

            Assert.IsFalse(so.HasDominance, "Pre-condition: player must not have dominance.");

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(bonusPanel.activeSelf,
                "Refresh must hide _bonusPanel when player lacks zone dominance.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(bonusPanel);
        }

        [Test]
        public void Refresh_StatusLabel_HasDominance()
        {
            var ctrl  = CreateController();
            var so    = CreateDominanceSO();
            var goLabel = new GameObject("Label_Test");
            var label = goLabel.AddComponent<Text>();

            SetField(ctrl, "_dominanceSO",  so);
            SetField(ctrl, "_statusLabel", label);

            so.AddPlayerZone();
            so.AddPlayerZone();
            Assert.IsTrue(so.HasDominance, "Pre-condition: player must have dominance.");

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual("Dominance Bonus Active!", label.text,
                "Status label must read 'Dominance Bonus Active!' when player has dominance.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(goLabel);
        }

        [Test]
        public void Refresh_StatusLabel_NoDominance()
        {
            var ctrl    = CreateController();
            var so      = CreateDominanceSO();
            var goLabel = new GameObject("Label_Test");
            var label   = goLabel.AddComponent<Text>();

            SetField(ctrl, "_dominanceSO",  so);
            SetField(ctrl, "_statusLabel", label);

            Assert.IsFalse(so.HasDominance, "Pre-condition: player must not have dominance.");

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual("No Dominance", label.text,
                "Status label must read 'No Dominance' when player lacks dominance.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(goLabel);
        }

        [Test]
        public void OnEnable_CallsRefresh()
        {
            var ctrl  = CreateController();
            var so    = CreateDominanceSO();
            var panel = CreatePanel();
            panel.SetActive(false);

            SetField(ctrl, "_dominanceSO", so);
            SetField(ctrl, "_panel",       panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            Assert.IsTrue(panel.activeSelf,
                "OnEnable must call Refresh, which shows the panel when SO is assigned.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }
    }
}
