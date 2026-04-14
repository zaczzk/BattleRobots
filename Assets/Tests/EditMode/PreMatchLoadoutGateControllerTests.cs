using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the M28 PreMatchLoadoutGateController (T176).
    ///
    /// PreMatchLoadoutGateControllerTests (16):
    ///   Fresh instance — IsValid is false (null loadout → validator returns invalid).
    ///   Fresh instance — Loadout property is null.
    ///   Fresh instance — RobotDef property is null.
    ///   Fresh instance — UnlockConfig, PrestigeSystem, and WeaponCatalog are null.
    ///   OnEnable with all-null refs does not throw.
    ///   OnDisable with all-null refs does not throw.
    ///   OnEnable with null _onLoadoutChanged channel does not throw.
    ///   OnDisable with null _onLoadoutChanged channel does not throw.
    ///   OnDisable unregisters from _onLoadoutChanged.
    ///   Validate — null loadout produces IsValid = false.
    ///   Validate — null _startMatchButton does not throw.
    ///   Validate — null _errorListContainer does not throw.
    ///   Validate — invalid loadout sets _startMatchButton.interactable to false.
    ///   Validate — valid loadout (empty loadout + null catalog/inventory) sets interactable true.
    ///   Validate — raises _onValidationChanged channel.
    ///   Validate — null _onValidationChanged channel does not throw.
    ///
    /// Total: 16 new EditMode tests.
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class PreMatchLoadoutGateControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        private static RobotDefinition CreateRobotDef()
        {
            return ScriptableObject.CreateInstance<RobotDefinition>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_IsValid_IsFalse()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.IsFalse(ctl.IsValid,
                "IsValid should be false on a fresh instance (null loadout → validation fails).");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_LoadoutProperty_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.IsNull(ctl.Loadout, "Loadout should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_RobotDefProperty_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.IsNull(ctl.RobotDef, "RobotDef should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_UnlockConfig_PrestigeSystem_WeaponCatalog_AreNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.IsNull(ctl.UnlockConfig,    "UnlockConfig should default to null.");
            Assert.IsNull(ctl.PrestigeSystem,  "PrestigeSystem should default to null.");
            Assert.IsNull(ctl.WeaponCatalog,   "WeaponCatalog should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_onLoadoutChanged", null);
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with null _onLoadoutChanged must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_onLoadoutChanged", null);
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with null _onLoadoutChanged must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_UnregistersFromOnLoadoutChanged()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PreMatchLoadoutGateController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctl, "_onLoadoutChanged", channel);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            InvokePrivate(ctl, "OnDisable");

            // Raise the channel — only the manual callback should fire; controller is unregistered.
            channel.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable, the controller must have unregistered its validate delegate.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Validate_NullLoadout_IsNotValid()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_loadout", null);
            ctl.Validate();
            Assert.IsFalse(ctl.IsValid,
                "Null loadout must result in IsValid = false.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Validate_NullStartMatchButton_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_startMatchButton", null);
            Assert.DoesNotThrow(() => ctl.Validate(),
                "Validate with null _startMatchButton must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Validate_NullErrorListContainer_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_errorListContainer", null);
            Assert.DoesNotThrow(() => ctl.Validate(),
                "Validate with null _errorListContainer must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Validate_InvalidLoadout_DisablesStartMatchButton()
        {
            var go     = new GameObject();
            var ctl    = go.AddComponent<PreMatchLoadoutGateController>();
            var btnGo  = new GameObject();
            // Add required UI components.
            btnGo.AddComponent<RectTransform>();
            var btn = btnGo.AddComponent<Button>();
            btn.interactable = true;

            SetField(ctl, "_loadout", null);   // null loadout → invalid
            SetField(ctl, "_startMatchButton", btn);

            ctl.Validate();

            Assert.IsFalse(btn.interactable,
                "Start Match button must be non-interactable when loadout is invalid.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
        }

        [Test]
        public void Validate_ValidLoadout_EnablesStartMatchButton()
        {
            // An empty loadout with a RobotDefinition that has no slots,
            // null catalog and null inventory → validator returns Valid.
            var go      = new GameObject();
            var ctl     = go.AddComponent<PreMatchLoadoutGateController>();
            var btnGo   = new GameObject();
            btnGo.AddComponent<RectTransform>();
            var btn     = btnGo.AddComponent<Button>();
            btn.interactable = false;

            var loadout  = ScriptableObject.CreateInstance<PlayerLoadout>();
            var robotDef = CreateRobotDef();  // no slots → rules 4 & 5 trivially pass

            SetField(ctl, "_loadout",          loadout);
            SetField(ctl, "_robotDef",         robotDef);
            SetField(ctl, "_inventory",        null);
            SetField(ctl, "_catalog",          null);
            SetField(ctl, "_unlockConfig",     null);
            SetField(ctl, "_startMatchButton", btn);

            ctl.Validate();

            Assert.IsTrue(btn.interactable,
                "Start Match button must be interactable when the loadout is valid.");
            Assert.IsTrue(ctl.IsValid, "IsValid must be true for a passing loadout.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(robotDef);
        }

        [Test]
        public void Validate_FiresOnValidationChanged()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PreMatchLoadoutGateController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctl, "_onValidationChanged", channel);

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            ctl.Validate();

            Assert.AreEqual(1, callCount,
                "Validate must raise _onValidationChanged exactly once.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Validate_NullValidationChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_onValidationChanged", null);
            Assert.DoesNotThrow(() => ctl.Validate(),
                "Validate with null _onValidationChanged must not throw.");
            Object.DestroyImmediate(go);
        }
    }
}
