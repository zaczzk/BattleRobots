using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="HazardZoneController"/>.
    ///
    /// All tests drive the hazard through the public
    /// <see cref="HazardZoneController.ProcessOverlap"/> and
    /// <see cref="HazardZoneController.ClearTracking"/> methods so no physics
    /// simulation is required.  Unity trigger callbacks (OnTriggerEnter/Stay/Exit)
    /// are not invoked — they are thin wrappers that call the same public methods.
    ///
    /// Covers:
    ///   • <see cref="HazardZoneController.IsActive"/> — default true; setter.
    ///   • <see cref="HazardZoneController.ProcessOverlap"/> null-safety, inactive
    ///     guard, time accumulation, tick fire, multi-interval, dead-target guard,
    ///     optional event, per-target independence.
    ///   • <see cref="HazardZoneController.ClearTracking"/> — resets per-target timer.
    /// </summary>
    public class HazardZoneControllerTests
    {
        // ── Shared fixtures ───────────────────────────────────────────────────

        private GameObject           _hazardGo;
        private HazardZoneController _controller;

        private GameObject     _robotGo;
        private DamageReceiver _dr;
        private HealthSO       _health;

        private HazardZoneSO _config;

        // Arbitrary stable IDs used as stand-ins for Collider.GetInstanceID()
        private const int TargetId  = 42;
        private const int TargetId2 = 99;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InjectHealth(DamageReceiver dr, HealthSO health)
        {
            FieldInfo fi = typeof(DamageReceiver)
                .GetField("_health", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_health' not found on DamageReceiver.");
            fi.SetValue(dr, health);
        }

        private HazardZoneSO MakeConfig(float damagePerTick = 10f, float tickInterval = 1f)
        {
            var cfg = ScriptableObject.CreateInstance<HazardZoneSO>();
            SetField(cfg, "_damagePerTick", damagePerTick);
            SetField(cfg, "_tickInterval",  tickInterval);
            SetField(cfg, "_damageSourceId", "TestHazard");
            return cfg;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Hazard
            _hazardGo   = new GameObject("HazardZone");
            _controller = _hazardGo.AddComponent<HazardZoneController>();

            // Config
            _config = MakeConfig();
            SetField(_controller, "_config", _config);

            // Robot with health
            _robotGo = new GameObject("Robot");
            _dr      = _robotGo.AddComponent<DamageReceiver>();
            _health  = ScriptableObject.CreateInstance<HealthSO>();
            _health.Reset();   // CurrentHealth = MaxHealth = 100
            InjectHealth(_dr, _health);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_hazardGo);
            Object.DestroyImmediate(_robotGo);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_health);

            _hazardGo   = null;
            _controller = null;
            _robotGo    = null;
            _dr         = null;
            _config     = null;
            _health     = null;
        }

        // ── IsActive ──────────────────────────────────────────────────────────

        [Test]
        public void IsActive_DefaultsTrue()
        {
            var go   = new GameObject("FreshHazard");
            var ctrl = go.AddComponent<HazardZoneController>();
            Assert.IsTrue(ctrl.IsActive);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void IsActive_Setter_CanDisable()
        {
            _controller.IsActive = false;
            Assert.IsFalse(_controller.IsActive);
        }

        [Test]
        public void IsActive_Setter_CanReEnable()
        {
            _controller.IsActive = false;
            _controller.IsActive = true;
            Assert.IsTrue(_controller.IsActive);
        }

        // ── ProcessOverlap null-safety ────────────────────────────────────────

        [Test]
        public void ProcessOverlap_NullConfig_DoesNotThrow()
        {
            SetField(_controller, "_config", null);
            Assert.DoesNotThrow(() => _controller.ProcessOverlap(TargetId, _dr, 2f));
        }

        [Test]
        public void ProcessOverlap_NullTarget_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _controller.ProcessOverlap(TargetId, null, 2f));
        }

        [Test]
        public void ProcessOverlap_NullConfig_NoDamage()
        {
            SetField(_controller, "_config", null);
            float before = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 2f);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f);
        }

        // ── ProcessOverlap inactive guard ─────────────────────────────────────

        [Test]
        public void ProcessOverlap_Inactive_NoDamage()
        {
            _controller.IsActive = false;
            float before = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 2f);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f);
        }

        // ── ProcessOverlap time accumulation ──────────────────────────────────

        [Test]
        public void ProcessOverlap_BelowInterval_NoDamage()
        {
            // interval = 1s; call with 0.5s — should not tick
            float before = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 0.5f);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ProcessOverlap_ExactInterval_AppliesDamage()
        {
            // interval = 1s, damage = 10 → health should drop by 10
            float before = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 1f);
            Assert.AreEqual(before - 10f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ProcessOverlap_ExceedsInterval_AppliesSingleTickDamage()
        {
            // Even if deltaTime > 2 intervals, only one tick fires per call
            // (remainder carries over to next call rather than multi-ticking).
            float before = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 1.8f);
            Assert.AreEqual(before - 10f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ProcessOverlap_AccumulatesAcrossMultipleCalls()
        {
            // Two calls of 0.6s each = 1.2s total → one tick after second call
            float before = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 0.6f);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f, "No tick after first call.");
            _controller.ProcessOverlap(TargetId, _dr, 0.6f);
            Assert.AreEqual(before - 10f, _health.CurrentHealth, 0.001f, "Tick after second call.");
        }

        [Test]
        public void ProcessOverlap_TwoFullIntervals_AppliesTwoTicks()
        {
            // Two separate calls each >= interval → two separate ticks
            float before = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 1f);
            _controller.ProcessOverlap(TargetId, _dr, 1f);
            Assert.AreEqual(before - 20f, _health.CurrentHealth, 0.001f);
        }

        // ── ProcessOverlap dead-target guard ──────────────────────────────────

        [Test]
        public void ProcessOverlap_DeadTarget_NoDamage()
        {
            // Kill the robot first
            _health.ApplyDamage(_health.MaxHealth);
            Assert.IsTrue(_dr.IsDead);

            float before = _health.CurrentHealth; // 0
            _controller.ProcessOverlap(TargetId, _dr, 2f);
            Assert.AreEqual(before, _health.CurrentHealth, 0.001f);
        }

        // ── ProcessOverlap optional event ─────────────────────────────────────

        [Test]
        public void ProcessOverlap_OnTick_FiresOptionalEvent()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_controller, "_onHazardTriggered", evt);

            int fireCount = 0;
            System.Action listener = () => fireCount++;
            evt.RegisterCallback(listener);

            _controller.ProcessOverlap(TargetId, _dr, 1f); // one tick
            Assert.AreEqual(1, fireCount);

            evt.UnregisterCallback(listener);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void ProcessOverlap_NullOptionalEvent_NoThrow()
        {
            SetField(_controller, "_onHazardTriggered", null);
            Assert.DoesNotThrow(() => _controller.ProcessOverlap(TargetId, _dr, 1f));
        }

        [Test]
        public void ProcessOverlap_BelowInterval_DoesNotFireEvent()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_controller, "_onHazardTriggered", evt);

            int fireCount = 0;
            System.Action listener = () => fireCount++;
            evt.RegisterCallback(listener);

            _controller.ProcessOverlap(TargetId, _dr, 0.3f); // below interval
            Assert.AreEqual(0, fireCount);

            evt.UnregisterCallback(listener);
            Object.DestroyImmediate(evt);
        }

        // ── ClearTracking ─────────────────────────────────────────────────────

        [Test]
        public void ClearTracking_ResetsAccumulator()
        {
            // Accumulate 0.8s (just below interval of 1s)
            _controller.ProcessOverlap(TargetId, _dr, 0.8f);
            float healthAfterAccumulate = _health.CurrentHealth;

            // Clear — resets accumulated time to 0
            _controller.ClearTracking(TargetId);

            // Another 0.8s should NOT tick (would require 1.0s from zero)
            _controller.ProcessOverlap(TargetId, _dr, 0.8f);
            Assert.AreEqual(healthAfterAccumulate, _health.CurrentHealth, 0.001f,
                "No tick expected after ClearTracking + 0.8s.");
        }

        [Test]
        public void ClearTracking_UnknownId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _controller.ClearTracking(9999));
        }

        // ── Per-target independence ────────────────────────────────────────────

        [Test]
        public void ProcessOverlap_DifferentTargetIds_TrackedIndependently()
        {
            // Second robot
            var go2     = new GameObject("Robot2");
            var dr2     = go2.AddComponent<DamageReceiver>();
            var health2 = ScriptableObject.CreateInstance<HealthSO>();
            health2.Reset();
            InjectHealth(dr2, health2);

            // Accumulate 0.6s for target1, 0.3s for target2
            _controller.ProcessOverlap(TargetId,  _dr,  0.6f);
            _controller.ProcessOverlap(TargetId2, dr2,  0.3f);

            float h1Before = _health.CurrentHealth;
            float h2Before = health2.CurrentHealth;

            // Another 0.6s for target1 → 1.2s total → tick
            _controller.ProcessOverlap(TargetId, _dr, 0.6f);
            Assert.AreEqual(h1Before - 10f, _health.CurrentHealth, 0.001f,
                "Target1 should have ticked.");
            Assert.AreEqual(h2Before, health2.CurrentHealth, 0.001f,
                "Target2 should be unaffected.");

            Object.DestroyImmediate(go2);
            Object.DestroyImmediate(health2);
        }
    }
}
