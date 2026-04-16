using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T321: <see cref="ZoneControlThreatAssessmentSO"/> and
    /// <see cref="ZoneControlThreatAssessmentController"/>.
    ///
    /// ZoneControlThreatAssessmentTests (12):
    ///   SO_FreshInstance_CurrentThreat_Low                                       ×1
    ///   SO_ComputeLevel_Rank1_NoD_ReturnsLow                                     ×1
    ///   SO_ComputeLevel_Rank2_NoD_ReturnsMedium                                  ×1
    ///   SO_ComputeLevel_Rank3_NoD_ReturnsHigh                                    ×1
    ///   SO_ComputeLevel_HasDominance_AlwaysLow                                   ×1
    ///   SO_EvaluateThreat_FiresEvent_WhenLevelChanges                            ×1
    ///   SO_Reset_SetsLow                                                         ×1
    ///   Controller_FreshInstance_AssessmentSO_Null                              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_Channel                                 ×1
    ///   Controller_Refresh_NullAssessmentSO_HidesPanel                          ×1
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

        private static ZoneControlThreatAssessmentSO CreateAssessmentSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlThreatAssessmentSO>();
            so.Reset();
            return so;
        }

        private static ZoneControlThreatAssessmentController CreateController() =>
            new GameObject("ThreatCtrl_Test")
                .AddComponent<ZoneControlThreatAssessmentController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentThreat_Low()
        {
            var so = CreateAssessmentSO();
            Assert.AreEqual(ThreatLevel.Low, so.CurrentThreat,
                "CurrentThreat must be Low on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeLevel_Rank1_NoD_ReturnsLow()
        {
            ThreatLevel level = ZoneControlThreatAssessmentSO.ComputeLevel(
                playerRank: 1, hasDominance: false);
            Assert.AreEqual(ThreatLevel.Low, level,
                "ComputeLevel must return Low when player is ranked 1st.");
        }

        [Test]
        public void SO_ComputeLevel_Rank2_NoD_ReturnsMedium()
        {
            ThreatLevel level = ZoneControlThreatAssessmentSO.ComputeLevel(
                playerRank: 2, hasDominance: false);
            Assert.AreEqual(ThreatLevel.Medium, level,
                "ComputeLevel must return Medium when player is ranked 2nd without dominance.");
        }

        [Test]
        public void SO_ComputeLevel_Rank3_NoD_ReturnsHigh()
        {
            ThreatLevel level = ZoneControlThreatAssessmentSO.ComputeLevel(
                playerRank: 3, hasDominance: false);
            Assert.AreEqual(ThreatLevel.High, level,
                "ComputeLevel must return High when player is ranked 3rd or worse without dominance.");
        }

        [Test]
        public void SO_ComputeLevel_HasDominance_AlwaysLow()
        {
            // Even with a high rank, dominance overrides to Low.
            Assert.AreEqual(ThreatLevel.Low,
                ZoneControlThreatAssessmentSO.ComputeLevel(playerRank: 3, hasDominance: true),
                "ComputeLevel must return Low when player has zone dominance, regardless of rank.");
            Assert.AreEqual(ThreatLevel.Low,
                ZoneControlThreatAssessmentSO.ComputeLevel(playerRank: 5, hasDominance: true),
                "ComputeLevel must return Low when player has zone dominance with any rank.");
        }

        [Test]
        public void SO_EvaluateThreat_FiresEvent_WhenLevelChanges()
        {
            var so         = CreateAssessmentSO();
            var onThreatCh = CreateEvent();
            SetField(so, "_onThreatChanged", onThreatCh);

            int fired = 0;
            onThreatCh.RegisterCallback(() => fired++);

            so.EvaluateThreat(3, hasDominance: false); // Low → High → fires
            Assert.AreEqual(1, fired,
                "_onThreatChanged must fire when the threat level changes.");
            Assert.AreEqual(ThreatLevel.High, so.CurrentThreat);

            so.EvaluateThreat(3, hasDominance: false); // High → High → no re-fire
            Assert.AreEqual(1, fired,
                "_onThreatChanged must not re-fire when the threat level stays the same.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onThreatCh);
        }

        [Test]
        public void SO_Reset_SetsLow()
        {
            var so = CreateAssessmentSO();
            so.EvaluateThreat(3, hasDominance: false); // set to High
            Assert.AreEqual(ThreatLevel.High, so.CurrentThreat);

            so.Reset();
            Assert.AreEqual(ThreatLevel.Low, so.CurrentThreat,
                "CurrentThreat must be Low after Reset.");

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_AssessmentSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.AssessmentSO,
                "AssessmentSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

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
        public void Controller_Refresh_NullAssessmentSO_HidesPanel()
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
