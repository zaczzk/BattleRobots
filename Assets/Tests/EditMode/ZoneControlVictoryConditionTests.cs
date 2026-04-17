using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T342: <see cref="ZoneControlVictoryConditionSO"/> and
    /// <see cref="ZoneControlVictoryEvaluatorController"/>.
    ///
    /// ZoneControlVictoryConditionTests (12):
    ///   SO_FreshInstance_DefaultVictoryType_IsFirstToCaptures             ×1
    ///   SO_FreshInstance_DefaultCaptureTarget_Is10                        ×1
    ///   SO_IsVictoryMet_FirstToCaptures_BelowTarget_ReturnsFalse          ×1
    ///   SO_IsVictoryMet_FirstToCaptures_AtTarget_ReturnsTrue              ×1
    ///   SO_IsVictoryMet_MostZonesHeld_BelowTimeLimit_ReturnsFalse         ×1
    ///   SO_IsVictoryMet_MostZonesHeld_AtTimeLimit_ReturnsTrue             ×1
    ///   SO_VictoryDescription_FirstToCaptures_ContainsCaptureTarget       ×1
    ///   SO_VictoryDescription_MostZonesHeld_ContainsTimeLimit             ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channels                         ×1
    ///   Controller_HandleScoreUpdated_FiresVictory_WhenConditionMet       ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlVictoryConditionTests
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

        private static ZoneControlVictoryConditionSO CreateVictorySO(
            ZoneControlVictoryType type       = ZoneControlVictoryType.FirstToCaptures,
            int                    target     = 10,
            float                  timeLimit  = 120f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlVictoryConditionSO>();
            SetField(so, "_victoryType",       type);
            SetField(so, "_captureTarget",     target);
            SetField(so, "_timeLimitSeconds",  timeLimit);
            return so;
        }

        private static ZoneControlScoreboardSO CreateScoreboardSO(int maxBots = 1)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlScoreboardSO>();
            SetField(so, "_maxBots", maxBots);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_DefaultVictoryType_IsFirstToCaptures()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlVictoryConditionSO>();
            Assert.AreEqual(ZoneControlVictoryType.FirstToCaptures, so.VictoryType,
                "Default VictoryType must be FirstToCaptures.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DefaultCaptureTarget_Is10()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlVictoryConditionSO>();
            Assert.AreEqual(10, so.CaptureTarget,
                "Default CaptureTarget must be 10.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsVictoryMet_FirstToCaptures_BelowTarget_ReturnsFalse()
        {
            var so = CreateVictorySO(ZoneControlVictoryType.FirstToCaptures, target: 5);
            Assert.IsFalse(so.IsVictoryMet(4, 0f),
                "IsVictoryMet must return false when captures < CaptureTarget.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsVictoryMet_FirstToCaptures_AtTarget_ReturnsTrue()
        {
            var so = CreateVictorySO(ZoneControlVictoryType.FirstToCaptures, target: 5);
            Assert.IsTrue(so.IsVictoryMet(5, 0f),
                "IsVictoryMet must return true when captures == CaptureTarget.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsVictoryMet_MostZonesHeld_BelowTimeLimit_ReturnsFalse()
        {
            var so = CreateVictorySO(ZoneControlVictoryType.MostZonesHeld, timeLimit: 60f);
            Assert.IsFalse(so.IsVictoryMet(99, 59f),
                "IsVictoryMet must return false when timeElapsed < TimeLimitSeconds.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsVictoryMet_MostZonesHeld_AtTimeLimit_ReturnsTrue()
        {
            var so = CreateVictorySO(ZoneControlVictoryType.MostZonesHeld, timeLimit: 60f);
            Assert.IsTrue(so.IsVictoryMet(0, 60f),
                "IsVictoryMet must return true when timeElapsed >= TimeLimitSeconds.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VictoryDescription_FirstToCaptures_ContainsCaptureTarget()
        {
            var so = CreateVictorySO(ZoneControlVictoryType.FirstToCaptures, target: 7);
            StringAssert.Contains("7", so.VictoryDescription,
                "VictoryDescription must mention the capture target.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VictoryDescription_MostZonesHeld_ContainsTimeLimit()
        {
            var so = CreateVictorySO(ZoneControlVictoryType.MostZonesHeld, timeLimit: 90f);
            StringAssert.Contains("90", so.VictoryDescription,
                "VictoryDescription must mention the time limit.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlVictoryEvaluatorController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlVictoryEvaluatorController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlVictoryEvaluatorController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onScoreUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onScoreUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleScoreUpdated_FiresVictory_WhenConditionMet()
        {
            var go           = new GameObject("Test_VictoryFired");
            var ctrl         = go.AddComponent<ZoneControlVictoryEvaluatorController>();
            var victorySO    = CreateVictorySO(ZoneControlVictoryType.FirstToCaptures, target: 3);
            var scoreboardSO = CreateScoreboardSO();
            var outEvt       = CreateEvent();

            SetField(ctrl, "_victorySO",         victorySO);
            SetField(ctrl, "_scoreboardSO",      scoreboardSO);
            SetField(ctrl, "_onVictoryAchieved", outEvt);

            // Put player at exactly 3 captures.
            scoreboardSO.RecordPlayerCapture();
            scoreboardSO.RecordPlayerCapture();
            scoreboardSO.RecordPlayerCapture();

            int firedCount = 0;
            outEvt.RegisterCallback(() => firedCount++);

            ctrl.HandleScoreUpdated();

            Assert.IsTrue(ctrl.IsVictoryAchieved,
                "IsVictoryAchieved must be true after condition is met.");
            Assert.AreEqual(1, firedCount,
                "_onVictoryAchieved must fire exactly once when condition is met.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(victorySO);
            Object.DestroyImmediate(scoreboardSO);
            Object.DestroyImmediate(outEvt);
        }
    }
}
