using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T321: <see cref="ZoneControlThreatAssessmentSO"/> and
    /// <see cref="ZoneControlThreatAssessmentController"/>.
    ///
    /// ZoneControlThreatAssessmentTests (12):
    ///   SO_FreshInstance_ThreatLevel_Low                              ×1
    ///   SO_EvaluateThreat_Rank1_Low                                   ×1
    ///   SO_EvaluateThreat_Rank2_Medium                                ×1
    ///   SO_EvaluateThreat_Rank3_NoDominance_High                      ×1
    ///   SO_EvaluateThreat_Rank3_HasDominance_Medium                   ×1
    ///   SO_EvaluateThreat_ChangedThreat_FiresEvent                    ×1
    ///   SO_EvaluateThreat_SameThreat_NoEvent                          ×1
    ///   SO_Reset_SetsLow                                              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_Refresh_NullSO_HidesPanel                          ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlThreatAssessmentTests
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

        private static ZoneControlThreatAssessmentSO CreateAssessmentSO(
            int mediumRank = 2, int highRank = 3)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlThreatAssessmentSO>();
            SetField(so, "_mediumThreatRank", mediumRank);
            SetField(so, "_highThreatRank",   highRank);
            so.Reset();
            return so;
        }

        private static ZoneControlThreatAssessmentController CreateController() =>
            new GameObject("ThreatCtrl_Test")
                .AddComponent<ZoneControlThreatAssessmentController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_ThreatLevel_Low()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlThreatAssessmentSO>();
            Assert.AreEqual(ThreatLevel.Low, so.CurrentThreat,
                "CurrentThreat must be Low on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateThreat_Rank1_Low()
        {
            var so = CreateAssessmentSO(mediumRank: 2, highRank: 3);
            so.EvaluateThreat(playerRank: 1, hasDominance: false);
            Assert.AreEqual(ThreatLevel.Low, so.CurrentThreat,
                "PlayerRank == 1 (leading) must always produce Low threat.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateThreat_Rank2_Medium()
        {
            var so = CreateAssessmentSO(mediumRank: 2, highRank: 3);
            so.EvaluateThreat(playerRank: 2, hasDominance: false);
            Assert.AreEqual(ThreatLevel.Medium, so.CurrentThreat,
                "PlayerRank 2 must produce Medium threat (< highThreatRank).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateThreat_Rank3_NoDominance_High()
        {
            var so = CreateAssessmentSO(mediumRank: 2, highRank: 3);
            so.EvaluateThreat(playerRank: 3, hasDominance: false);
            Assert.AreEqual(ThreatLevel.High, so.CurrentThreat,
                "PlayerRank >= highThreatRank without dominance must produce High threat.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateThreat_Rank3_HasDominance_Medium()
        {
            var so = CreateAssessmentSO(mediumRank: 2, highRank: 3);
            so.EvaluateThreat(playerRank: 3, hasDominance: true);
            Assert.AreEqual(ThreatLevel.Medium, so.CurrentThreat,
                "PlayerRank >= highThreatRank WITH dominance must fall back to Medium threat.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateThreat_ChangedThreat_FiresEvent()
        {
            var so  = CreateAssessmentSO(mediumRank: 2, highRank: 3);
            var evt = CreateEvent();
            SetField(so, "_onThreatChanged", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            // Low → Medium change must fire.
            so.EvaluateThreat(2, false);
            Assert.AreEqual(1, fired,
                "_onThreatChanged must fire when threat level changes.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateThreat_SameThreat_NoEvent()
        {
            var so  = CreateAssessmentSO(mediumRank: 2, highRank: 3);
            var evt = CreateEvent();
            SetField(so, "_onThreatChanged", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            // Low → Low: no change.
            so.EvaluateThreat(1, false);
            so.EvaluateThreat(1, true);
            Assert.AreEqual(0, fired,
                "_onThreatChanged must NOT fire when threat level stays the same.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_SetsLow()
        {
            var so = CreateAssessmentSO(mediumRank: 2, highRank: 3);
            so.EvaluateThreat(3, false); // High
            so.Reset();
            Assert.AreEqual(ThreatLevel.Low, so.CurrentThreat,
                "CurrentThreat must be Low after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlThreatAssessmentController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlThreatAssessmentController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlThreatAssessmentController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onScoreboardUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onScoreboardUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlThreatAssessmentController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when AssessmentSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
