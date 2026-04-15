using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T281: <see cref="ZoneTimerSO.SetCooldownDuration"/> patch,
    /// <see cref="ZoneTimerExtensionConfig"/>, and <see cref="ZoneTimerExtensionController"/>.
    ///
    /// ZoneTimerExtensionTests (12):
    ///   ZoneTimerSO_SetCooldownDuration_UpdatesDuration                    ×1
    ///   ZoneTimerSO_SetCooldownDuration_ClampsToMinimum                    ×1
    ///   Config_FreshInstance_DefaultCooldown_Five                          ×1
    ///   Config_FreshInstance_EntryCount_Zero                               ×1
    ///   Config_GetCooldownDuration_ValidIndex_ReturnsEntry                 ×1
    ///   Config_GetCooldownDuration_OutOfRange_ReturnsDefault               ×1
    ///   Config_GetCooldownDuration_NullArray_ReturnsDefault                ×1
    ///   Controller_FreshInstance_ConfigNull                                ×1
    ///   Controller_FreshInstance_TimerCount_Zero                           ×1
    ///   Controller_Apply_NullConfig_NoThrow                                ×1
    ///   Controller_Apply_NullTimers_NoThrow                                ×1
    ///   Controller_Apply_WithConfig_AppliesDurationsToTimers               ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneTimerExtensionTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static ZoneTimerSO CreateTimerSO() =>
            ScriptableObject.CreateInstance<ZoneTimerSO>();

        private static ZoneTimerExtensionConfig CreateConfig() =>
            ScriptableObject.CreateInstance<ZoneTimerExtensionConfig>();

        private static ZoneTimerExtensionController CreateController() =>
            new GameObject("ZoneTimerExtCtrl_Test")
                .AddComponent<ZoneTimerExtensionController>();

        // ── ZoneTimerSO patch tests ───────────────────────────────────────────

        [Test]
        public void ZoneTimerSO_SetCooldownDuration_UpdatesDuration()
        {
            var timer = CreateTimerSO();
            timer.SetCooldownDuration(8f);
            Assert.AreEqual(8f, timer.CooldownDuration, 0.001f,
                "SetCooldownDuration must update CooldownDuration.");
            Object.DestroyImmediate(timer);
        }

        [Test]
        public void ZoneTimerSO_SetCooldownDuration_ClampsToMinimum()
        {
            var timer = CreateTimerSO();
            timer.SetCooldownDuration(0f);   // below minimum
            Assert.AreEqual(0.1f, timer.CooldownDuration, 0.001f,
                "SetCooldownDuration must clamp values below 0.1 to 0.1.");
            Object.DestroyImmediate(timer);
        }

        // ── Config Tests ──────────────────────────────────────────────────────

        [Test]
        public void Config_FreshInstance_DefaultCooldown_Five()
        {
            var cfg = CreateConfig();
            Assert.AreEqual(5f, cfg.DefaultCooldown, 0.001f,
                "DefaultCooldown must default to 5.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_FreshInstance_EntryCount_Zero()
        {
            var cfg = CreateConfig();
            Assert.AreEqual(0, cfg.EntryCount,
                "EntryCount must be 0 on a fresh ZoneTimerExtensionConfig (no array).");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetCooldownDuration_ValidIndex_ReturnsEntry()
        {
            var cfg = CreateConfig();
            SetField(cfg, "_defaultCooldown", 5f);
            SetField(cfg, "_cooldownDurations", new float[] { 3f, 7f, 10f });

            Assert.AreEqual(3f,  cfg.GetCooldownDuration(0), 0.001f, "Index 0 must return 3.");
            Assert.AreEqual(7f,  cfg.GetCooldownDuration(1), 0.001f, "Index 1 must return 7.");
            Assert.AreEqual(10f, cfg.GetCooldownDuration(2), 0.001f, "Index 2 must return 10.");

            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetCooldownDuration_OutOfRange_ReturnsDefault()
        {
            var cfg = CreateConfig();
            SetField(cfg, "_defaultCooldown", 5f);
            SetField(cfg, "_cooldownDurations", new float[] { 3f });

            float result = cfg.GetCooldownDuration(99);

            Assert.AreEqual(5f, result, 0.001f,
                "GetCooldownDuration with an out-of-range index must return DefaultCooldown.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetCooldownDuration_NullArray_ReturnsDefault()
        {
            var cfg = CreateConfig();
            SetField(cfg, "_defaultCooldown", 5f);
            // _cooldownDurations left null (default)

            float result = cfg.GetCooldownDuration(0);

            Assert.AreEqual(5f, result, 0.001f,
                "GetCooldownDuration with a null array must return DefaultCooldown.");
            Object.DestroyImmediate(cfg);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ConfigNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Config,
                "Config must be null on a fresh ZoneTimerExtensionController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_TimerCount_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0, ctrl.TimerCount,
                "TimerCount must be 0 when no timers are assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Apply_NullConfig_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.Apply(),
                "Apply must not throw when _config is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Apply_NullTimers_NoThrow()
        {
            var ctrl = CreateController();
            var cfg  = CreateConfig();
            SetField(ctrl, "_config", cfg);
            // _timers left null

            Assert.DoesNotThrow(() => ctrl.Apply(),
                "Apply must not throw when _timers is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Controller_Apply_WithConfig_AppliesDurationsToTimers()
        {
            var ctrl  = CreateController();
            var cfg   = CreateConfig();
            var timerA = CreateTimerSO();
            var timerB = CreateTimerSO();

            SetField(cfg, "_defaultCooldown",   5f);
            SetField(cfg, "_cooldownDurations", new float[] { 2f, 9f });
            SetField(ctrl, "_config", cfg);
            SetField(ctrl, "_timers", new ZoneTimerSO[] { timerA, timerB });

            ctrl.Apply();

            Assert.AreEqual(2f, timerA.CooldownDuration, 0.001f,
                "Apply must set timerA.CooldownDuration to config entry 0 (2s).");
            Assert.AreEqual(9f, timerB.CooldownDuration, 0.001f,
                "Apply must set timerB.CooldownDuration to config entry 1 (9s).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(timerA);
            Object.DestroyImmediate(timerB);
        }
    }
}
