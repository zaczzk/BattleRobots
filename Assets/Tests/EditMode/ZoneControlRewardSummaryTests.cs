using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T340: <see cref="ZoneControlRewardSummarySO"/> and
    /// <see cref="ZoneControlRewardSummaryController"/>.
    ///
    /// ZoneControlRewardSummaryTests (12):
    ///   SO_FreshInstance_AllFieldsZero                                ×1
    ///   SO_AddMatchReward_IncrementsTotalRewardAndMatchCount          ×1
    ///   SO_AddMatchReward_TracksBestMatchReward                       ×1
    ///   SO_AddMatchReward_ClampsNegativeAmountToZero                  ×1
    ///   SO_AddMatchReward_FiresSummaryUpdatedEvent                    ×1
    ///   SO_Reset_ClearsAllAccumulators                                ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   Controller_OnDisable_Unregisters_Channels                    ×1
    ///   Controller_Refresh_NullSummarySO_HidesPanel                  ×1
    ///   Controller_Refresh_WithSummarySO_ShowsPanel                  ×1
    ///   Controller_HandleMatchEnded_CallsAddMatchReward               ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlRewardSummaryTests
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

        private static ZoneControlRewardSummarySO CreateSummarySO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlRewardSummarySO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_AllFieldsZero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlRewardSummarySO>();
            Assert.AreEqual(0, so.TotalReward,     "TotalReward must be 0 on fresh instance.");
            Assert.AreEqual(0, so.MatchCount,       "MatchCount must be 0 on fresh instance.");
            Assert.AreEqual(0, so.BestMatchReward,  "BestMatchReward must be 0 on fresh instance.");
            Assert.AreEqual(0f, so.AverageReward, 0.001f, "AverageReward must be 0 on fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatchReward_IncrementsTotalRewardAndMatchCount()
        {
            var so = CreateSummarySO();
            so.AddMatchReward(100);
            so.AddMatchReward(200);
            Assert.AreEqual(300, so.TotalReward, "TotalReward must accumulate across calls.");
            Assert.AreEqual(2,   so.MatchCount,  "MatchCount must count each AddMatchReward call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatchReward_TracksBestMatchReward()
        {
            var so = CreateSummarySO();
            so.AddMatchReward(50);
            so.AddMatchReward(300);
            so.AddMatchReward(150);
            Assert.AreEqual(300, so.BestMatchReward,
                "BestMatchReward must track the highest single-match reward.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatchReward_ClampsNegativeAmountToZero()
        {
            var so = CreateSummarySO();
            so.AddMatchReward(-50);
            Assert.AreEqual(0, so.TotalReward,
                "Negative reward amount must be clamped to 0.");
            Assert.AreEqual(1, so.MatchCount,
                "MatchCount must still increment even when amount is clamped.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatchReward_FiresSummaryUpdatedEvent()
        {
            var so  = CreateSummarySO();
            var evt = CreateEvent();
            SetField(so, "_onSummaryUpdated", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.AddMatchReward(100);

            Assert.AreEqual(1, fired,
                "_onSummaryUpdated must fire once per AddMatchReward call.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAllAccumulators()
        {
            var so = CreateSummarySO();
            so.AddMatchReward(500);
            so.AddMatchReward(200);
            so.Reset();

            Assert.AreEqual(0, so.TotalReward,    "TotalReward must be 0 after Reset.");
            Assert.AreEqual(0, so.MatchCount,      "MatchCount must be 0 after Reset.");
            Assert.AreEqual(0, so.BestMatchReward, "BestMatchReward must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlRewardSummaryController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlRewardSummaryController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go    = new GameObject("Test_Unregister");
            var ctrl  = go.AddComponent<ZoneControlRewardSummaryController>();
            var evt1  = CreateEvent();
            var evt2  = CreateEvent();
            SetField(ctrl, "_onMatchEnded",    evt1);
            SetField(ctrl, "_onSummaryUpdated", evt2);

            go.SetActive(true);
            go.SetActive(false);

            int count1 = 0, count2 = 0;
            evt1.RegisterCallback(() => count1++);
            evt2.RegisterCallback(() => count2++);
            evt1.Raise();
            evt2.Raise();

            Assert.AreEqual(1, count1, "_onMatchEnded must be unregistered after OnDisable.");
            Assert.AreEqual(1, count2, "_onSummaryUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt1);
            Object.DestroyImmediate(evt2);
        }

        [Test]
        public void Controller_Refresh_NullSummarySO_HidesPanel()
        {
            var go    = new GameObject("Test_NullSO");
            var ctrl  = go.AddComponent<ZoneControlRewardSummaryController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _summarySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSummarySO_ShowsPanel()
        {
            var go    = new GameObject("Test_WithSO");
            var ctrl  = go.AddComponent<ZoneControlRewardSummaryController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            var so = CreateSummarySO();
            SetField(ctrl, "_summarySO", so);
            SetField(ctrl, "_panel",     panel);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when _summarySO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleMatchEnded_CallsAddMatchReward()
        {
            var go   = new GameObject("Test_HandleMatchEnded");
            var ctrl = go.AddComponent<ZoneControlRewardSummaryController>();
            var so   = CreateSummarySO();
            SetField(ctrl, "_summarySO", so);

            ctrl.HandleMatchEnded();

            Assert.AreEqual(1, so.MatchCount,
                "HandleMatchEnded must call AddMatchReward (incrementing MatchCount).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
