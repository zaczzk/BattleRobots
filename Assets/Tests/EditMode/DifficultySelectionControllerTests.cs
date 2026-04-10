using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DifficultySelectionController"/>.
    ///
    /// Covers:
    ///   • Initial <see cref="DifficultySelectionController.SelectedIndex"/> is 0.
    ///   • <see cref="DifficultySelectionController.NextPreset"/>:
    ///       - null / empty presets → no-op, no throw.
    ///       - increments index correctly.
    ///       - wraps forward from the last preset back to index 0.
    ///   • <see cref="DifficultySelectionController.PreviousPreset"/>:
    ///       - null / empty presets → no-op, no throw.
    ///       - decrements index correctly.
    ///       - wraps backward from index 0 to the last preset.
    ///   • <c>ApplySelection</c> (triggered via Next/Prev):
    ///       - writes the correct <see cref="BotDifficultyConfig"/> to
    ///         <see cref="SelectedDifficultySO.Current"/>.
    ///       - null <see cref="SelectedDifficultySO"/> → no throw.
    ///       - null presets list → <see cref="SelectedDifficultySO.Current"/> stays null.
    ///
    /// All tests run headless (no scene, no uGUI).  Private inspector fields are
    /// injected via reflection so the tests remain independent of the Unity Editor.
    /// </summary>
    public class DifficultySelectionControllerTests
    {
        // ── Scene objects ──────────────────────────────────────────────────────

        private GameObject                   _go;
        private DifficultySelectionController _ctrl;

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi,
                $"Reflection: field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Creates a <see cref="DifficultyPresetsConfig"/> whose internal <c>_presets</c>
        /// list is populated with <paramref name="count"/> entries, each backed by a
        /// distinct <see cref="BotDifficultyConfig"/> instance.
        /// Returns both the config SO and the array of created BotDifficultyConfig SOs
        /// so the caller can destroy them in TearDown.
        /// </summary>
        private static (DifficultyPresetsConfig config, BotDifficultyConfig[] botConfigs)
            MakePresetsConfig(int count)
        {
            var botConfigs = new BotDifficultyConfig[count];
            var presets    = new List<DifficultyPresetsConfig.DifficultyPreset>(count);

            for (int i = 0; i < count; i++)
            {
                botConfigs[i] = ScriptableObject.CreateInstance<BotDifficultyConfig>();

                var preset = new DifficultyPresetsConfig.DifficultyPreset
                {
                    displayName = $"Preset{i}",
                    config      = botConfigs[i]
                };
                presets.Add(preset);
            }

            var config = ScriptableObject.CreateInstance<DifficultyPresetsConfig>();
            // Inject the list into the private _presets field.
            FieldInfo presetsField = typeof(DifficultyPresetsConfig)
                .GetField("_presets", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(presetsField, "Reflection: _presets not found on DifficultyPresetsConfig.");
            presetsField.SetValue(config, presets);

            return (config, botConfigs);
        }

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestDifficultySelectionController");
            _ctrl = _go.AddComponent<DifficultySelectionController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            _go   = null;
            _ctrl = null;
        }

        // ── Initial state ──────────────────────────────────────────────────────

        [Test]
        public void SelectedIndex_InitialValue_IsZero()
        {
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must start at 0 (first preset selected by default).");
        }

        // ── NextPreset — guard paths ───────────────────────────────────────────

        [Test]
        public void NextPreset_NullPresets_DoesNotThrow()
        {
            // _presets is null by default (not injected).
            Assert.DoesNotThrow(() => _ctrl.NextPreset(),
                "NextPreset with null _presets must not throw.");
        }

        [Test]
        public void NextPreset_NullPresets_IndexRemainsZero()
        {
            _ctrl.NextPreset();
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must stay 0 when _presets is null.");
        }

        [Test]
        public void NextPreset_EmptyPresets_DoesNotChangeIndex()
        {
            var (config, _) = MakePresetsConfig(0);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextPreset();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "NextPreset on an empty presets list must not change SelectedIndex.");
            Object.DestroyImmediate(config);
        }

        // ── NextPreset — cycling ───────────────────────────────────────────────

        [Test]
        public void NextPreset_ThreePresets_IncrementsIndex()
        {
            var (config, bots) = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextPreset();

            Assert.AreEqual(1, _ctrl.SelectedIndex,
                "NextPreset from index 0 should advance to 1.");

            Object.DestroyImmediate(config);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        [Test]
        public void NextPreset_AtLastPreset_WrapsToZero()
        {
            var (config, bots) = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            // Advance to the last preset (index 2).
            _ctrl.NextPreset(); // 0 → 1
            _ctrl.NextPreset(); // 1 → 2
            _ctrl.NextPreset(); // 2 → wraps to 0

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "NextPreset past the last preset must wrap back to index 0.");

            Object.DestroyImmediate(config);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        [Test]
        public void NextPreset_FullCycle_ReturnsToStart()
        {
            var (config, bots) = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            // Three nexts on 3 presets returns to starting index.
            _ctrl.NextPreset();
            _ctrl.NextPreset();
            _ctrl.NextPreset();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "Three consecutive NextPreset calls on 3 presets must return to 0.");

            Object.DestroyImmediate(config);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        // ── PreviousPreset — guard paths ───────────────────────────────────────

        [Test]
        public void PreviousPreset_NullPresets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ctrl.PreviousPreset(),
                "PreviousPreset with null _presets must not throw.");
        }

        [Test]
        public void PreviousPreset_NullPresets_IndexRemainsZero()
        {
            _ctrl.PreviousPreset();
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must stay 0 when _presets is null.");
        }

        [Test]
        public void PreviousPreset_EmptyPresets_DoesNotChangeIndex()
        {
            var (config, _) = MakePresetsConfig(0);
            SetField(_ctrl, "_presets", config);

            _ctrl.PreviousPreset();

            Assert.AreEqual(0, _ctrl.SelectedIndex);
            Object.DestroyImmediate(config);
        }

        // ── PreviousPreset — cycling ───────────────────────────────────────────

        [Test]
        public void PreviousPreset_AtFirstPreset_WrapsToLast()
        {
            var (config, bots) = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.PreviousPreset(); // 0 → wraps to 2

            Assert.AreEqual(2, _ctrl.SelectedIndex,
                "PreviousPreset at index 0 must wrap to the last preset index (2).");

            Object.DestroyImmediate(config);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        [Test]
        public void PreviousPreset_FromSecond_GoesToFirst()
        {
            var (config, bots) = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextPreset();     // 0 → 1
            _ctrl.PreviousPreset(); // 1 → 0

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "PreviousPreset from index 1 should return to index 0.");

            Object.DestroyImmediate(config);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        [Test]
        public void NextThenPrevious_ReturnsToStartingIndex()
        {
            var (config, bots) = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextPreset();
            _ctrl.PreviousPreset();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "Next then Previous should return to the starting index.");

            Object.DestroyImmediate(config);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        // ── ApplySelection — SelectedDifficultySO writes ───────────────────────

        [Test]
        public void NextPreset_WritesCorrectConfigToSelectedDifficultySO()
        {
            var (config, bots) = MakePresetsConfig(3);
            var selected = ScriptableObject.CreateInstance<SelectedDifficultySO>();
            SetField(_ctrl, "_presets",            config);
            SetField(_ctrl, "_selectedDifficulty", selected);

            _ctrl.NextPreset(); // advances to index 1

            // SelectedDifficultySO.Current should equal preset[1].config.
            Assert.AreEqual(bots[1], selected.Current,
                "After NextPreset to index 1, SelectedDifficultySO.Current must be presets[1].config.");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(selected);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        [Test]
        public void PreviousPreset_FromZero_WritesLastConfigToSelectedDifficultySO()
        {
            var (config, bots) = MakePresetsConfig(3);
            var selected = ScriptableObject.CreateInstance<SelectedDifficultySO>();
            SetField(_ctrl, "_presets",            config);
            SetField(_ctrl, "_selectedDifficulty", selected);

            _ctrl.PreviousPreset(); // 0 → wraps to 2

            Assert.AreEqual(bots[2], selected.Current,
                "PreviousPreset from 0 wraps to last preset; SelectedDifficultySO.Current must be presets[2].config.");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(selected);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }

        [Test]
        public void ApplySelection_NullSelectedDifficulty_DoesNotThrow()
        {
            var (config, bots) = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);
            // _selectedDifficulty remains null.

            Assert.DoesNotThrow(() => _ctrl.NextPreset(),
                "NextPreset must not throw when _selectedDifficulty is null.");

            Object.DestroyImmediate(config);
            foreach (var b in bots) Object.DestroyImmediate(b);
        }
    }
}
