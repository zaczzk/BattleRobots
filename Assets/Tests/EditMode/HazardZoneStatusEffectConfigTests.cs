using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="HazardZoneStatusEffectConfig"/> and the
    /// corresponding patch to <see cref="HazardZoneController"/> that applies a
    /// status effect per damage tick.
    ///
    /// Covers:
    ///   HazardZoneStatusEffectConfig SO:
    ///     • StatusEffect property returns the assigned StatusEffectSO.
    ///     • HazardZone property returns the assigned HazardZoneSO.
    ///     • Both properties default to null when unassigned.
    ///
    ///   HazardZoneController patch:
    ///     • ProcessOverlap — on tick with config assigned, TriggerStatusEffect is called
    ///       (verified via StatusEffectController.ActiveEffectCount).
    ///     • ProcessOverlap — null _statusEffectConfig does not throw (no-op).
    ///     • ProcessOverlap — config with null StatusEffect does not throw (no-op).
    ///     • ProcessOverlap — below tick interval, no status effect is applied.
    ///     • ProcessOverlap — status effect is applied on each subsequent tick.
    /// </summary>
    public class HazardZoneStatusEffectConfigTests
    {
        // ── Shared fixtures ───────────────────────────────────────────────────

        private GameObject           _hazardGo;
        private HazardZoneController _controller;

        private GameObject            _robotGo;
        private DamageReceiver        _dr;
        private HealthSO              _health;
        private StatusEffectController _sec;

        private HazardZoneSO              _hazardConfig;
        private HazardZoneStatusEffectConfig _effectConfig;
        private StatusEffectSO            _burnEffect;

        private const int TargetId = 42;

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

        private HazardZoneSO MakeHazardConfig(float damage = 10f, float interval = 1f)
        {
            var cfg = ScriptableObject.CreateInstance<HazardZoneSO>();
            SetField(cfg, "_damagePerTick", damage);
            SetField(cfg, "_tickInterval",  interval);
            SetField(cfg, "_damageSourceId", "TestHazard");
            return cfg;
        }

        private StatusEffectSO MakeBurnEffect(float duration = 3f, float dps = 5f)
        {
            var effect = ScriptableObject.CreateInstance<StatusEffectSO>();
            SetField(effect, "_type",            StatusEffectType.Burn);
            SetField(effect, "_durationSeconds", duration);
            SetField(effect, "_damagePerSecond", dps);
            SetField(effect, "_displayName",     "Test Burn");
            return effect;
        }

        private static void InjectHealth(DamageReceiver dr, HealthSO health)
        {
            FieldInfo fi = typeof(DamageReceiver)
                .GetField("_health", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_health' not found on DamageReceiver.");
            fi.SetValue(dr, health);
        }

        private static void InjectStatusEffectController(DamageReceiver dr, StatusEffectController sec)
        {
            FieldInfo fi = typeof(DamageReceiver)
                .GetField("_statusEffectController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_statusEffectController' not found on DamageReceiver.");
            fi.SetValue(dr, sec);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Hazard zone controller
            _hazardGo   = new GameObject("HazardZone");
            _controller = _hazardGo.AddComponent<HazardZoneController>();

            _hazardConfig = MakeHazardConfig();
            SetField(_controller, "_config", _hazardConfig);

            // Status effect SO and config
            _burnEffect   = MakeBurnEffect();
            _effectConfig = ScriptableObject.CreateInstance<HazardZoneStatusEffectConfig>();
            SetField(_effectConfig, "_statusEffect", _burnEffect);
            SetField(_effectConfig, "_hazardZone",   _hazardConfig);

            // Robot with HealthSO and StatusEffectController
            _robotGo = new GameObject("Robot");
            _dr      = _robotGo.AddComponent<DamageReceiver>();
            _health  = ScriptableObject.CreateInstance<HealthSO>();
            _health.Reset();  // 100 HP
            InjectHealth(_dr, _health);

            _sec = _robotGo.AddComponent<StatusEffectController>();
            InjectStatusEffectController(_dr, _sec);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_hazardGo);
            Object.DestroyImmediate(_robotGo);
            Object.DestroyImmediate(_hazardConfig);
            Object.DestroyImmediate(_effectConfig);
            Object.DestroyImmediate(_burnEffect);
            Object.DestroyImmediate(_health);

            _hazardGo    = null;
            _controller  = null;
            _hazardConfig = null;
            _effectConfig = null;
            _burnEffect   = null;
            _robotGo      = null;
            _dr           = null;
            _health       = null;
            _sec          = null;
        }

        // ── HazardZoneStatusEffectConfig SO ───────────────────────────────────

        [Test]
        public void StatusEffect_DefaultsToNull()
        {
            var cfg = ScriptableObject.CreateInstance<HazardZoneStatusEffectConfig>();
            Assert.IsNull(cfg.StatusEffect, "StatusEffect should default to null.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void HazardZone_DefaultsToNull()
        {
            var cfg = ScriptableObject.CreateInstance<HazardZoneStatusEffectConfig>();
            Assert.IsNull(cfg.HazardZone, "HazardZone should default to null.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void StatusEffect_ReturnsAssignedEffect()
        {
            Assert.AreSame(_burnEffect, _effectConfig.StatusEffect,
                "StatusEffect property should return the assigned StatusEffectSO.");
        }

        [Test]
        public void HazardZone_ReturnsAssignedZone()
        {
            Assert.AreSame(_hazardConfig, _effectConfig.HazardZone,
                "HazardZone property should return the assigned HazardZoneSO.");
        }

        // ── HazardZoneController patch: status effect per tick ────────────────

        [Test]
        public void ProcessOverlap_OnTick_WithStatusEffectConfig_TriggersStatusEffect()
        {
            SetField(_controller, "_statusEffectConfig", _effectConfig);

            // Exactly one tick interval → tick fires and status effect is applied.
            _controller.ProcessOverlap(TargetId, _dr, 1f);

            Assert.AreEqual(1, _sec.ActiveEffectCount,
                "One Burn effect should be active after the first tick fires.");
        }

        [Test]
        public void ProcessOverlap_OnTick_NullStatusEffectConfig_DoesNotThrow()
        {
            // _statusEffectConfig defaults to null — must be a safe no-op.
            SetField(_controller, "_statusEffectConfig", null);

            Assert.DoesNotThrow(() => _controller.ProcessOverlap(TargetId, _dr, 1f),
                "Null _statusEffectConfig must not throw on tick.");
        }

        [Test]
        public void ProcessOverlap_OnTick_ConfigWithNullStatusEffect_DoesNotThrow()
        {
            var emptyConfig = ScriptableObject.CreateInstance<HazardZoneStatusEffectConfig>();
            // _statusEffect left null by default.
            SetField(_controller, "_statusEffectConfig", emptyConfig);

            Assert.DoesNotThrow(() => _controller.ProcessOverlap(TargetId, _dr, 1f),
                "Config with null StatusEffect must not throw on tick.");

            Object.DestroyImmediate(emptyConfig);
        }

        [Test]
        public void ProcessOverlap_BelowInterval_WithStatusEffectConfig_NoEffectApplied()
        {
            SetField(_controller, "_statusEffectConfig", _effectConfig);

            // 0.5s < 1s interval — no tick fires, so no status effect.
            _controller.ProcessOverlap(TargetId, _dr, 0.5f);

            Assert.AreEqual(0, _sec.ActiveEffectCount,
                "No status effect should be applied before the tick interval is reached.");
        }

        [Test]
        public void ProcessOverlap_StatusEffectAppliedOnEachTick()
        {
            SetField(_controller, "_statusEffectConfig", _effectConfig);

            // First tick.
            _controller.ProcessOverlap(TargetId, _dr, 1f);
            int afterFirstTick = _sec.ActiveEffectCount;

            // Second full tick — Burn already active so take-maximum stacking rule applies;
            // ActiveEffectCount stays at 1 (same slot refreshed, not a new slot).
            _controller.ProcessOverlap(TargetId, _dr, 1f);

            // Effect is still present — the stacking rule never removes an active effect
            // when the re-applied duration equals the original.
            Assert.AreEqual(afterFirstTick, _sec.ActiveEffectCount,
                "Burn slot should remain occupied (take-maximum stacking) after a second tick.");
        }

        [Test]
        public void ProcessOverlap_OnTick_NoStatusEffectConfig_DoesDamageOnly()
        {
            // No status config — damage fires, status effect count stays 0.
            SetField(_controller, "_statusEffectConfig", null);

            float healthBefore = _health.CurrentHealth;
            _controller.ProcessOverlap(TargetId, _dr, 1f);

            Assert.Less(_health.CurrentHealth, healthBefore,
                "Damage should still be applied when no status config is assigned.");
            Assert.AreEqual(0, _sec.ActiveEffectCount,
                "No status effect should be applied when _statusEffectConfig is null.");
        }
    }
}
