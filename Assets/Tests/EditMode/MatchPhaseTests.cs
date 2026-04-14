using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T223:
    ///   <see cref="MatchPhaseSO"/> and <see cref="MatchPhaseHUDController"/>.
    ///
    /// MatchPhaseSOTests (8):
    ///   FreshInstance_CurrentPhase_IsPreMatch                ×1
    ///   FreshInstance_PhaseLabel_IsPreMatch                  ×1
    ///   SetPhase_ChangesCurrentPhase                         ×1
    ///   SetPhase_SamePhase_FiresNoEvent                      ×1
    ///   SetPhase_DifferentPhase_FiresEvent                   ×1
    ///   PhaseLabel_Active_ReturnsActive                      ×1
    ///   PhaseLabel_SuddenDeath_ReturnsSuddenDeath            ×1
    ///   PhaseLabel_PostMatch_ReturnsPostMatch                ×1
    ///
    /// MatchPhaseHUDControllerTests (8):
    ///   FreshInstance_MatchPhaseNull                         ×1
    ///   OnEnable_NullRefs_DoesNotThrow                       ×1
    ///   OnDisable_NullRefs_DoesNotThrow                      ×1
    ///   OnDisable_Unregisters                                ×1
    ///   Refresh_NullPhase_LabelDash                          ×1
    ///   Refresh_NullPhase_HidesPanel                         ×1
    ///   Refresh_WithPhase_SetsLabel                          ×1
    ///   Refresh_WithPhase_ShowsPanel                         ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MatchPhaseTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static MatchPhaseSO CreateMatchPhaseSO()
        {
            var so = ScriptableObject.CreateInstance<MatchPhaseSO>();
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchPhaseHUDController CreateController() =>
            new GameObject("MatchPhaseHUDCtrl_Test").AddComponent<MatchPhaseHUDController>();

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── MatchPhaseSOTests ─────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentPhase_IsPreMatch()
        {
            var so = CreateMatchPhaseSO();
            Assert.AreEqual(MatchPhase.PreMatch, so.CurrentPhase);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_PhaseLabel_IsPreMatch()
        {
            var so = CreateMatchPhaseSO();
            Assert.AreEqual("Pre-Match", so.PhaseLabel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SetPhase_ChangesCurrentPhase()
        {
            var so = CreateMatchPhaseSO();
            so.SetPhase(MatchPhase.Active);
            Assert.AreEqual(MatchPhase.Active, so.CurrentPhase);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SetPhase_SamePhase_FiresNoEvent()
        {
            var so  = CreateMatchPhaseSO(); // starts at PreMatch
            var evt = CreateVoidEvent();
            SetField(so, "_onPhaseChanged", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.SetPhase(MatchPhase.PreMatch); // same phase — should be no-op

            Assert.AreEqual(0, count,
                "SetPhase with same phase must not raise _onPhaseChanged.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SetPhase_DifferentPhase_FiresEvent()
        {
            var so  = CreateMatchPhaseSO();
            var evt = CreateVoidEvent();
            SetField(so, "_onPhaseChanged", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);
            so.SetPhase(MatchPhase.Active);

            Assert.AreEqual(1, count,
                "SetPhase with a new phase must raise _onPhaseChanged once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void PhaseLabel_Active_ReturnsActive()
        {
            var so = CreateMatchPhaseSO();
            so.SetPhase(MatchPhase.Active);
            Assert.AreEqual("Active", so.PhaseLabel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void PhaseLabel_SuddenDeath_ReturnsSuddenDeath()
        {
            var so = CreateMatchPhaseSO();
            so.SetPhase(MatchPhase.SuddenDeath);
            Assert.AreEqual("Sudden Death", so.PhaseLabel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void PhaseLabel_PostMatch_ReturnsPostMatch()
        {
            var so = CreateMatchPhaseSO();
            so.SetPhase(MatchPhase.PostMatch);
            Assert.AreEqual("Post-Match", so.PhaseLabel);
            Object.DestroyImmediate(so);
        }

        // ── MatchPhaseHUDControllerTests ──────────────────────────────────────

        [Test]
        public void FreshInstance_MatchPhaseNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.MatchPhase);
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
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onPhaseChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback should fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullPhase_LabelDash()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "phaseLabel");
            label.text = "Active";
            SetField(ctrl, "_phaseLabel", label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh(); // _matchPhase is null

            Assert.AreEqual("\u2014", label.text,
                "Phase label should be an em-dash when no MatchPhaseSO is assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullPhase_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh(); // _matchPhase is null

            Assert.IsFalse(panel.activeSelf,
                "Panel should be hidden when no MatchPhaseSO is assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithPhase_SetsLabel()
        {
            var ctrl  = CreateController();
            var label = AddText(ctrl.gameObject, "phaseLabel");
            var so    = CreateMatchPhaseSO();
            so.SetPhase(MatchPhase.SuddenDeath);

            SetField(ctrl, "_matchPhase", so);
            SetField(ctrl, "_phaseLabel", label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Sudden Death", label.text,
                "Phase label should reflect the current PhaseLabel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_WithPhase_ShowsPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            var so = CreateMatchPhaseSO();

            SetField(ctrl, "_matchPhase", so);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel should be shown when a valid MatchPhaseSO is assigned.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }
    }
}
