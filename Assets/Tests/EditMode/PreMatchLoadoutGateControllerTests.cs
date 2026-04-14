using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PreMatchLoadoutGateController"/> (T176).
    ///
    /// Tests cover:
    ///   OnEnable / OnDisable null-safety (×2).
    ///   Null channel combinations do not throw (×2).
    ///   OnDisable unregisters from _onLoadoutChanged.
    ///   OnDisable unregisters from _onPrestige.
    ///   Fresh instance public properties are null (×3).
    ///   Refresh with null loadout disables the Start Match button.
    ///   Refresh with null button does not throw.
    ///   Refresh with null validation panel does not throw.
    ///   Refresh with null error container does not throw.
    ///   Refresh with valid loadout enables the Start Match button.
    ///   Refresh with invalid loadout disables Start Match button.
    ///
    /// Total: 14 tests.
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

        private static Button CreateButton(GameObject parent)
        {
            var btnGO = new GameObject("Button");
            btnGO.transform.SetParent(parent.transform);
            return btnGO.AddComponent<Button>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Null-safety lifecycle tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_onLoadoutChanged", null);
            SetField(ctl, "_onPrestige",        null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_onLoadoutChanged", null);
            SetField(ctl, "_onPrestige",        null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Unsubscription tests
        // ══════════════════════════════════════════════════════════════════════

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
            channel.Raise();

            // Only our manual callback fires after unregister.
            Assert.AreEqual(1, callCount,
                "After OnDisable the controller must have unregistered from _onLoadoutChanged.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDisable_UnregistersFromOnPrestige()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PreMatchLoadoutGateController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctl, "_onPrestige", channel);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            InvokePrivate(ctl, "OnDisable");
            channel.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable the controller must have unregistered from _onPrestige.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Fresh-instance property tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void FreshInstance_Loadout_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.IsNull(ctl.Loadout, "Loadout should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_UnlockConfig_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.IsNull(ctl.UnlockConfig, "UnlockConfig should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_PrestigeSystem_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            Assert.IsNull(ctl.PrestigeSystem, "PrestigeSystem should default to null.");
            Object.DestroyImmediate(go);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Refresh button-state tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Refresh_NullLoadout_DisablesStartMatchButton()
        {
            var go     = new GameObject();
            var ctl    = go.AddComponent<PreMatchLoadoutGateController>();
            var button = CreateButton(go);
            button.interactable = true;
            SetField(ctl, "_startMatchButton", button);
            // No loadout → validator returns invalid immediately.
            ctl.Refresh();
            Assert.IsFalse(button.interactable,
                "Start Match button must be disabled when the loadout is null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullButton_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_startMatchButton", null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Refresh with null _startMatchButton must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullValidationPanel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_validationPanel", null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Refresh with null _validationPanel must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullErrorListContainer_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PreMatchLoadoutGateController>();
            SetField(ctl, "_errorListContainer", (Transform)null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Refresh with null _errorListContainer must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullRobotDefinition_DisablesButton()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PreMatchLoadoutGateController>();
            var button  = CreateButton(go);
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            button.interactable = true;
            SetField(ctl, "_startMatchButton", button);
            SetField(ctl, "_playerLoadout",    loadout);
            // RobotDefinition null → validator returns invalid.
            ctl.Refresh();
            Assert.IsFalse(button.interactable,
                "Start Match button must be disabled when RobotDefinition is null.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(loadout);
        }

        [Test]
        public void Refresh_ValidLoadout_NullCatalog_EnablesButton()
        {
            // Without catalog or robotDef restriction the validator runs rules 1+2 only.
            // A non-null loadout with null robotDef fails rule 1b (robotDef null).
            // We need a robotDef with no required slots to get "valid".
            var go      = new GameObject();
            var ctl     = go.AddComponent<PreMatchLoadoutGateController>();
            var button  = CreateButton(go);
            button.interactable = false;
            SetField(ctl, "_startMatchButton", button);

            // Provide a loadout and a robot definition.
            // With null catalog the only rules are null-guards (rules 1a+1b).
            // Both non-null → valid when equippedIds is non-null and robotDef non-null.
            var loadout  = ScriptableObject.CreateInstance<PlayerLoadout>();
            var robotDef = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(ctl, "_playerLoadout",    loadout);
            SetField(ctl, "_robotDefinition",  robotDef);

            ctl.Refresh();

            Assert.IsTrue(button.interactable,
                "Start Match button must be enabled when loadout + robotDef are non-null " +
                "and catalog is null (bypasses catalog/slot rules).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(robotDef);
        }
    }
}
