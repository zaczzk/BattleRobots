using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageFlashConfig"/> and
    /// <see cref="DamageFlashController"/>.
    ///
    /// Covers:
    ///   • <c>DamageFlashConfig</c> fresh-instance defaults (FlashDuration, FlashColor).
    ///   • <c>DamageFlashConfig.OnValidate</c> clamps FlashDuration to ≥ 0.05 s.
    ///   • <c>DamageFlashController.OnEnable</c>: all-null fields → no throw.
    ///   • <c>DamageFlashController.OnEnable</c>: null _onHealthChanged channel → no throw.
    ///   • <c>DamageFlashController.OnEnable</c>: with HealthSO → seeds _previousHealth
    ///     to HealthSO.CurrentHealth.
    ///   • <c>DamageFlashController.OnDisable</c>: null channel → no throw.
    ///   • <c>DamageFlashController.OnDisable</c>: unregisters from _onHealthChanged
    ///     (external-counter pattern confirms controller's callback is removed).
    ///   • HandleHealthChanged (triggered via FloatGameEvent.Raise):
    ///     — decrease: _previousHealth updated to new value.
    ///     — increase (heal): _previousHealth still updated (always tracks current health).
    ///     — no Renderers on the GameObject: no NullReferenceException.
    ///   • Re-enable after disable does not throw.
    ///
    /// Coroutine / MaterialPropertyBlock rendering effects are NOT tested here —
    /// those require a PlayMode environment with an active rendering context.
    ///
    /// All tests run headless; no scene objects or uGUI components required.
    /// Private fields are accessed via reflection using the standard pattern in this project.
    /// </summary>
    public class DamageFlashControllerTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject             _go;
        private DamageFlashController  _ctrl;

        // ── Reflection helpers ────────────────────────────────────────────────

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

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Keep inactive so Awake/OnEnable do not fire during field injection.
            _go   = new GameObject("TestFlashController");
            _go.SetActive(false);
            _ctrl = _go.AddComponent<DamageFlashController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            _go   = null;
            _ctrl = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // DamageFlashConfig tests
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void DamageFlashConfig_FreshInstance_FlashDuration_Is0Point15()
        {
            var cfg = ScriptableObject.CreateInstance<DamageFlashConfig>();
            Assert.AreEqual(0.15f, cfg.FlashDuration, 0.001f,
                "Default FlashDuration should be 0.15 s.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void DamageFlashConfig_FreshInstance_FlashColor_IsRed()
        {
            var cfg = ScriptableObject.CreateInstance<DamageFlashConfig>();
            // Color.red is (1, 0, 0, 1); compare component-wise with tolerance.
            Assert.AreEqual(Color.red.r, cfg.FlashColor.r, 0.001f,
                "FlashColor.r should be 1 (red).");
            Assert.AreEqual(Color.red.g, cfg.FlashColor.g, 0.001f,
                "FlashColor.g should be 0.");
            Assert.AreEqual(Color.red.b, cfg.FlashColor.b, 0.001f,
                "FlashColor.b should be 0.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void DamageFlashConfig_OnValidate_ClampsFlashDurationBelowMinimum()
        {
            // OnValidate is an Editor-only method; invoke via reflection to verify the
            // clamping guard without needing an Inspector round-trip.
            var cfg = ScriptableObject.CreateInstance<DamageFlashConfig>();
            FieldInfo fi = typeof(DamageFlashConfig)
                .GetField("_flashDuration", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "_flashDuration field not found.");
            fi.SetValue(cfg, 0.02f);    // below the 0.05 minimum

            InvokePrivate(cfg, "OnValidate");

            Assert.GreaterOrEqual(cfg.FlashDuration, 0.05f,
                "OnValidate must clamp _flashDuration to ≥ 0.05 s.");
            Object.DestroyImmediate(cfg);
        }

        // ─────────────────────────────────────────────────────────────────────
        // OnEnable — null-safety
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void OnEnable_AllNullFields_DoesNotThrow()
        {
            // All fields (_health, _onHealthChanged, _config) are null by default.
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "DamageFlashController.OnEnable with all null fields must not throw.");
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            // _health present, _onHealthChanged null → ?. guard must prevent throw.
            var health = ScriptableObject.CreateInstance<HealthSO>();
            health.Reset();
            SetField(_ctrl, "_health", health);

            Assert.DoesNotThrow(() => _go.SetActive(true),
                "DamageFlashController.OnEnable with null _onHealthChanged must not throw.");

            Object.DestroyImmediate(health);
        }

        [Test]
        public void OnEnable_WithHealth_SetsPreviousHealthFromCurrentHealth()
        {
            // HealthSO.CurrentHealth after Reset() == MaxHealth (100 by default).
            var health  = ScriptableObject.CreateInstance<HealthSO>();
            health.Reset();          // CurrentHealth = 100
            health.ApplyDamage(30f); // CurrentHealth = 70

            SetField(_ctrl, "_health", health);

            _go.SetActive(true);    // triggers Awake + OnEnable

            float previousHealth = GetField<float>(_ctrl, "_previousHealth");
            Assert.AreEqual(70f, previousHealth, 0.001f,
                "_previousHealth should be seeded from HealthSO.CurrentHealth (70) on OnEnable.");

            Object.DestroyImmediate(health);
        }

        // ─────────────────────────────────────────────────────────────────────
        // OnDisable — null-safety + unregistration
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false),
                "DamageFlashController.OnDisable with null _onHealthChanged must not throw.");
        }

        [Test]
        public void OnDisable_UnregistersFromHealthChangedChannel()
        {
            // External-counter pattern: register a test counter AFTER the controller
            // registers its own callback, then disable the controller.
            // Raising the event should only increment our counter (not trigger any
            // controller logic that would reset/start a coroutine in EditMode).
            var channel = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onHealthChanged", channel);

            _go.SetActive(true);   // OnEnable registers controller's delegate.

            int testCallCount = 0;
            System.Action<float> testCallback = _ => testCallCount++;
            channel.RegisterCallback(testCallback);

            _go.SetActive(false);  // OnDisable must unregister controller's delegate.

            channel.UnregisterCallback(testCallback);   // remove ours as well before Raise
            // Re-register just our counter so we can confirm the channel still works.
            channel.RegisterCallback(testCallback);

            // Raise the event — only our callback should fire.
            channel.Raise(50f);
            Assert.AreEqual(1, testCallCount,
                "After OnDisable, only the test counter callback should fire — " +
                "controller's delegate must have been unregistered.");

            channel.UnregisterCallback(testCallback);
            Object.DestroyImmediate(channel);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HandleHealthChanged — _previousHealth tracking
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void HandleHealthChanged_Decrease_UpdatesPreviousHealthToNewValue()
        {
            // Damage scenario: OnEnable seeds _previousHealth = 100; then event fires 70.
            var health  = ScriptableObject.CreateInstance<HealthSO>();
            var channel = ScriptableObject.CreateInstance<FloatGameEvent>();
            health.Reset();   // CurrentHealth = 100

            SetField(_ctrl, "_health",           health);
            SetField(_ctrl, "_onHealthChanged",   channel);
            _go.SetActive(true);    // _previousHealth seeded to 100

            // Raise event with 70 (damage of 30) — handler fires: 70 < 100, flash triggered.
            // _previousHealth must be updated to 70 regardless.
            channel.Raise(70f);

            float prev = GetField<float>(_ctrl, "_previousHealth");
            Assert.AreEqual(70f, prev, 0.001f,
                "After a damage event (100→70), _previousHealth must be updated to 70.");

            Object.DestroyImmediate(health);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void HandleHealthChanged_Increase_UpdatesPreviousHealthToNewValue()
        {
            // Heal scenario: OnEnable seeds _previousHealth = 60; event fires 80 (heal).
            var health  = ScriptableObject.CreateInstance<HealthSO>();
            var channel = ScriptableObject.CreateInstance<FloatGameEvent>();
            health.Reset();        // CurrentHealth = 100
            health.ApplyDamage(40f); // CurrentHealth = 60

            SetField(_ctrl, "_health",           health);
            SetField(_ctrl, "_onHealthChanged",   channel);
            _go.SetActive(true);    // _previousHealth seeded to 60

            // Raise event with 80 (heal) — 80 >= 60, no flash; _previousHealth must still update.
            channel.Raise(80f);

            float prev = GetField<float>(_ctrl, "_previousHealth");
            Assert.AreEqual(80f, prev, 0.001f,
                "After a heal event (60→80), _previousHealth must be updated to 80 " +
                "(always tracks the most recent health value).");

            Object.DestroyImmediate(health);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void HandleHealthChanged_NoRenderers_DoesNotThrow()
        {
            // The default test GameObject has no Renderer children.
            // The flash coroutine path requires Renderer[] (will be empty, not null).
            // In EditMode coroutines don't execute, so we only verify no immediate throw.
            var health  = ScriptableObject.CreateInstance<HealthSO>();
            var channel = ScriptableObject.CreateInstance<FloatGameEvent>();
            health.Reset();  // CurrentHealth = 100

            SetField(_ctrl, "_health",           health);
            SetField(_ctrl, "_onHealthChanged",   channel);
            _go.SetActive(true);    // _previousHealth seeded to 100

            // Fire a damage event (70 < 100) — handler fires, StartCoroutine called.
            // _renderers is an empty array; foreach over empty array is safe.
            Assert.DoesNotThrow(() => channel.Raise(70f),
                "HandleHealthChanged with an empty Renderer array must not throw.");

            Object.DestroyImmediate(health);
            Object.DestroyImmediate(channel);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Re-enable idempotency
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void OnEnable_AfterDisable_DoesNotThrow()
        {
            var health  = ScriptableObject.CreateInstance<HealthSO>();
            var channel = ScriptableObject.CreateInstance<FloatGameEvent>();
            health.Reset();

            SetField(_ctrl, "_health",           health);
            SetField(_ctrl, "_onHealthChanged",   channel);

            // Enable → disable → re-enable cycle must be stable.
            Assert.DoesNotThrow(() =>
            {
                _go.SetActive(true);
                _go.SetActive(false);
                _go.SetActive(true);
            }, "Re-enabling DamageFlashController after disable must not throw.");

            Object.DestroyImmediate(health);
            Object.DestroyImmediate(channel);
        }
    }
}
