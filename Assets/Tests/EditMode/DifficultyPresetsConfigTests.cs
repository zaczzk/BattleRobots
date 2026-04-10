using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DifficultyPresetsConfig"/> ScriptableObject
    /// and its nested <see cref="DifficultyPresetsConfig.DifficultyPreset"/> data class.
    ///
    /// Covers:
    ///   • Fresh-instance: Presets list is non-null and empty.
    ///   • Presets exposes <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.
    ///   • DifficultyPreset defaults: displayName "Normal", config null.
    ///   • Count accuracy after injecting one / two preset entries via reflection.
    ///   • Content fidelity: injected displayName and config are returned correctly.
    ///   • Null entry in the list: Count reflects the null entry (OnValidate warns,
    ///     but the list itself does not throw on access).
    ///
    /// No scene or AssetDatabase required — CreateInstance and plain-object
    /// construction are sufficient.
    /// </summary>
    public class DifficultyPresetsConfigTests
    {
        private DifficultyPresetsConfig _config;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static DifficultyPresetsConfig.DifficultyPreset MakePreset(
            string name, BotDifficultyConfig cfg = null) =>
            new DifficultyPresetsConfig.DifficultyPreset { displayName = name, config = cfg };

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DifficultyPresetsConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
        }

        // ── Fresh-instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Presets_IsNotNull()
        {
            Assert.IsNotNull(_config.Presets);
        }

        [Test]
        public void FreshInstance_Presets_IsEmpty()
        {
            Assert.AreEqual(0, _config.Presets.Count);
        }

        [Test]
        public void Presets_ImplementsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<DifficultyPresetsConfig.DifficultyPreset>>(
                _config.Presets);
        }

        // ── DifficultyPreset defaults ─────────────────────────────────────────

        [Test]
        public void DifficultyPreset_DefaultDisplayName_IsNormal()
        {
            var preset = new DifficultyPresetsConfig.DifficultyPreset();
            Assert.AreEqual("Normal", preset.displayName);
        }

        [Test]
        public void DifficultyPreset_DefaultConfig_IsNull()
        {
            var preset = new DifficultyPresetsConfig.DifficultyPreset();
            Assert.IsNull(preset.config);
        }

        // ── Count after injection ─────────────────────────────────────────────

        [Test]
        public void WithOnePreset_Count_IsOne()
        {
            SetField(_config, "_presets",
                new List<DifficultyPresetsConfig.DifficultyPreset> { MakePreset("Easy") });

            Assert.AreEqual(1, _config.Presets.Count);
        }

        [Test]
        public void WithTwoPresets_Count_IsTwo()
        {
            SetField(_config, "_presets",
                new List<DifficultyPresetsConfig.DifficultyPreset>
                {
                    MakePreset("Easy"),
                    MakePreset("Hard")
                });

            Assert.AreEqual(2, _config.Presets.Count);
        }

        // ── Content fidelity ──────────────────────────────────────────────────

        [Test]
        public void WithOnePreset_DisplayName_IsAccessible()
        {
            SetField(_config, "_presets",
                new List<DifficultyPresetsConfig.DifficultyPreset> { MakePreset("Hard") });

            Assert.AreEqual("Hard", _config.Presets[0].displayName);
        }

        [Test]
        public void WithOnePreset_Config_IsRetained()
        {
            var cfg = ScriptableObject.CreateInstance<BotDifficultyConfig>();
            SetField(_config, "_presets",
                new List<DifficultyPresetsConfig.DifficultyPreset> { MakePreset("Normal", cfg) });

            Assert.AreEqual(cfg, _config.Presets[0].config);

            Object.DestroyImmediate(cfg);
        }

        // ── Null entry tolerance ──────────────────────────────────────────────

        [Test]
        public void WithNullPresetEntry_Count_IsOne_AccessDoesNotThrow()
        {
            // A null DifficultyPreset entry in the list is allowed (OnValidate warns).
            // Accessing Presets.Count must not throw.
            var list = new List<DifficultyPresetsConfig.DifficultyPreset> { null };
            SetField(_config, "_presets", list);

            Assert.DoesNotThrow(() =>
            {
                int count = _config.Presets.Count;
                Assert.AreEqual(1, count);
            });
        }
    }
}
