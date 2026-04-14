using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T212:
    ///   <see cref="BuildGradeConfig"/> + <see cref="BuildGradeController"/>.
    ///
    /// BuildGradeTests (14):
    ///   BuildGradeConfig_GetGrade_SThreshold                              ×1
    ///   BuildGradeConfig_GetGrade_AThreshold                              ×1
    ///   BuildGradeConfig_GetGrade_BThreshold                              ×1
    ///   BuildGradeConfig_GetGrade_DGrade_BelowAll                         ×1
    ///   BuildGradeConfig_GetAdvice_FallsBackToDForUnknown                 ×1
    ///   FreshInstance_PlayerUpgradesNull                                   ×1
    ///   FreshInstance_PlayerLoadoutNull                                    ×1
    ///   FreshInstance_GradeConfigNull                                      ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters                                              ×1
    ///   ComputeAverageTier_NullLoadout_Zero                                ×1
    ///   ComputeAverageTier_MultipleParts_Average                           ×1
    ///   Refresh_NullPanel_DoesNotThrow                                     ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class BuildGradeTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static BuildGradeConfig CreateConfig()
        {
            var cfg = ScriptableObject.CreateInstance<BuildGradeConfig>();
            // Thresholds: S≥4, A≥3, B≥2, C≥1
            SetField(cfg, "_sThreshold", 4);
            SetField(cfg, "_aThreshold", 3);
            SetField(cfg, "_bThreshold", 2);
            SetField(cfg, "_cThreshold", 1);
            return cfg;
        }

        private static BuildGradeController CreateController() =>
            new GameObject("BuildGrade_Test").AddComponent<BuildGradeController>();

        // ── BuildGradeConfig tests ────────────────────────────────────────────

        [Test]
        public void BuildGradeConfig_GetGrade_SThreshold()
        {
            var cfg = CreateConfig();
            Assert.AreEqual("S", cfg.GetGrade(4f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void BuildGradeConfig_GetGrade_AThreshold()
        {
            var cfg = CreateConfig();
            Assert.AreEqual("A", cfg.GetGrade(3f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void BuildGradeConfig_GetGrade_BThreshold()
        {
            var cfg = CreateConfig();
            Assert.AreEqual("B", cfg.GetGrade(2f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void BuildGradeConfig_GetGrade_DGrade_BelowAll()
        {
            var cfg = CreateConfig();
            Assert.AreEqual("D", cfg.GetGrade(0f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void BuildGradeConfig_GetAdvice_FallsBackToDForUnknown()
        {
            var cfg = CreateConfig();
            // Unknown grade should return D advice (non-null, non-empty)
            string advice = cfg.GetAdvice("Z");
            Assert.IsFalse(string.IsNullOrEmpty(advice),
                "Unknown grade should return D-grade advice string.");
            Object.DestroyImmediate(cfg);
        }

        // ── BuildGradeController tests ────────────────────────────────────────

        [Test]
        public void FreshInstance_PlayerUpgradesNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PlayerUpgrades);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_PlayerLoadoutNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PlayerLoadout);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_GradeConfigNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.GradeConfig);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "After OnDisable only the manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void ComputeAverageTier_NullLoadout_Zero()
        {
            var ctrl = CreateController();
            var upg  = ScriptableObject.CreateInstance<PlayerPartUpgrades>();
            SetField(ctrl, "_playerUpgrades", upg);
            // _playerLoadout left null
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(0f, ctrl.ComputeAverageTier(), 0.001f);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(upg);
        }

        [Test]
        public void ComputeAverageTier_MultipleParts_Average()
        {
            var ctrl    = CreateController();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "a", "b", "c" });
            var upg = ScriptableObject.CreateInstance<PlayerPartUpgrades>();
            upg.SetTier("a", 2);
            upg.SetTier("b", 4);
            upg.SetTier("c", 0); // no upgrade
            SetField(ctrl, "_playerLoadout",  loadout);
            SetField(ctrl, "_playerUpgrades", upg);
            InvokePrivate(ctrl, "Awake");

            // (2 + 4 + 0) / 3 = 2
            Assert.AreEqual(2f, ctrl.ComputeAverageTier(), 0.001f);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(upg);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var ctrl = CreateController();
            // _gradePanel left null
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
