using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="OpponentSelectionController"/>.
    ///
    /// Covers:
    ///   • SelectedIndex starts at 0.
    ///   • NextOpponent / PreviousOpponent: null roster and empty roster guards.
    ///   • NextOpponent: increments index; wraps from last to 0; full N-cycle returns to start.
    ///   • PreviousOpponent: wraps 0 → last; decrements from 1 to 0.
    ///   • Next then Previous: returns to starting index.
    ///   • NextOpponent writes the correct profile to SelectedOpponentSO.
    ///   • PreviousOpponent wrap writes the last profile to SelectedOpponentSO.
    ///   • Null SelectedOpponentSO: does not throw.
    ///
    /// Uses the inactive-GO pattern to defer Awake until after field injection.
    /// No uGUI components needed — Button / Text fields are all null-safe.
    /// </summary>
    public class OpponentSelectionControllerTests
    {
        // ── Scene objects ──────────────────────────────────────────────────────
        private GameObject                  _go;
        private OpponentSelectionController _controller;

        private readonly List<GameObject>       _extraGOs = new List<GameObject>();
        private readonly List<ScriptableObject> _extraSOs = new List<ScriptableObject>();

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private ScriptableObject MakeSO<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            _extraSOs.Add(so);
            return so;
        }

        private OpponentProfileSO MakeProfile(string displayName = "Bot")
        {
            var p = (OpponentProfileSO)MakeSO<OpponentProfileSO>();
            FieldInfo fi = p.GetType()
                .GetField("_displayName", BindingFlags.Instance | BindingFlags.NonPublic);
            fi?.SetValue(p, displayName);
            return p;
        }

        private OpponentRosterSO MakeRoster(params OpponentProfileSO[] profiles)
        {
            var roster = (OpponentRosterSO)MakeSO<OpponentRosterSO>();
            SetField(roster, "_opponents", new List<OpponentProfileSO>(profiles));
            return roster;
        }

        /// <summary>
        /// Creates a controller with Awake deferred — caller injects fields then calls
        /// <c>go.SetActive(true)</c> to trigger Awake + OnEnable.
        /// </summary>
        private (GameObject go, OpponentSelectionController ctrl) MakeInactiveController()
        {
            var go = new GameObject("Ctrl");
            go.SetActive(false);
            var ctrl = go.AddComponent<OpponentSelectionController>();
            _extraGOs.Add(go);
            return (go, ctrl);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go         = new GameObject("TestCtrl");
            _controller = _go.AddComponent<OpponentSelectionController>();
            _extraGOs.Clear();
            _extraSOs.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (var g in _extraGOs) if (g != null) Object.DestroyImmediate(g);
            foreach (var s in _extraSOs) if (s != null) Object.DestroyImmediate(s);
            _extraGOs.Clear();
            _extraSOs.Clear();
            _go         = null;
            _controller = null;
        }

        // ── SelectedIndex ─────────────────────────────────────────────────────

        [Test]
        public void SelectedIndex_DefaultIsZero()
        {
            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        // ── NextOpponent guards ───────────────────────────────────────────────

        [Test]
        public void NextOpponent_NullRoster_DoesNotThrow_IndexUnchanged()
        {
            Assert.DoesNotThrow(() => _controller.NextOpponent());
            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        [Test]
        public void NextOpponent_EmptyRoster_DoesNotThrow_IndexUnchanged()
        {
            var roster = MakeRoster(); // empty
            SetField(_controller, "_roster", roster);
            Assert.DoesNotThrow(() => _controller.NextOpponent());
            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        // ── NextOpponent cycling ──────────────────────────────────────────────

        [Test]
        public void NextOpponent_Increments_SelectedIndex()
        {
            var roster = MakeRoster(MakeProfile("A"), MakeProfile("B"), MakeProfile("C"));
            SetField(_controller, "_roster", roster);

            _controller.NextOpponent();

            Assert.AreEqual(1, _controller.SelectedIndex);
        }

        [Test]
        public void NextOpponent_WrapsFromLastToFirst()
        {
            var roster = MakeRoster(MakeProfile("A"), MakeProfile("B"));
            SetField(_controller, "_roster", roster);

            _controller.NextOpponent(); // → 1
            _controller.NextOpponent(); // → wrap → 0

            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        [Test]
        public void NextOpponent_FullCycle_ReturnsToStart()
        {
            var pA = MakeProfile("A");
            var pB = MakeProfile("B");
            var pC = MakeProfile("C");
            var roster = MakeRoster(pA, pB, pC);
            SetField(_controller, "_roster", roster);

            _controller.NextOpponent(); // 1
            _controller.NextOpponent(); // 2
            _controller.NextOpponent(); // 0 (wrap)

            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        // ── PreviousOpponent guards ───────────────────────────────────────────

        [Test]
        public void PreviousOpponent_NullRoster_DoesNotThrow_IndexUnchanged()
        {
            Assert.DoesNotThrow(() => _controller.PreviousOpponent());
            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        [Test]
        public void PreviousOpponent_EmptyRoster_DoesNotThrow_IndexUnchanged()
        {
            var roster = MakeRoster();
            SetField(_controller, "_roster", roster);
            Assert.DoesNotThrow(() => _controller.PreviousOpponent());
            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        // ── PreviousOpponent cycling ──────────────────────────────────────────

        [Test]
        public void PreviousOpponent_WrapsFromFirstToLast()
        {
            var roster = MakeRoster(MakeProfile("A"), MakeProfile("B"), MakeProfile("C"));
            SetField(_controller, "_roster", roster);

            _controller.PreviousOpponent(); // 0 - 1 + 3 = 2

            Assert.AreEqual(2, _controller.SelectedIndex);
        }

        [Test]
        public void PreviousOpponent_Decrements_SelectedIndex()
        {
            var roster = MakeRoster(MakeProfile("A"), MakeProfile("B"));
            SetField(_controller, "_roster", roster);

            _controller.NextOpponent();     // → 1
            _controller.PreviousOpponent(); // → 0

            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        [Test]
        public void NextThenPrevious_ReturnsToStart()
        {
            var roster = MakeRoster(MakeProfile("A"), MakeProfile("B"), MakeProfile("C"));
            SetField(_controller, "_roster", roster);

            _controller.NextOpponent();
            _controller.PreviousOpponent();

            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        // ── SelectedOpponentSO writes ─────────────────────────────────────────

        [Test]
        public void NextOpponent_WritesCorrectProfile_ToSelectedOpponentSO()
        {
            var pA = MakeProfile("Alpha");
            var pB = MakeProfile("Beta");
            var roster = MakeRoster(pA, pB);
            var selectedSO = (SelectedOpponentSO)MakeSO<SelectedOpponentSO>();

            SetField(_controller, "_roster", roster);
            SetField(_controller, "_selectedOpponent", selectedSO);

            _controller.NextOpponent(); // → index 1 → pB

            Assert.AreSame(pB, selectedSO.Current);
        }

        [Test]
        public void PreviousOpponent_Wrap_WritesLastProfile_ToSelectedOpponentSO()
        {
            var pA = MakeProfile("Alpha");
            var pB = MakeProfile("Beta");
            var pC = MakeProfile("Gamma");
            var roster = MakeRoster(pA, pB, pC);
            var selectedSO = (SelectedOpponentSO)MakeSO<SelectedOpponentSO>();

            SetField(_controller, "_roster", roster);
            SetField(_controller, "_selectedOpponent", selectedSO);

            _controller.PreviousOpponent(); // 0 → wrap → 2 → pC

            Assert.AreSame(pC, selectedSO.Current);
        }

        [Test]
        public void NullSelectedOpponentSO_DoesNotThrow()
        {
            // _selectedOpponent left null — ApplySelection must guard with ?.
            var roster = MakeRoster(MakeProfile("A"), MakeProfile("B"));
            SetField(_controller, "_roster", roster);
            // _selectedOpponent is null by default

            Assert.DoesNotThrow(() => _controller.NextOpponent());
        }
    }
}
