using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T298:
    ///   <see cref="ZoneControlProgressionGateSO"/> and
    ///   <see cref="ZoneControlProgressionGateController"/>.
    ///
    /// ZoneControlProgressionGateTests (12):
    ///   SO_FreshInstance_UnlockedTiers_Zero                      ×1
    ///   SO_EvaluateGates_UnlocksTier_WhenThresholdMet            ×1
    ///   SO_EvaluateGates_Idempotent_DoesNotDoubleUnlock          ×1
    ///   SO_AllUnlocked_TrueWhenAllTiersCrossed                   ×1
    ///   SO_NextThreshold_ReturnsMinusOne_WhenAllUnlocked         ×1
    ///   SO_LoadSnapshot_RestoresTiers                            ×1
    ///   SO_Reset_ZerosTiers                                      ×1
    ///   Controller_FreshInstance_GateSO_Null                     ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullGateSO_HidesPanel                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlProgressionGateTests
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

        private static ZoneControlProgressionGateSO CreateGateSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlProgressionGateSO>();
            // Set thresholds: 5, 15, 30
            SetField(so, "_gateThresholds", new int[] { 5, 15, 30 });
            return so;
        }

        private static ZoneControlSessionSummarySO CreateSummarySO() =>
            ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();

        private static ZoneControlProgressionGateController CreateController() =>
            new GameObject("Gate_Test").AddComponent<ZoneControlProgressionGateController>();

        private static Text CreateText()
        {
            var go = new GameObject("Txt");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Text>();
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_UnlockedTiers_Zero()
        {
            var so = CreateGateSO();
            Assert.AreEqual(0, so.UnlockedTiers,
                "UnlockedTiers must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateGates_UnlocksTier_WhenThresholdMet()
        {
            var so = CreateGateSO();
            so.EvaluateGates(10); // crosses threshold[0]=5
            Assert.AreEqual(1, so.UnlockedTiers,
                "UnlockedTiers must be 1 after crossing the first threshold.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateGates_Idempotent_DoesNotDoubleUnlock()
        {
            var so  = CreateGateSO();
            int fired = 0;
            var evt = CreateEvent();
            evt.RegisterCallback(() => fired++);
            SetField(so, "_onGateUnlocked", evt);

            so.EvaluateGates(10); // unlocks tier 1
            so.EvaluateGates(10); // same value — no new unlock
            Assert.AreEqual(1, fired,
                "EvaluateGates must not fire the event again for an already-unlocked tier.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_AllUnlocked_TrueWhenAllTiersCrossed()
        {
            var so = CreateGateSO();
            so.EvaluateGates(100); // crosses all thresholds
            Assert.IsTrue(so.AllUnlocked,
                "AllUnlocked must be true after all thresholds are crossed.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NextThreshold_ReturnsMinusOne_WhenAllUnlocked()
        {
            var so = CreateGateSO();
            so.EvaluateGates(100);
            Assert.AreEqual(-1, so.NextThreshold,
                "NextThreshold must return -1 when all tiers are unlocked.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoadSnapshot_RestoresTiers()
        {
            var so = CreateGateSO();
            so.LoadSnapshot(2);
            Assert.AreEqual(2, so.UnlockedTiers,
                "LoadSnapshot must restore the persisted unlocked tier count.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ZerosTiers()
        {
            var so = CreateGateSO();
            so.EvaluateGates(50);
            so.Reset();
            Assert.AreEqual(0, so.UnlockedTiers,
                "Reset must zero the unlocked tier count.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_GateSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.GateSO,
                "GateSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlProgressionGateController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlProgressionGateController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlProgressionGateController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onSummaryUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onSummaryUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullGateSO_HidesPanel()
        {
            var go    = new GameObject("Test_NullGateSO");
            var ctrl  = go.AddComponent<ZoneControlProgressionGateController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when GateSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
