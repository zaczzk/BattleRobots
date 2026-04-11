using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SelectedOpponentSO"/>.
    ///
    /// Covers:
    ///   • Fresh instance: Current null, HasSelection false, CurrentDisplayName fallback.
    ///   • Select(): sets Current; sets HasSelection true; fires _onOpponentSelected;
    ///     null profile allowed; called twice last-write-wins, event fires per call.
    ///   • Select() with no event wired: does not throw.
    ///   • Reset(): clears Current; clears HasSelection; does not fire event.
    ///   • Reset() when already clear: does not throw.
    ///   • CurrentDisplayName fallback / populated paths.
    /// </summary>
    public class SelectedOpponentSOTests
    {
        private SelectedOpponentSO _so;
        private VoidGameEvent      _onSelected;
        private OpponentProfileSO  _profileA;
        private OpponentProfileSO  _profileB;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetProfileName(OpponentProfileSO profile, string displayName)
        {
            SetField(profile, "_displayName", displayName);
        }

        private void WireEvent()
        {
            SetField(_so, "_onOpponentSelected", _onSelected);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so         = ScriptableObject.CreateInstance<SelectedOpponentSO>();
            _onSelected = ScriptableObject.CreateInstance<VoidGameEvent>();
            _profileA   = ScriptableObject.CreateInstance<OpponentProfileSO>();
            _profileB   = ScriptableObject.CreateInstance<OpponentProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onSelected);
            Object.DestroyImmediate(_profileA);
            Object.DestroyImmediate(_profileB);
            _so         = null;
            _onSelected = null;
            _profileA   = null;
            _profileB   = null;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Current_IsNull()
        {
            Assert.IsNull(_so.Current);
        }

        [Test]
        public void FreshInstance_HasSelection_IsFalse()
        {
            Assert.IsFalse(_so.HasSelection);
        }

        [Test]
        public void FreshInstance_CurrentDisplayName_IsFallback()
        {
            Assert.AreEqual("Opponent", _so.CurrentDisplayName);
        }

        // ── Select() ──────────────────────────────────────────────────────────

        [Test]
        public void Select_SetsCurrent()
        {
            _so.Select(_profileA);
            Assert.AreSame(_profileA, _so.Current);
        }

        [Test]
        public void Select_SetsHasSelection_True()
        {
            _so.Select(_profileA);
            Assert.IsTrue(_so.HasSelection);
        }

        [Test]
        public void Select_FiresEvent()
        {
            WireEvent();
            int count = 0;
            _onSelected.RegisterCallback(() => count++);

            _so.Select(_profileA);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Select_NullProfile_SetsHasSelection_True()
        {
            // Passing null explicitly means "I chose no opponent" — distinct from Reset().
            _so.Select(null);
            Assert.IsTrue(_so.HasSelection);
            Assert.IsNull(_so.Current);
        }

        [Test]
        public void Select_NoEventWired_DoesNotThrow()
        {
            // _onOpponentSelected left null — must not NullReferenceException.
            Assert.DoesNotThrow(() => _so.Select(_profileA));
        }

        [Test]
        public void Select_CalledTwice_LastWriteWins()
        {
            _so.Select(_profileA);
            _so.Select(_profileB);
            Assert.AreSame(_profileB, _so.Current);
        }

        [Test]
        public void Select_CalledTwice_EventFiredBothTimes()
        {
            WireEvent();
            int count = 0;
            _onSelected.RegisterCallback(() => count++);

            _so.Select(_profileA);
            _so.Select(_profileB);

            Assert.AreEqual(2, count);
        }

        // ── Reset() ───────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsCurrent()
        {
            _so.Select(_profileA);
            _so.Reset();
            Assert.IsNull(_so.Current);
        }

        [Test]
        public void Reset_ClearsHasSelection()
        {
            _so.Select(_profileA);
            _so.Reset();
            Assert.IsFalse(_so.HasSelection);
        }

        [Test]
        public void Reset_DoesNotFireEvent()
        {
            WireEvent();
            int count = 0;
            _onSelected.RegisterCallback(() => count++);

            _so.Select(_profileA); // count = 1
            _so.Reset();           // should NOT fire

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Reset_WhenAlreadyClear_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.Reset());
        }

        // ── CurrentDisplayName ────────────────────────────────────────────────

        [Test]
        public void CurrentDisplayName_AfterSelect_ReturnsProfileDisplayName()
        {
            SetProfileName(_profileA, "Steel Titan");
            _so.Select(_profileA);
            Assert.AreEqual("Steel Titan", _so.CurrentDisplayName);
        }

        [Test]
        public void CurrentDisplayName_AfterReset_ReturnsFallback()
        {
            SetProfileName(_profileA, "Steel Titan");
            _so.Select(_profileA);
            _so.Reset();
            Assert.AreEqual("Opponent", _so.CurrentDisplayName);
        }
    }
}
