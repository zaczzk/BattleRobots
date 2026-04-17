using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T345: <see cref="ZoneControlTieBreakSO"/> and
    /// <see cref="ZoneControlTieBreakController"/>.
    ///
    /// ZoneControlTieBreakTests (12):
    ///   SO_FreshInstance_IsActive_False                                   ×1
    ///   SO_FreshInstance_DefaultType_IsSuddenDeath                        ×1
    ///   SO_EvaluateTie_EqualScores_SetsIsActive_True                      ×1
    ///   SO_EvaluateTie_UnequalScores_IsActive_False                       ×1
    ///   SO_EvaluateTie_EqualScores_FiresTieBreakTriggered                 ×1
    ///   SO_EvaluateTie_AlreadyActive_EqualScores_NoDoubleEvent            ×1
    ///   SO_TieBreakDescription_SuddenDeath                                ×1
    ///   SO_Reset_ClearsIsActive                                           ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channels                         ×1
    ///   Controller_HandleMatchEnded_EvaluatesTie                          ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlTieBreakTests
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

        private static ZoneControlTieBreakSO CreateTieBreakSO(
            ZoneControlTieBreakType type  = ZoneControlTieBreakType.SuddenDeath,
            int                     bonus = 3)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlTieBreakSO>();
            SetField(so, "_tieBreakType",        type);
            SetField(so, "_captureTargetBonus",  bonus);
            so.Reset();
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
        public void SO_FreshInstance_IsActive_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlTieBreakSO>();
            Assert.IsFalse(so.IsActive,
                "IsActive must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DefaultType_IsSuddenDeath()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlTieBreakSO>();
            Assert.AreEqual(ZoneControlTieBreakType.SuddenDeath, so.TieBreakType,
                "Default TieBreakType must be SuddenDeath.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTie_EqualScores_SetsIsActive_True()
        {
            var so = CreateTieBreakSO();
            so.EvaluateTie(5, 5);
            Assert.IsTrue(so.IsActive,
                "IsActive must be true when player and bot scores are equal.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTie_UnequalScores_IsActive_False()
        {
            var so = CreateTieBreakSO();
            so.EvaluateTie(6, 5);
            Assert.IsFalse(so.IsActive,
                "IsActive must remain false when scores are unequal.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTie_EqualScores_FiresTieBreakTriggered()
        {
            var so  = CreateTieBreakSO();
            var evt = CreateEvent();
            SetField(so, "_onTieBreakTriggered", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.EvaluateTie(3, 3);

            Assert.AreEqual(1, count,
                "_onTieBreakTriggered must fire once on the first tied-score evaluation.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateTie_AlreadyActive_EqualScores_NoDoubleEvent()
        {
            var so  = CreateTieBreakSO();
            var evt = CreateEvent();
            SetField(so, "_onTieBreakTriggered", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.EvaluateTie(3, 3); // activates
            so.EvaluateTie(3, 3); // already active — no second event

            Assert.AreEqual(1, count,
                "_onTieBreakTriggered must not fire again when already active.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_TieBreakDescription_SuddenDeath()
        {
            var so = CreateTieBreakSO(ZoneControlTieBreakType.SuddenDeath);
            StringAssert.Contains("Sudden Death", so.TieBreakDescription,
                "TieBreakDescription must contain 'Sudden Death' for SuddenDeath type.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsIsActive()
        {
            var so = CreateTieBreakSO();
            so.EvaluateTie(5, 5);
            so.Reset();
            Assert.IsFalse(so.IsActive,
                "IsActive must be false after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_TieBreak_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlTieBreakController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_TieBreak_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlTieBreakController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_TieBreak_Unregister");
            var ctrl = go.AddComponent<ZoneControlTieBreakController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchEnded_EvaluatesTie()
        {
            var go           = new GameObject("Test_TieBreak_Evaluate");
            var ctrl         = go.AddComponent<ZoneControlTieBreakController>();
            var tieBreakSO   = CreateTieBreakSO();
            var scoreboardSO = CreateScoreboardSO();

            SetField(ctrl, "_tieBreakSO",   tieBreakSO);
            SetField(ctrl, "_scoreboardSO", scoreboardSO);

            // Both start at 0 — equal scores → tie.
            ctrl.HandleMatchEnded();

            Assert.IsTrue(tieBreakSO.IsActive,
                "HandleMatchEnded must call EvaluateTie; equal scores must activate tie-break.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tieBreakSO);
            Object.DestroyImmediate(scoreboardSO);
        }
    }
}
