using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T332: <see cref="ZoneControlMatchPaceSO"/> and
    /// <see cref="ZoneControlMatchPaceAdvisorController"/>.
    ///
    /// ZoneControlMatchPaceAdvisorTests (12):
    ///   SO_FreshInstance_LastAdvice_OnTarget                     ×1
    ///   SO_EvaluatePace_WellAboveTarget_ReturnsAheadOfPace       ×1
    ///   SO_EvaluatePace_WellBelowTarget_ReturnsBehindPace        ×1
    ///   SO_EvaluatePace_NearTarget_ReturnsOnTarget               ×1
    ///   SO_EvaluatePace_FiresAheadEvent                          ×1
    ///   SO_EvaluatePace_FiresBehindEvent                         ×1
    ///   SO_EvaluatePace_NoEventWhenOnTarget                      ×1
    ///   SO_Reset_ClearsAdviceToOnTarget                          ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullPaceSO_HidesPanel                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchPaceAdvisorTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlMatchPaceSO CreatePaceSO()
        {
            // defaults: targetPace=1.5, aheadThreshold=0.5, behindThreshold=0.5
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPaceSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_LastAdvice_OnTarget()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPaceSO>();
            Assert.AreEqual(ZoneControlPaceAdvice.OnTarget, so.LastAdvice,
                "LastAdvice must be OnTarget on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluatePace_WellAboveTarget_ReturnsAheadOfPace()
        {
            var so = CreatePaceSO(); // target=1.5, ahead at ≥2.0
            var result = so.EvaluatePace(3f);
            Assert.AreEqual(ZoneControlPaceAdvice.AheadOfPace, result,
                "Rate well above target must return AheadOfPace.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluatePace_WellBelowTarget_ReturnsBehindPace()
        {
            var so = CreatePaceSO(); // target=1.5, behind at ≤1.0
            var result = so.EvaluatePace(0f);
            Assert.AreEqual(ZoneControlPaceAdvice.BehindPace, result,
                "Rate well below target must return BehindPace.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluatePace_NearTarget_ReturnsOnTarget()
        {
            var so = CreatePaceSO(); // target=1.5, on-target range (1.0, 2.0)
            var result = so.EvaluatePace(1.5f);
            Assert.AreEqual(ZoneControlPaceAdvice.OnTarget, result,
                "Rate equal to target must return OnTarget.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluatePace_FiresAheadEvent()
        {
            var so  = CreatePaceSO();
            var evt = CreateEvent();
            SetField(so, "_onAheadOfPace", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.EvaluatePace(3f);

            Assert.AreEqual(1, fired,
                "_onAheadOfPace must fire when rate exceeds target + aheadThreshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluatePace_FiresBehindEvent()
        {
            var so  = CreatePaceSO();
            var evt = CreateEvent();
            SetField(so, "_onBehindPace", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.EvaluatePace(0f);

            Assert.AreEqual(1, fired,
                "_onBehindPace must fire when rate falls below target - behindThreshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluatePace_NoEventWhenOnTarget()
        {
            var so    = CreatePaceSO();
            var ahead = CreateEvent();
            var behind = CreateEvent();
            SetField(so, "_onAheadOfPace", ahead);
            SetField(so, "_onBehindPace",  behind);

            int firedAhead  = 0;
            int firedBehind = 0;
            ahead.RegisterCallback(() => firedAhead++);
            behind.RegisterCallback(() => firedBehind++);
            so.EvaluatePace(1.5f);

            Assert.AreEqual(0, firedAhead,  "No ahead event when on target.");
            Assert.AreEqual(0, firedBehind, "No behind event when on target.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(ahead);
            Object.DestroyImmediate(behind);
        }

        [Test]
        public void SO_Reset_ClearsAdviceToOnTarget()
        {
            var so = CreatePaceSO();
            so.EvaluatePace(3f); // AheadOfPace
            so.Reset();
            Assert.AreEqual(ZoneControlPaceAdvice.OnTarget, so.LastAdvice,
                "LastAdvice must be OnTarget after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchPaceAdvisorController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchPaceAdvisorController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchPaceAdvisorController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchStarted", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchStarted must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullPaceSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlMatchPaceAdvisorController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when PaceSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
