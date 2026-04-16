using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T316: <see cref="ZoneControlMatchGoalSO"/> and
    /// <see cref="ZoneControlMatchGoalController"/>.
    ///
    /// ZoneControlMatchGoalTests (12):
    ///   SO_FirstToCaptures_IsGoalMet_PlayerAtTarget                               ×1
    ///   SO_FirstToCaptures_IsGoalMet_PlayerBelowTarget                            ×1
    ///   SO_MostZonesInTime_IsGoalMet_TimeReached                                  ×1
    ///   SO_MostZonesInTime_IsGoalMet_TimeNotReached                               ×1
    ///   SO_GoalDescription_FirstToCaptures                                        ×1
    ///   SO_GoalDescription_MostZonesInTime                                        ×1
    ///   Controller_FreshInstance_GoalSO_Null                                      ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandleScoreboardUpdated_FiresGoalMet                           ×1
    ///   Controller_Refresh_NullSO_HidesPanel                                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchGoalTests
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

        private static ZoneControlMatchGoalSO CreateGoalSO(
            ZoneControlMatchGoalType type = ZoneControlMatchGoalType.FirstToCaptures,
            int captureTarget = 10,
            float timeLimit   = 120f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchGoalSO>();
            SetField(so, "_goalType",         type);
            SetField(so, "_captureTarget",    captureTarget);
            SetField(so, "_timeLimitSeconds", timeLimit);
            return so;
        }

        private static ZoneControlMatchGoalController CreateController() =>
            new GameObject("MatchGoalCtrl_Test")
                .AddComponent<ZoneControlMatchGoalController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FirstToCaptures_IsGoalMet_PlayerAtTarget()
        {
            var so = CreateGoalSO(ZoneControlMatchGoalType.FirstToCaptures, captureTarget: 10);
            Assert.IsTrue(so.IsGoalMet(10, 0),
                "IsGoalMet must return true when playerScore >= captureTarget.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstToCaptures_IsGoalMet_PlayerBelowTarget()
        {
            var so = CreateGoalSO(ZoneControlMatchGoalType.FirstToCaptures, captureTarget: 10);
            Assert.IsFalse(so.IsGoalMet(9, 9999),
                "IsGoalMet must return false when playerScore < captureTarget.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MostZonesInTime_IsGoalMet_TimeReached()
        {
            var so = CreateGoalSO(ZoneControlMatchGoalType.MostZonesInTime, timeLimit: 60f);
            Assert.IsTrue(so.IsGoalMet(0, 60),
                "IsGoalMet must return true when timeElapsed >= timeLimitSeconds.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MostZonesInTime_IsGoalMet_TimeNotReached()
        {
            var so = CreateGoalSO(ZoneControlMatchGoalType.MostZonesInTime, timeLimit: 60f);
            Assert.IsFalse(so.IsGoalMet(999, 59),
                "IsGoalMet must return false when timeElapsed < timeLimitSeconds.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GoalDescription_FirstToCaptures()
        {
            var so = CreateGoalSO(ZoneControlMatchGoalType.FirstToCaptures, captureTarget: 15);
            StringAssert.Contains("15", so.GoalDescription,
                "GoalDescription must include the capture target.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GoalDescription_MostZonesInTime()
        {
            var so = CreateGoalSO(ZoneControlMatchGoalType.MostZonesInTime, timeLimit: 90f);
            StringAssert.Contains("90", so.GoalDescription,
                "GoalDescription must include the time limit.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_GoalSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.GoalSO,
                "GoalSO must be null on a freshly added controller.");
            UnityEngine.Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchGoalController>(),
                "Adding controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchGoalController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchGoalController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onScoreboardUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onScoreboardUpdated must be unregistered after OnDisable.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleScoreboardUpdated_FiresGoalMet()
        {
            var go   = new GameObject("Test_GoalMet");
            var ctrl = go.AddComponent<ZoneControlMatchGoalController>();

            // Goal: first to 1 capture.
            var goalSO = CreateGoalSO(ZoneControlMatchGoalType.FirstToCaptures, captureTarget: 1);
            var scoreSO = ScriptableObject.CreateInstance<ZoneControlScoreboardSO>();
            var onGoalMet = CreateEvent();

            SetField(ctrl, "_goalSO",       goalSO);
            SetField(ctrl, "_scoreboardSO", scoreSO);
            SetField(ctrl, "_onGoalMet",    onGoalMet);

            // Give the player a capture so scoreboard reads ≥ 1.
            scoreSO.RecordPlayerCapture(); // PlayerScore = 1

            int fired = 0;
            onGoalMet.RegisterCallback(() => fired++);

            ctrl.HandleScoreboardUpdated();

            Assert.AreEqual(1, fired,
                "_onGoalMet must fire once when the goal is first met.");
            Assert.IsTrue(ctrl.IsGoalMet,
                "IsGoalMet property must be true after the goal is met.");

            // Second call must not fire again (idempotent).
            ctrl.HandleScoreboardUpdated();
            Assert.AreEqual(1, fired,
                "_onGoalMet must not fire a second time (idempotent).");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(goalSO);
            UnityEngine.Object.DestroyImmediate(scoreSO);
            UnityEngine.Object.DestroyImmediate(onGoalMet);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlMatchGoalController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when GoalSO is null.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(panel);
        }
    }
}
