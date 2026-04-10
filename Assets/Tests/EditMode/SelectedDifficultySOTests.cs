using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SelectedDifficultySO"/>.
    ///
    /// Covers:
    ///   • Fresh instance: <see cref="SelectedDifficultySO.Current"/> is null.
    ///   • <see cref="SelectedDifficultySO.Select"/>: sets Current; fires
    ///     <c>_onDifficultyChanged</c>; null config allowed (clears override).
    ///   • Select with no event wired: does not throw.
    ///   • Select called twice: last-write-wins; event fires per call.
    ///   • <see cref="SelectedDifficultySO.Reset"/>: clears Current without
    ///     firing the event.
    ///   • Reset when Current is already null: no throw.
    /// </summary>
    public class SelectedDifficultySOTests
    {
        private SelectedDifficultySO _so;
        private VoidGameEvent        _onChanged;
        private BotDifficultyConfig  _configA;
        private BotDifficultyConfig  _configB;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>Inject the VoidGameEvent channel so Select() can raise it.</summary>
        private void WireEvent()
        {
            SetField(_so, "_onDifficultyChanged", _onChanged);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so        = ScriptableObject.CreateInstance<SelectedDifficultySO>();
            _onChanged = ScriptableObject.CreateInstance<VoidGameEvent>();
            _configA   = ScriptableObject.CreateInstance<BotDifficultyConfig>();
            _configB   = ScriptableObject.CreateInstance<BotDifficultyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onChanged);
            Object.DestroyImmediate(_configA);
            Object.DestroyImmediate(_configB);

            _so        = null;
            _onChanged = null;
            _configA   = null;
            _configB   = null;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Current_IsNull()
        {
            Assert.IsNull(_so.Current);
        }

        // ── Select() ─────────────────────────────────────────────────────────

        [Test]
        public void Select_Config_SetsCurrent()
        {
            _so.Select(_configA);
            Assert.AreEqual(_configA, _so.Current);
        }

        [Test]
        public void Select_Null_SetsCurrentToNull()
        {
            _so.Select(_configA);
            _so.Select(null);
            Assert.IsNull(_so.Current);
        }

        [Test]
        public void Select_NullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.Select(null));
        }

        [Test]
        public void Select_WithNoEventWired_DoesNotThrow()
        {
            // _onDifficultyChanged left null — Select must be null-safe.
            Assert.DoesNotThrow(() => _so.Select(_configA));
        }

        [Test]
        public void Select_FiresOnDifficultyChangedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.Select(_configA);

            Assert.AreEqual(1, fireCount, "Select() must raise _onDifficultyChanged.");
        }

        [Test]
        public void Select_NullConfig_StillFiresEventIfWired()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.Select(null);

            Assert.AreEqual(1, fireCount, "Select(null) must still raise _onDifficultyChanged.");
        }

        [Test]
        public void Select_Twice_LastWins_Current()
        {
            _so.Select(_configA);
            _so.Select(_configB);
            Assert.AreEqual(_configB, _so.Current);
        }

        [Test]
        public void Select_Twice_EventFiresPerCall()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.Select(_configA);
            _so.Select(_configB);

            Assert.AreEqual(2, fireCount, "Each Select() call must fire the event once.");
        }

        // ── Reset() ───────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsCurrent()
        {
            _so.Select(_configA);
            _so.Reset();
            Assert.IsNull(_so.Current);
        }

        [Test]
        public void Reset_DoesNotFireChangedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.Select(_configA); // count = 1
            _so.Reset();          // must NOT fire

            Assert.AreEqual(1, fireCount,
                "Reset() must not fire _onDifficultyChanged — it is a silent clear.");
        }

        [Test]
        public void Reset_WhenCurrentIsAlreadyNull_DoesNotThrow()
        {
            // Fresh instance — Current already null.
            Assert.DoesNotThrow(() => _so.Reset());
        }
    }
}
