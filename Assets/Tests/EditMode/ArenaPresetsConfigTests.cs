using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ArenaPresetsConfig"/>.
    ///
    /// Covers:
    ///   • Fresh-instance invariants: Presets list not-null, empty, IReadOnlyList type.
    ///   • Population: one preset / two presets count correct.
    ///   • Insertion order is preserved.
    ///   • <see cref="ArenaPresetsConfig.ArenaPreset"/> default field values.
    ///
    /// All tests use <c>ScriptableObject.CreateInstance</c>; internal list injected
    /// via reflection following the same pattern as DifficultyPresetsConfigTests.
    /// </summary>
    public class ArenaPresetsConfigTests
    {
        private ArenaPresetsConfig _config;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetPresets(ArenaPresetsConfig target,
                                       List<ArenaPresetsConfig.ArenaPreset> presets)
        {
            FieldInfo fi = typeof(ArenaPresetsConfig)
                .GetField("_presets", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "Reflection: _presets not found on ArenaPresetsConfig.");
            fi.SetValue(target, presets);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ArenaPresetsConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
        }

        // ── Fresh-instance invariants ─────────────────────────────────────────

        [Test]
        public void FreshInstance_Presets_IsNotNull()
        {
            Assert.IsNotNull(_config.Presets,
                "Presets must never be null — empty list is acceptable, null is not.");
        }

        [Test]
        public void FreshInstance_Presets_IsEmpty()
        {
            Assert.AreEqual(0, _config.Presets.Count,
                "A freshly created ArenaPresetsConfig must start with an empty presets list.");
        }

        [Test]
        public void FreshInstance_Presets_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<System.Collections.Generic.IReadOnlyList<ArenaPresetsConfig.ArenaPreset>>(
                _config.Presets,
                "Presets must be exposed as IReadOnlyList<ArenaPreset> to prevent runtime mutation.");
        }

        // ── Population ────────────────────────────────────────────────────────

        [Test]
        public void WithOnePreset_Count_IsOne()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Factory" };
            SetPresets(_config, new List<ArenaPresetsConfig.ArenaPreset> { preset });

            Assert.AreEqual(1, _config.Presets.Count,
                "Presets.Count must equal 1 after injecting one entry.");
        }

        [Test]
        public void WithTwoPresets_Count_IsTwo()
        {
            var presets = new List<ArenaPresetsConfig.ArenaPreset>
            {
                new ArenaPresetsConfig.ArenaPreset { displayName = "Factory" },
                new ArenaPresetsConfig.ArenaPreset { displayName = "Wasteland" },
            };
            SetPresets(_config, presets);

            Assert.AreEqual(2, _config.Presets.Count,
                "Presets.Count must equal 2 after injecting two entries.");
        }

        [Test]
        public void PreservesInsertionOrder()
        {
            var presets = new List<ArenaPresetsConfig.ArenaPreset>
            {
                new ArenaPresetsConfig.ArenaPreset { displayName = "Alpha" },
                new ArenaPresetsConfig.ArenaPreset { displayName = "Beta" },
                new ArenaPresetsConfig.ArenaPreset { displayName = "Gamma" },
            };
            SetPresets(_config, presets);

            Assert.AreEqual("Alpha",  _config.Presets[0].displayName);
            Assert.AreEqual("Beta",   _config.Presets[1].displayName);
            Assert.AreEqual("Gamma",  _config.Presets[2].displayName);
        }

        // ── ArenaPreset default values ────────────────────────────────────────

        [Test]
        public void ArenaPreset_DefaultDisplayName_IsArena()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset();
            Assert.AreEqual("Arena", preset.displayName,
                "ArenaPreset default displayName must be 'Arena'.");
        }

        [Test]
        public void ArenaPreset_DefaultConfig_IsNull()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset();
            Assert.IsNull(preset.config,
                "ArenaPreset default config must be null (requires Editor assignment).");
        }

        [Test]
        public void ArenaPreset_DisplayName_CanBeSetViaField()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Colosseum" };
            Assert.AreEqual("Colosseum", preset.displayName,
                "ArenaPreset.displayName must accept arbitrary string values.");
        }
    }
}
