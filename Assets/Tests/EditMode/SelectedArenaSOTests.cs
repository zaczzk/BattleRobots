using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SelectedArenaSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance: HasSelection false, Current null, CurrentDisplayName "Arena".
    ///   • Select with a valid preset: HasSelection true, Current set, DisplayName correct.
    ///   • Select with null preset: HasSelection true, Current null.
    ///   • Select fires _onArenaSelected exactly once per call.
    ///   • Select with null event channel: no throw.
    ///   • Select called twice: Current updated to second preset.
    ///   • Reset: clears HasSelection without firing event.
    ///   • CurrentDisplayName fallback: null preset / whitespace display name → "Arena".
    /// </summary>
    public class SelectedArenaSOTests
    {
        private SelectedArenaSO _so;
        private VoidGameEvent   _event;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Reflection: field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so    = ScriptableObject.CreateInstance<SelectedArenaSO>();
            _event = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_so, "_onArenaSelected", _event);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_event);
            _so    = null;
            _event = null;
        }

        // ── Fresh-instance invariants ─────────────────────────────────────────

        [Test]
        public void FreshInstance_HasSelection_IsFalse()
        {
            Assert.IsFalse(_so.HasSelection,
                "HasSelection must be false on a fresh instance (no Select called yet).");
        }

        [Test]
        public void FreshInstance_Current_IsNull()
        {
            Assert.IsNull(_so.Current,
                "Current must be null on a fresh instance.");
        }

        [Test]
        public void FreshInstance_CurrentDisplayName_IsArena()
        {
            Assert.AreEqual("Arena", _so.CurrentDisplayName,
                "CurrentDisplayName must return 'Arena' when nothing is selected.");
        }

        // ── Select with valid preset ──────────────────────────────────────────

        [Test]
        public void Select_ValidPreset_SetsHasSelection_True()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Factory" };
            _so.Select(preset);

            Assert.IsTrue(_so.HasSelection,
                "HasSelection must be true after Select is called with a valid preset.");
        }

        [Test]
        public void Select_ValidPreset_CurrentIsPreset()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Wasteland" };
            _so.Select(preset);

            Assert.AreSame(preset, _so.Current,
                "Current must reference the exact preset object passed to Select.");
        }

        [Test]
        public void Select_ValidPreset_CurrentDisplayName_MatchesPreset()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Colosseum" };
            _so.Select(preset);

            Assert.AreEqual("Colosseum", _so.CurrentDisplayName,
                "CurrentDisplayName must equal the preset's displayName after Select.");
        }

        [Test]
        public void Select_FiresOnArenaSelectedEvent_Once()
        {
            int raised = 0;
            _event.RegisterCallback(() => raised++);

            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Arena" };
            _so.Select(preset);

            Assert.AreEqual(1, raised,
                "_onArenaSelected must fire exactly once per Select call.");
        }

        // ── Select with null preset ───────────────────────────────────────────

        [Test]
        public void Select_NullPreset_SetsHasSelection_True()
        {
            // null is a valid argument (clears config override, ArenaManager falls back)
            _so.Select(null);

            Assert.IsTrue(_so.HasSelection,
                "HasSelection must be true even when null is passed to Select.");
        }

        [Test]
        public void Select_NullPreset_CurrentIsNull()
        {
            _so.Select(null);
            Assert.IsNull(_so.Current,
                "Current must be null when null preset is passed to Select.");
        }

        // ── Select with missing event channel ─────────────────────────────────

        [Test]
        public void Select_NullEventChannel_DoesNotThrow()
        {
            // Remove the event channel to test the ?. guard.
            SetField(_so, "_onArenaSelected", null);
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Arena" };

            Assert.DoesNotThrow(() => _so.Select(preset),
                "Select must not throw when _onArenaSelected is null.");
        }

        // ── Select called twice ───────────────────────────────────────────────

        [Test]
        public void Select_CalledTwice_CurrentIsSecondPreset()
        {
            var first  = new ArenaPresetsConfig.ArenaPreset { displayName = "First" };
            var second = new ArenaPresetsConfig.ArenaPreset { displayName = "Second" };

            _so.Select(first);
            _so.Select(second);

            Assert.AreSame(second, _so.Current,
                "After two Select calls, Current must be the second preset.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsHasSelection()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Arena" };
            _so.Select(preset);
            _so.Reset();

            Assert.IsFalse(_so.HasSelection,
                "HasSelection must be false after Reset.");
        }

        [Test]
        public void Reset_DoesNotFireEvent()
        {
            int raised = 0;
            _event.RegisterCallback(() => raised++);

            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "Arena" };
            _so.Select(preset); // +1
            _so.Reset();        // must not fire

            Assert.AreEqual(1, raised,
                "Reset must not fire _onArenaSelected (only Select should raise it).");
        }

        // ── CurrentDisplayName edge cases ─────────────────────────────────────

        [Test]
        public void CurrentDisplayName_WhitespaceDisplayName_FallsBackToArena()
        {
            var preset = new ArenaPresetsConfig.ArenaPreset { displayName = "   " };
            _so.Select(preset);

            Assert.AreEqual("Arena", _so.CurrentDisplayName,
                "CurrentDisplayName must fall back to 'Arena' when displayName is whitespace.");
        }
    }
}
