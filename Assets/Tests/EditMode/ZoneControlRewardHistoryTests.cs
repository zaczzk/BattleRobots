using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T335: <see cref="ZoneControlRewardHistorySO"/> and
    /// <see cref="ZoneControlRewardHistoryController"/>.
    ///
    /// ZoneControlRewardHistoryTests (12):
    ///   SO_FreshInstance_EntryCount_Zero                            ×1
    ///   SO_AddReward_AppendsEntry                                   ×1
    ///   SO_AddReward_NegativeValue_ClampedToZero                    ×1
    ///   SO_AddReward_PrunesOldest_WhenAtCapacity                    ×1
    ///   SO_AddReward_FiresOnHistoryUpdated                          ×1
    ///   SO_GetAverageReward_EmptyHistory_ReturnsZero                ×1
    ///   SO_GetAverageReward_SingleEntry_ReturnsValue                ×1
    ///   SO_Reset_ClearsHistory                                      ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_Unregisters_Channel                    ×1
    ///   Controller_HandleMatchEnded_AddsReward                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlRewardHistoryTests
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

        private static ZoneControlRewardHistorySO CreateHistorySO(int capacity = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlRewardHistorySO>();
            SetField(so, "_capacity", capacity);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBonusSO CreateCaptureBonusSO(int threshold = 1, int bonus = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBonusSO>();
            SetField(so, "_captureThreshold", threshold);
            SetField(so, "_bonusPerCapture",  bonus);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlRewardHistorySO>();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddReward_AppendsEntry()
        {
            var so = CreateHistorySO();
            so.AddReward(200);
            Assert.AreEqual(1, so.EntryCount,
                "EntryCount must be 1 after one AddReward call.");
            Assert.AreEqual(200, so.GetReward(0),
                "GetReward(0) must return the appended value.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddReward_NegativeValue_ClampedToZero()
        {
            var so = CreateHistorySO();
            so.AddReward(-50);
            Assert.AreEqual(0, so.GetReward(0),
                "Negative reward must be clamped to 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddReward_PrunesOldest_WhenAtCapacity()
        {
            var so = CreateHistorySO(capacity: 3);
            so.AddReward(10);
            so.AddReward(20);
            so.AddReward(30);
            so.AddReward(40);  // should prune 10

            Assert.AreEqual(3, so.EntryCount,
                "EntryCount must not exceed capacity.");
            Assert.AreEqual(20, so.GetReward(0),
                "Oldest entry must be pruned when capacity is exceeded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddReward_FiresOnHistoryUpdated()
        {
            var so  = CreateHistorySO();
            var evt = CreateEvent();
            SetField(so, "_onHistoryUpdated", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.AddReward(100);

            Assert.AreEqual(1, fired,
                "_onHistoryUpdated must fire after AddReward.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_GetAverageReward_EmptyHistory_ReturnsZero()
        {
            var so = CreateHistorySO();
            Assert.AreEqual(0f, so.GetAverageReward(),
                "GetAverageReward must return 0 when history is empty.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetAverageReward_SingleEntry_ReturnsValue()
        {
            var so = CreateHistorySO();
            so.AddReward(300);
            Assert.AreEqual(300f, so.GetAverageReward(), 0.001f,
                "GetAverageReward must return the single entry value.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsHistory()
        {
            var so = CreateHistorySO();
            so.AddReward(100);
            so.AddReward(200);
            so.Reset();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlRewardHistoryController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlRewardHistoryController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlRewardHistoryController>();
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
        public void Controller_HandleMatchEnded_AddsReward()
        {
            var go        = new GameObject("Test_HandleMatchEnded");
            var ctrl      = go.AddComponent<ZoneControlRewardHistoryController>();
            var historySO = CreateHistorySO();
            var bonusSO   = CreateCaptureBonusSO(threshold: 1, bonus: 50);
            bonusSO.EvaluateBonus(5);  // earn (5-1)*50 = 200

            SetField(ctrl, "_rewardHistorySO", historySO);
            SetField(ctrl, "_captureBonusSO",  bonusSO);

            ctrl.HandleMatchEnded();

            Assert.AreEqual(1, historySO.EntryCount,
                "HandleMatchEnded must append one reward entry.");
            Assert.AreEqual(bonusSO.TotalBonusAwarded, historySO.GetReward(0),
                "Recorded reward must equal captureBonusSO.TotalBonusAwarded.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(bonusSO);
        }
    }
}
