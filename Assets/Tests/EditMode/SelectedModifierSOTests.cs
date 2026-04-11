using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SelectedModifierSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (Current null, HasSelection false, DisplayName "Standard").
    ///   • <see cref="SelectedModifierSO.Select"/>:
    ///       - sets Current.
    ///       - sets HasSelection to true.
    ///       - CurrentDisplayName returns modifier's DisplayName when non-empty.
    ///       - null modifier keeps HasSelection true but CurrentDisplayName → "Standard".
    ///       - null event channel does not throw.
    ///       - fires VoidGameEvent (external-counter pattern).
    ///       - called twice — last value wins.
    ///   • <see cref="SelectedModifierSO.Reset"/>:
    ///       - clears Current.
    ///       - clears HasSelection.
    ///       - does NOT fire VoidGameEvent (silent).
    ///
    /// All tests run headless (no scene).
    /// </summary>
    public class SelectedModifierSOTests
    {
        private SelectedModifierSO _so;

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<SelectedModifierSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_so);
            _so = null;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi,
                $"Reflection: field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static MatchModifierSO MakeModifier(string displayName)
        {
            var mod = ScriptableObject.CreateInstance<MatchModifierSO>();
            FieldInfo fi = typeof(MatchModifierSO)
                .GetField("_displayName", BindingFlags.NonPublic | BindingFlags.Instance);
            fi?.SetValue(mod, displayName);
            return mod;
        }

        // ── Fresh-instance defaults ────────────────────────────────────────────

        [Test]
        public void FreshInstance_Current_IsNull()
        {
            Assert.IsNull(_so.Current,
                "Current must be null on a fresh SelectedModifierSO.");
        }

        [Test]
        public void FreshInstance_HasSelection_IsFalse()
        {
            Assert.IsFalse(_so.HasSelection,
                "HasSelection must be false on a fresh SelectedModifierSO.");
        }

        [Test]
        public void FreshInstance_CurrentDisplayName_IsStandard()
        {
            Assert.AreEqual("Standard", _so.CurrentDisplayName,
                "CurrentDisplayName must return \"Standard\" before any selection is made.");
        }

        // ── Select — state writes ──────────────────────────────────────────────

        [Test]
        public void Select_SetsCurrent()
        {
            var mod = MakeModifier("DoubleRewards");
            _so.Select(mod);

            Assert.AreSame(mod, _so.Current,
                "Select must store the provided modifier in Current.");

            UnityEngine.Object.DestroyImmediate(mod);
        }

        [Test]
        public void Select_SetsHasSelectionTrue()
        {
            var mod = MakeModifier("ExtendedTime");
            _so.Select(mod);

            Assert.IsTrue(_so.HasSelection,
                "HasSelection must be true after Select is called.");

            UnityEngine.Object.DestroyImmediate(mod);
        }

        [Test]
        public void Select_WithName_CurrentDisplayNameReturnsName()
        {
            var mod = MakeModifier("Overdrive");
            _so.Select(mod);

            Assert.AreEqual("Overdrive", _so.CurrentDisplayName,
                "CurrentDisplayName must return the modifier's DisplayName when it is non-empty.");

            UnityEngine.Object.DestroyImmediate(mod);
        }

        [Test]
        public void Select_NullModifier_HasSelectionStillTrue()
        {
            _so.Select(null);

            Assert.IsTrue(_so.HasSelection,
                "HasSelection must be true even when a null modifier is selected " +
                "(null means \"no modifier\" was deliberately chosen).");
        }

        [Test]
        public void Select_NullModifier_CurrentDisplayNameIsStandard()
        {
            _so.Select(null);

            Assert.AreEqual("Standard", _so.CurrentDisplayName,
                "CurrentDisplayName must fall back to \"Standard\" when Current is null.");
        }

        [Test]
        public void Select_NullEventChannel_DoesNotThrow()
        {
            // _onModifierChanged not assigned → should not throw.
            var mod = MakeModifier("ShortTime");

            Assert.DoesNotThrow(() => _so.Select(mod),
                "Select must not throw when _onModifierChanged is null.");

            UnityEngine.Object.DestroyImmediate(mod);
        }

        [Test]
        public void Select_FiresOnModifierChangedEvent()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_so, "_onModifierChanged", channel);

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            var mod = MakeModifier("FragileArmor");
            _so.Select(mod);

            Assert.AreEqual(1, callCount,
                "Select must fire _onModifierChanged exactly once.");

            UnityEngine.Object.DestroyImmediate(mod);
            UnityEngine.Object.DestroyImmediate(channel);
        }

        [Test]
        public void Select_CalledTwice_LastValueWins()
        {
            var mod1 = MakeModifier("DoubleRewards");
            var mod2 = MakeModifier("Overdrive");

            _so.Select(mod1);
            _so.Select(mod2);

            Assert.AreSame(mod2, _so.Current,
                "After two Select calls, Current must equal the last modifier passed.");

            UnityEngine.Object.DestroyImmediate(mod1);
            UnityEngine.Object.DestroyImmediate(mod2);
        }

        // ── Reset ──────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsCurrent()
        {
            var mod = MakeModifier("ExtendedTime");
            _so.Select(mod);
            _so.Reset();

            Assert.IsNull(_so.Current,
                "Reset must clear Current to null.");

            UnityEngine.Object.DestroyImmediate(mod);
        }

        [Test]
        public void Reset_ClearsHasSelection()
        {
            var mod = MakeModifier("ShortTime");
            _so.Select(mod);
            _so.Reset();

            Assert.IsFalse(_so.HasSelection,
                "Reset must set HasSelection back to false.");

            UnityEngine.Object.DestroyImmediate(mod);
        }

        [Test]
        public void Reset_IsSilent_DoesNotFireEvent()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_so, "_onModifierChanged", channel);

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            var mod = MakeModifier("Overdrive");
            _so.Select(mod);  // fires once
            callCount = 0;    // reset counter

            _so.Reset();      // must NOT fire

            Assert.AreEqual(0, callCount,
                "Reset must not fire _onModifierChanged (silent operation).");

            UnityEngine.Object.DestroyImmediate(mod);
            UnityEngine.Object.DestroyImmediate(channel);
        }
    }
}
