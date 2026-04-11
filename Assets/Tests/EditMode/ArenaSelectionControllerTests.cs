using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ArenaSelectionController"/>.
    ///
    /// Covers:
    ///   • Initial <see cref="ArenaSelectionController.SelectedIndex"/> is 0.
    ///   • <see cref="ArenaSelectionController.NextArena"/>:
    ///       - null / empty presets → no-op, no throw.
    ///       - increments index correctly.
    ///       - wraps forward from the last preset back to index 0.
    ///   • <see cref="ArenaSelectionController.PreviousArena"/>:
    ///       - null / empty presets → no-op, no throw.
    ///       - decrements index correctly.
    ///       - wraps backward from index 0 to the last preset.
    ///   • ApplySelection (triggered via Next/Prev):
    ///       - writes the correct preset to <see cref="SelectedArenaSO"/>.
    ///       - null <see cref="SelectedArenaSO"/> → no throw.
    ///
    /// All tests run headless (no scene, no uGUI).  Private inspector fields are
    /// injected via reflection following the same pattern as DifficultySelectionControllerTests.
    /// </summary>
    public class ArenaSelectionControllerTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────

        private GameObject              _go;
        private ArenaSelectionController _ctrl;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi,
                $"Reflection: field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Creates an <see cref="ArenaPresetsConfig"/> whose internal <c>_presets</c>
        /// list contains <paramref name="count"/> entries, each with a unique
        /// display name and a null ArenaConfig (SO refs are irrelevant for controller tests).
        /// </summary>
        private static ArenaPresetsConfig MakePresetsConfig(int count)
        {
            var presets = new List<ArenaPresetsConfig.ArenaPreset>(count);

            for (int i = 0; i < count; i++)
            {
                presets.Add(new ArenaPresetsConfig.ArenaPreset
                {
                    displayName = $"Arena{i}",
                    config      = null  // ArenaConfig SO not needed for controller tests
                });
            }

            var config = ScriptableObject.CreateInstance<ArenaPresetsConfig>();
            FieldInfo fi = typeof(ArenaPresetsConfig)
                .GetField("_presets", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "Reflection: _presets not found on ArenaPresetsConfig.");
            fi.SetValue(config, presets);
            return config;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestArenaSelectionController");
            _ctrl = _go.AddComponent<ArenaSelectionController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            _go   = null;
            _ctrl = null;
        }

        // ── Initial state ─────────────────────────────────────────────────────

        [Test]
        public void SelectedIndex_InitialValue_IsZero()
        {
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must start at 0 (first preset selected by default).");
        }

        // ── NextArena — guard paths ───────────────────────────────────────────

        [Test]
        public void NextArena_NullPresets_DoesNotThrow()
        {
            // _presets is null by default (not injected).
            Assert.DoesNotThrow(() => _ctrl.NextArena(),
                "NextArena with null _presets must not throw.");
        }

        [Test]
        public void NextArena_NullPresets_IndexRemainsZero()
        {
            _ctrl.NextArena();
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must stay 0 when _presets is null.");
        }

        [Test]
        public void NextArena_EmptyPresets_DoesNotChangeIndex()
        {
            var config = MakePresetsConfig(0);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextArena();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "NextArena on an empty presets list must not change SelectedIndex.");
            Object.DestroyImmediate(config);
        }

        // ── NextArena — cycling ───────────────────────────────────────────────

        [Test]
        public void NextArena_ThreePresets_IncrementsIndex()
        {
            var config = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextArena();

            Assert.AreEqual(1, _ctrl.SelectedIndex,
                "NextArena from index 0 should advance to 1.");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void NextArena_AtLastPreset_WrapsToZero()
        {
            var config = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextArena(); // 0 → 1
            _ctrl.NextArena(); // 1 → 2
            _ctrl.NextArena(); // 2 → wraps to 0

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "NextArena past the last preset must wrap back to index 0.");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void NextArena_FullCycle_ReturnsToStart()
        {
            var config = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextArena();
            _ctrl.NextArena();
            _ctrl.NextArena();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "Three consecutive NextArena calls on 3 presets must return to 0.");
            Object.DestroyImmediate(config);
        }

        // ── PreviousArena — guard paths ───────────────────────────────────────

        [Test]
        public void PreviousArena_NullPresets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ctrl.PreviousArena(),
                "PreviousArena with null _presets must not throw.");
        }

        [Test]
        public void PreviousArena_NullPresets_IndexRemainsZero()
        {
            _ctrl.PreviousArena();
            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "SelectedIndex must stay 0 when _presets is null.");
        }

        [Test]
        public void PreviousArena_EmptyPresets_DoesNotChangeIndex()
        {
            var config = MakePresetsConfig(0);
            SetField(_ctrl, "_presets", config);

            _ctrl.PreviousArena();

            Assert.AreEqual(0, _ctrl.SelectedIndex);
            Object.DestroyImmediate(config);
        }

        // ── PreviousArena — cycling ───────────────────────────────────────────

        [Test]
        public void PreviousArena_AtFirstPreset_WrapsToLast()
        {
            var config = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.PreviousArena(); // 0 → wraps to 2

            Assert.AreEqual(2, _ctrl.SelectedIndex,
                "PreviousArena at index 0 must wrap to the last preset index (2).");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void PreviousArena_FromSecond_GoesToFirst()
        {
            var config = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextArena();     // 0 → 1
            _ctrl.PreviousArena(); // 1 → 0

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "PreviousArena from index 1 should return to index 0.");
            Object.DestroyImmediate(config);
        }

        [Test]
        public void NextThenPrevious_ReturnsToStartingIndex()
        {
            var config = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);

            _ctrl.NextArena();
            _ctrl.PreviousArena();

            Assert.AreEqual(0, _ctrl.SelectedIndex,
                "Next then Previous should return to the starting index.");
            Object.DestroyImmediate(config);
        }

        // ── ApplySelection — SelectedArenaSO writes ───────────────────────────

        [Test]
        public void NextArena_WritesCorrectPresetToSelectedArenaSO()
        {
            var config   = MakePresetsConfig(3);
            var selected = ScriptableObject.CreateInstance<SelectedArenaSO>();
            SetField(_ctrl, "_presets",       config);
            SetField(_ctrl, "_selectedArena", selected);

            _ctrl.NextArena(); // advances to index 1

            // SelectedArenaSO.Current should reference presets[1].
            Assert.IsTrue(selected.HasSelection,
                "SelectedArenaSO.HasSelection must be true after NextArena.");
            Assert.AreEqual("Arena1", selected.CurrentDisplayName,
                "After NextArena to index 1, SelectedArenaSO.CurrentDisplayName must be 'Arena1'.");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(selected);
        }

        [Test]
        public void PreviousArena_FromZero_WritesLastPresetToSelectedArenaSO()
        {
            var config   = MakePresetsConfig(3);
            var selected = ScriptableObject.CreateInstance<SelectedArenaSO>();
            SetField(_ctrl, "_presets",       config);
            SetField(_ctrl, "_selectedArena", selected);

            _ctrl.PreviousArena(); // 0 → wraps to 2

            Assert.AreEqual("Arena2", selected.CurrentDisplayName,
                "PreviousArena from 0 wraps to last preset; CurrentDisplayName must be 'Arena2'.");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(selected);
        }

        [Test]
        public void NextArena_NullSelectedArenaSO_DoesNotThrow()
        {
            var config = MakePresetsConfig(3);
            SetField(_ctrl, "_presets", config);
            // _selectedArena remains null.

            Assert.DoesNotThrow(() => _ctrl.NextArena(),
                "NextArena must not throw when _selectedArena is null.");

            Object.DestroyImmediate(config);
        }
    }
}
