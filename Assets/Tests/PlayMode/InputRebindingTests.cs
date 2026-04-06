using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// PlayMode integration tests for T034 — input rebinding system.
    ///
    /// Complements the EditMode <c>KeyBindingsTests</c> by exercising the system
    /// in a running Player context, covering:
    ///   A. SettingsSO key-binding API — persistence round-trip via SaveSystem,
    ///      GetAllActionNames discovery, sequential SetBinding/LoadKeyBindings cycles.
    ///   B. RobotController integration — ReadAxis invoked via reflection, FixedUpdate
    ///      smoke tests, binding updates visible to an already-live controller.
    ///
    /// All MonoBehaviours and ScriptableObjects are destroyed in TearDown; the
    /// SaveSystem test file is cleaned up to keep tests hermetic.
    ///
    /// Private method/field access uses reflection (sealed components with serialized
    /// private fields cannot be written from a separate assembly without InternalsVisibleTo).
    /// </summary>
    [TestFixture]
    public sealed class InputRebindingTests
    {
        // ── Per-test fixtures ─────────────────────────────────────────────────

        private SettingsSO               _settings;
        private readonly List<GameObject>        _gameObjects = new List<GameObject>();
        private readonly List<ScriptableObject>  _soAssets    = new List<ScriptableObject>();

        // ── Reflection helpers ────────────────────────────────────────────────

        private static readonly BindingFlags k_Private =
            BindingFlags.NonPublic | BindingFlags.Instance;

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, k_Private);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, k_Private);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo mi = target.GetType().GetMethod(methodName, k_Private);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args);
        }

        private static T InvokeMethod<T>(object target, string methodName, params object[] args)
        {
            MethodInfo mi = target.GetType().GetMethod(methodName, k_Private);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            return (T)mi.Invoke(target, args);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<SettingsSO>();
            _soAssets.Add(_settings);
            SaveSystem.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _gameObjects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _gameObjects.Clear();

            foreach (ScriptableObject so in _soAssets)
            {
                if (so != null)
                    Object.DestroyImmediate(so);
            }
            _soAssets.Clear();

            SaveSystem.Delete();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Creates a RobotController on a plain GO; optionally wires a SettingsSO.</summary>
        private RobotController MakeController(SettingsSO settingsSO = null)
        {
            var go   = new GameObject("RobotController_Test");
            var ctrl = go.AddComponent<RobotController>();
            _gameObjects.Add(go);

            if (settingsSO != null)
                SetField(ctrl, "_settings", settingsSO);

            return ctrl;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Group A — SettingsSO key-binding API in a running Player context
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// After LoadKeyBindings(null) in a player context, all five default
        /// actions are accessible via GetBinding.
        /// </summary>
        [Test]
        public void SettingsSO_LoadKeyBindings_Null_AllDefaultsApplied_InPlayerContext()
        {
            _settings.LoadKeyBindings(null);

            Assert.AreEqual(KeyCode.W,     _settings.GetBinding("Forward"), "Forward");
            Assert.AreEqual(KeyCode.S,     _settings.GetBinding("Back"),    "Back");
            Assert.AreEqual(KeyCode.A,     _settings.GetBinding("Left"),    "Left");
            Assert.AreEqual(KeyCode.D,     _settings.GetBinding("Right"),   "Right");
            Assert.AreEqual(KeyCode.Space, _settings.GetBinding("Fire"),    "Fire");
        }

        /// <summary>
        /// GetAllActionNames returns a collection that contains all five default actions
        /// after LoadKeyBindings(null).
        /// </summary>
        [Test]
        public void SettingsSO_GetAllActionNames_ContainsAllDefaultActions()
        {
            _settings.LoadKeyBindings(null);

            IReadOnlyCollection<string> names = _settings.GetAllActionNames();

            Assert.IsNotNull(names, "GetAllActionNames must not return null.");
            Assert.GreaterOrEqual(names.Count, 5,
                "Expected at least 5 default actions (Forward, Back, Left, Right, Fire).");
            Assert.IsTrue(new List<string>(names).Contains("Forward"), "Forward");
            Assert.IsTrue(new List<string>(names).Contains("Back"),    "Back");
            Assert.IsTrue(new List<string>(names).Contains("Left"),    "Left");
            Assert.IsTrue(new List<string>(names).Contains("Right"),   "Right");
            Assert.IsTrue(new List<string>(names).Contains("Fire"),    "Fire");
        }

        /// <summary>
        /// Multiple sequential SetBinding calls on the same action overwrite
        /// each other — only the final value persists.
        /// </summary>
        [Test]
        public void SettingsSO_SetBinding_SequentialCalls_OnlyLastValuePersists()
        {
            _settings.LoadKeyBindings(null);

            _settings.SetBinding("Forward", KeyCode.UpArrow);
            _settings.SetBinding("Forward", KeyCode.Keypad8);
            _settings.SetBinding("Forward", KeyCode.I);

            Assert.AreEqual(KeyCode.I, _settings.GetBinding("Forward"),
                "Only the last SetBinding value should be active.");
        }

        /// <summary>
        /// BuildKeyBindings snapshot → LoadKeyBindings into a second instance
        /// round-trips all five default bindings plus a custom override.
        /// </summary>
        [Test]
        public void SettingsSO_BuildAndLoadKeyBindings_RoundTrip_InPlayerContext()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Forward", KeyCode.UpArrow);
            _settings.SetBinding("Left",    KeyCode.LeftArrow);

            KeyBindingsData snapshot = _settings.BuildKeyBindings();

            var restored = ScriptableObject.CreateInstance<SettingsSO>();
            _soAssets.Add(restored);
            restored.LoadKeyBindings(snapshot);

            Assert.AreEqual(KeyCode.UpArrow,   restored.GetBinding("Forward"), "Forward custom");
            Assert.AreEqual(KeyCode.LeftArrow,  restored.GetBinding("Left"),    "Left custom");
            Assert.AreEqual(KeyCode.S,          restored.GetBinding("Back"),    "Back default");
            Assert.AreEqual(KeyCode.D,          restored.GetBinding("Right"),   "Right default");
            Assert.AreEqual(KeyCode.Space,      restored.GetBinding("Fire"),    "Fire default");
        }

        /// <summary>
        /// Full SaveSystem round-trip in a player context:
        /// SetBinding → BuildKeyBindings → SaveSystem.Save → SaveSystem.Load
        /// → LoadKeyBindings → GetBinding returns persisted values.
        /// </summary>
        [Test]
        public void SettingsSO_SaveSystemRoundTrip_KeyBindingsPersisted_InPlayerContext()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Forward", KeyCode.UpArrow);
            _settings.SetBinding("Fire",    KeyCode.Return);

            // Write to disk.
            SaveData outgoing = SaveSystem.Load();
            outgoing.keyBindings = _settings.BuildKeyBindings();
            SaveSystem.Save(outgoing);

            // Restore from disk into a fresh SettingsSO.
            var fresh = ScriptableObject.CreateInstance<SettingsSO>();
            _soAssets.Add(fresh);
            SaveData incoming = SaveSystem.Load();
            fresh.LoadKeyBindings(incoming.keyBindings);

            Assert.AreEqual(KeyCode.UpArrow, fresh.GetBinding("Forward"), "Forward persisted");
            Assert.AreEqual(KeyCode.Return,  fresh.GetBinding("Fire"),    "Fire persisted");
            Assert.AreEqual(KeyCode.S,       fresh.GetBinding("Back"),    "Back default preserved");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Group B — RobotController integration with SettingsSO
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// RobotController.FixedUpdate runs without exception when a SettingsSO
        /// with default bindings is assigned. No wheel joints are wired (null-safe).
        /// </summary>
        [Test]
        public void RobotController_FixedUpdate_WithSettingsSO_NoException()
        {
            _settings.LoadKeyBindings(null);
            RobotController ctrl = MakeController(_settings);

            // Should not throw — null wheel joints are handled with ?. operators.
            Assert.DoesNotThrow(
                () => InvokeMethod(ctrl, "FixedUpdate"),
                "FixedUpdate must not throw when SettingsSO is assigned and no joints wired.");
        }

        /// <summary>
        /// RobotController.FixedUpdate runs without exception when no SettingsSO
        /// is assigned (falls back to legacy Input.GetAxis).
        /// </summary>
        [Test]
        public void RobotController_FixedUpdate_WithoutSettingsSO_NoException()
        {
            RobotController ctrl = MakeController(settingsSO: null);

            Assert.DoesNotThrow(
                () => InvokeMethod(ctrl, "FixedUpdate"),
                "FixedUpdate must not throw when SettingsSO is null.");
        }

        /// <summary>
        /// ReadAxis returns 0 when no physical key is held, regardless of which
        /// KeyCode bindings are configured. Verifies the method is reachable and
        /// does not throw with custom bindings wired.
        /// </summary>
        [Test]
        public void RobotController_ReadAxis_WithCustomBindings_ReturnsZeroWhenNoKeyHeld()
        {
            _settings.LoadKeyBindings(null);
            _settings.SetBinding("Forward", KeyCode.UpArrow);
            _settings.SetBinding("Back",    KeyCode.DownArrow);

            RobotController ctrl = MakeController(_settings);

            // Invoke private ReadAxis(positiveAction, negativeAction, fallbackAxis).
            float result = InvokeMethod<float>(ctrl, "ReadAxis",
                new object[] { "Forward", "Back", "Vertical" });

            // In test environment no key is held, so the result must be 0.
            Assert.AreEqual(0f, result, 0.001f,
                "ReadAxis must return 0 when no key is held in the test environment.");
        }

        /// <summary>
        /// When a binding is explicitly set to KeyCode.None for the positive direction,
        /// ReadAxis falls back to Input.GetAxis (which also returns 0 in tests).
        /// No exception must be thrown either way.
        /// </summary>
        [Test]
        public void RobotController_ReadAxis_WithNoneBinding_FallsBackToLegacyInput()
        {
            _settings.LoadKeyBindings(null);
            // Setting Forward to None forces ReadAxis to use the legacy Input.GetAxis path.
            _settings.SetBinding("Forward", KeyCode.None);

            RobotController ctrl = MakeController(_settings);

            Assert.DoesNotThrow(
                () => InvokeMethod<float>(ctrl, "ReadAxis",
                    new object[] { "Forward", "Back", "Vertical" }),
                "ReadAxis must not throw when the positive binding is KeyCode.None.");
        }

        /// <summary>
        /// A binding updated on the SettingsSO after the RobotController is already
        /// live is immediately visible via GetBinding — confirms the controller holds
        /// a live reference to the SO (not a snapshot).
        /// </summary>
        [Test]
        public void RobotController_BindingUpdatedAfterInit_LiveReferenceReflectsChange()
        {
            _settings.LoadKeyBindings(null);
            RobotController ctrl = MakeController(_settings);

            // Verify initial default.
            Assert.AreEqual(KeyCode.W, _settings.GetBinding("Forward"),
                "Pre-condition: Forward must default to W.");

            // Update the binding after the controller is already live.
            _settings.SetBinding("Forward", KeyCode.I);

            // The SO is held by reference; GetBinding must immediately reflect the change.
            SettingsSO liveSettings = GetField<SettingsSO>(ctrl, "_settings");
            Assert.IsNotNull(liveSettings, "Controller must have _settings reference.");
            Assert.AreEqual(KeyCode.I, liveSettings.GetBinding("Forward"),
                "Updated binding must be immediately visible via the controller's SO reference.");
        }

        /// <summary>
        /// RobotController.OnDisable (AllStop) does not throw when no joints are wired
        /// and a SettingsSO is assigned.
        /// </summary>
        [Test]
        public void RobotController_OnDisable_NoException()
        {
            _settings.LoadKeyBindings(null);
            RobotController ctrl = MakeController(_settings);
            GameObject go = ctrl.gameObject;

            Assert.DoesNotThrow(
                () => go.SetActive(false),
                "Disabling the controller must not throw (AllStop with null joints is null-safe).");
        }

        /// <summary>
        /// Calling FixedUpdate multiple times in sequence (simulating multiple game frames)
        /// with changing key bindings between calls does not throw.
        /// </summary>
        [Test]
        public void RobotController_FixedUpdate_MultipleCallsWithChangingBindings_NoException()
        {
            _settings.LoadKeyBindings(null);
            RobotController ctrl = MakeController(_settings);

            // Frame 1: default bindings
            InvokeMethod(ctrl, "FixedUpdate");

            // Change binding mid-run
            _settings.SetBinding("Forward", KeyCode.UpArrow);
            _settings.SetBinding("Back",    KeyCode.DownArrow);

            // Frame 2: updated bindings
            Assert.DoesNotThrow(
                () => InvokeMethod(ctrl, "FixedUpdate"),
                "FixedUpdate must not throw after bindings are changed mid-session.");
        }

        /// <summary>
        /// ApplySpeedBonus increases the controller's drive speed field, and FixedUpdate
        /// subsequently still operates without exception (no SettingsSO interaction broken
        /// by the bonus application).
        /// </summary>
        [Test]
        public void RobotController_ApplySpeedBonus_ThenFixedUpdate_NoException()
        {
            _settings.LoadKeyBindings(null);
            RobotController ctrl = MakeController(_settings);

            ctrl.ApplySpeedBonus(10f);

            Assert.DoesNotThrow(
                () => InvokeMethod(ctrl, "FixedUpdate"),
                "FixedUpdate must not throw after ApplySpeedBonus modifies the drive speed.");

            float speed = GetField<float>(ctrl, "_driveSpeedRadPerSec");
            Assert.Greater(speed, 0f, "Drive speed must be positive after ApplySpeedBonus.");
        }
    }
}
