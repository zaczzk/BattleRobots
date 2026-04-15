using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T261: <see cref="ZoneObjectiveSO"/> and
    /// <see cref="ZoneObjectiveController"/>.
    ///
    /// ZoneObjectiveTests (12):
    ///   SO_FreshInstance_RequiredZones_Default_One                        ×1
    ///   SO_FreshInstance_IsComplete_False                                  ×1
    ///   SO_Evaluate_MeetsRequirement_CompletesObjective                   ×1
    ///   SO_Evaluate_BelowRequirement_DoesNotComplete                      ×1
    ///   SO_Evaluate_AlreadyComplete_DoesNotFireEventAgain                 ×1
    ///   SO_Reset_ClearsIsComplete                                          ×1
    ///   Controller_FreshInstance_ObjectiveSO_Null                         ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_Unregisters_BothChannels                     ×1
    ///   Controller_ResetObjective_CallsSOReset                            ×1
    ///   Controller_EvaluateObjective_NullDominance_PassesZero             ×1
    ///   Controller_EvaluateObjective_WithDominance_PassesCount            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneObjectiveTests
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

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneObjectiveSO CreateObjectiveSO() =>
            ScriptableObject.CreateInstance<ZoneObjectiveSO>();

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static ZoneObjectiveController CreateController() =>
            new GameObject("ZoneObjCtrl_Test").AddComponent<ZoneObjectiveController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_RequiredZones_Default_One()
        {
            var so = CreateObjectiveSO();
            Assert.AreEqual(1, so.RequiredZones,
                "RequiredZones must default to 1.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsComplete_False()
        {
            var so = CreateObjectiveSO();
            Assert.IsFalse(so.IsComplete,
                "IsComplete must be false on a fresh ZoneObjectiveSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Evaluate_MeetsRequirement_CompletesObjective()
        {
            var so    = CreateObjectiveSO();
            var evt   = CreateEvent();
            int fired = 0;
            SetField(so, "_requiredZones",       1);
            SetField(so, "_onObjectiveComplete", evt);
            evt.RegisterCallback(() => fired++);

            so.Evaluate(1);

            Assert.IsTrue(so.IsComplete,
                "Evaluate must set IsComplete when playerZoneCount >= RequiredZones.");
            Assert.AreEqual(1, fired,
                "_onObjectiveComplete must fire once when requirement is met.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Evaluate_BelowRequirement_DoesNotComplete()
        {
            var so = CreateObjectiveSO();
            SetField(so, "_requiredZones", 2);

            so.Evaluate(1);

            Assert.IsFalse(so.IsComplete,
                "Evaluate must not complete when playerZoneCount < RequiredZones.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Evaluate_AlreadyComplete_DoesNotFireEventAgain()
        {
            var so    = CreateObjectiveSO();
            var evt   = CreateEvent();
            int fired = 0;
            SetField(so, "_requiredZones",       1);
            SetField(so, "_onObjectiveComplete", evt);
            evt.RegisterCallback(() => fired++);

            so.Evaluate(1);  // first completion
            fired = 0;       // reset counter
            so.Evaluate(1);  // must be a no-op

            Assert.AreEqual(0, fired,
                "Evaluate must not fire _onObjectiveComplete again if already complete.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsIsComplete()
        {
            var so = CreateObjectiveSO();
            SetField(so, "_requiredZones", 1);

            so.Evaluate(1);
            Assert.IsTrue(so.IsComplete, "Pre-condition: objective should be complete.");

            so.Reset();

            Assert.IsFalse(so.IsComplete,
                "Reset must clear IsComplete.");

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ObjectiveSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ObjectiveSO,
                "ObjectiveSO must be null on a fresh ZoneObjectiveController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_BothChannels()
        {
            var ctrl      = CreateController();
            var objective = CreateObjectiveSO();
            var dominance = CreateDominanceSO();
            var start     = CreateEvent();
            var end       = CreateEvent();

            SetField(ctrl, "_objectiveSO",    objective);
            SetField(ctrl, "_dominanceSO",    dominance);
            SetField(ctrl, "_onMatchStarted", start);
            SetField(ctrl, "_onMatchEnded",   end);
            SetField(objective, "_requiredZones", 1);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Manually complete the objective so we can detect if Reset was called.
            objective.Evaluate(1);
            Assert.IsTrue(objective.IsComplete, "Pre-condition: objective must be complete.");

            InvokePrivate(ctrl, "OnDisable");

            // After disable, _onMatchStarted must NOT reset the objective.
            start.Raise();
            Assert.IsTrue(objective.IsComplete,
                "After OnDisable, _onMatchStarted must not reset the objective.");

            // After disable, _onMatchEnded must NOT evaluate again.
            int firedCount = 0;
            objective.Reset();
            var completionEvt = CreateEvent();
            SetField(objective, "_onObjectiveComplete", completionEvt);
            completionEvt.RegisterCallback(() => firedCount++);
            dominance.AddPlayerZone();
            end.Raise();
            Assert.AreEqual(0, firedCount,
                "After OnDisable, _onMatchEnded must not evaluate the objective.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objective);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(start);
            Object.DestroyImmediate(end);
            Object.DestroyImmediate(completionEvt);
        }

        [Test]
        public void Controller_ResetObjective_CallsSOReset()
        {
            var ctrl      = CreateController();
            var objective = CreateObjectiveSO();
            SetField(ctrl, "_objectiveSO",      objective);
            SetField(objective, "_requiredZones", 1);
            InvokePrivate(ctrl, "Awake");

            objective.Evaluate(1);
            Assert.IsTrue(objective.IsComplete, "Pre-condition: objective must be complete.");

            ctrl.ResetObjective();

            Assert.IsFalse(objective.IsComplete,
                "ResetObjective must call ZoneObjectiveSO.Reset() and clear IsComplete.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objective);
        }

        [Test]
        public void Controller_EvaluateObjective_NullDominance_PassesZero()
        {
            var ctrl      = CreateController();
            var objective = CreateObjectiveSO();
            SetField(ctrl, "_objectiveSO",      objective);
            SetField(objective, "_requiredZones", 2);  // needs 2 zones
            InvokePrivate(ctrl, "Awake");

            // No DominanceSO → controller must treat playerZoneCount as 0.
            ctrl.EvaluateObjective();

            Assert.IsFalse(objective.IsComplete,
                "EvaluateObjective must treat null DominanceSO as 0 zones and not complete a 2-zone objective.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objective);
        }

        [Test]
        public void Controller_EvaluateObjective_WithDominance_PassesCount()
        {
            var ctrl      = CreateController();
            var objective = CreateObjectiveSO();
            var dominance = CreateDominanceSO();
            SetField(ctrl, "_objectiveSO",      objective);
            SetField(ctrl, "_dominanceSO",      dominance);
            SetField(objective, "_requiredZones", 1);
            InvokePrivate(ctrl, "Awake");

            dominance.AddPlayerZone();   // PlayerZoneCount = 1
            ctrl.EvaluateObjective();

            Assert.IsTrue(objective.IsComplete,
                "EvaluateObjective must pass DominanceSO.PlayerZoneCount to Evaluate and complete the objective.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(objective);
            Object.DestroyImmediate(dominance);
        }
    }
}
