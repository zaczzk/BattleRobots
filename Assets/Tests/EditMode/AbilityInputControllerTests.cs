using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AbilityInputController"/>.
    ///
    /// Note: <c>Input.GetKeyDown</c> always returns false in EditMode, so tests
    /// focus on null-safety, default field values, and Update no-throw guarantees.
    /// The live activation path (key pressed → TryActivate → optional fail event)
    /// is validated in conjunction with AbilityController's existing test suite.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all null refs — no throw.
    ///   • Update with null _abilityController — no throw.
    ///   • Update with null _onInputBlocked — no throw.
    ///   • Default _activationKey is KeyCode.Space.
    ///   • _activationKey can be changed via inspector (reflection).
    ///   • Default _abilityController is null.
    ///   • Default _onInputBlocked is null.
    ///   • Update with assigned AbilityController (but no key pressed) — no throw.
    ///   • Multiple Update calls — no throw.
    ///   • OnEnable with null channel — no throw (no channels exist on this MB).
    /// </summary>
    public class AbilityInputControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static void InvokeUpdate(AbilityInputController mb)
        {
            MethodInfo mi = typeof(AbilityInputController)
                .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, "Update method not found on AbilityInputController.");
            mi.Invoke(mb, null);
        }

        private static AbilityInputController MakeController(out GameObject go)
        {
            go = new GameObject("AbilityInputControllerTest");
            go.SetActive(false); // prevent OnEnable before wiring
            return go.AddComponent<AbilityInputController>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── Update — null guards ──────────────────────────────────────────────

        [Test]
        public void Update_NullAbilityController_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var mb = go.GetComponent<AbilityInputController>();
            // _abilityController remains null
            Assert.DoesNotThrow(() => InvokeUpdate(mb),
                "Update must not throw when _abilityController is null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Update_NullOnInputBlocked_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var mb     = go.GetComponent<AbilityInputController>();
            var ability = ScriptableObject.CreateInstance<PartAbilitySO>();
            SetField(ability, "_abilityId",   "test");
            SetField(ability, "_energyCost",  0f);
            SetField(ability, "_cooldown",    0f);

            var abilityGO         = new GameObject("AbilityCtrl");
            var abilityController = abilityGO.AddComponent<AbilityController>();
            SetField(abilityController, "_ability", ability);
            SetField(mb, "_abilityController", abilityController);
            // _onInputBlocked remains null

            Assert.DoesNotThrow(() => InvokeUpdate(mb),
                "Update must not throw when _onInputBlocked is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(abilityGO);
            Object.DestroyImmediate(ability);
        }

        // ── Default field values ──────────────────────────────────────────────

        [Test]
        public void DefaultActivationKey_IsSpace()
        {
            MakeController(out GameObject go);
            var mb  = go.GetComponent<AbilityInputController>();
            var key = GetField<KeyCode>(mb, "_activationKey");
            Object.DestroyImmediate(go);
            Assert.AreEqual(KeyCode.Space, key,
                "Default _activationKey must be KeyCode.Space.");
        }

        [Test]
        public void ActivationKey_CanBeSetAndRead()
        {
            MakeController(out GameObject go);
            var mb = go.GetComponent<AbilityInputController>();
            SetField(mb, "_activationKey", KeyCode.Q);
            var key = GetField<KeyCode>(mb, "_activationKey");
            Object.DestroyImmediate(go);
            Assert.AreEqual(KeyCode.Q, key,
                "_activationKey must reflect the value set via inspector/reflection.");
        }

        [Test]
        public void FreshInstance_NullAbilityController_FieldIsNull()
        {
            MakeController(out GameObject go);
            var mb    = go.GetComponent<AbilityInputController>();
            var value = GetField<AbilityController>(mb, "_abilityController");
            Object.DestroyImmediate(go);
            Assert.IsNull(value,
                "_abilityController must default to null.");
        }

        [Test]
        public void FreshInstance_NullOnInputBlocked_FieldIsNull()
        {
            MakeController(out GameObject go);
            var mb    = go.GetComponent<AbilityInputController>();
            var value = GetField<VoidGameEvent>(mb, "_onInputBlocked");
            Object.DestroyImmediate(go);
            Assert.IsNull(value,
                "_onInputBlocked must default to null.");
        }

        // ── Update with controller assigned ───────────────────────────────────

        [Test]
        public void Update_WithAbilityControllerAssigned_NoThrow()
        {
            MakeController(out GameObject go);
            var mb            = go.GetComponent<AbilityInputController>();
            var abilityGO     = new GameObject("AC");
            var abilityCtrl   = abilityGO.AddComponent<AbilityController>();
            SetField(mb, "_abilityController", abilityCtrl);
            // Key is not pressed in EditMode, so TryActivate is never called

            Assert.DoesNotThrow(() => InvokeUpdate(mb),
                "Update with a non-null AbilityController must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(abilityGO);
        }

        // ── Multiple Update calls ─────────────────────────────────────────────

        [Test]
        public void Update_MultipleCallsNullController_DoNotThrow()
        {
            MakeController(out GameObject go);
            var mb = go.GetComponent<AbilityInputController>();

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 5; i++)
                    InvokeUpdate(mb);
            }, "Multiple Update calls with null controller must not throw.");

            Object.DestroyImmediate(go);
        }
    }
}
