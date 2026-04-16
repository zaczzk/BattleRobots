using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T330: <see cref="ZoneControlCaptureStreakRewardSO"/> and
    /// <see cref="ZoneControlCaptureStreakRewardController"/>.
    ///
    /// ZoneControlCaptureStreakRewardTests (12):
    ///   SO_FreshInstance_TotalReward_Zero                        ×1
    ///   SO_EvaluateStreak_BelowFirstMilestone_NoReward           ×1
    ///   SO_EvaluateStreak_MeetsMilestone_AwardsReward            ×1
    ///   SO_EvaluateStreak_CrossesMultipleMilestones_AccumulatesReward ×1
    ///   SO_EvaluateStreak_MilestoneNotReCrossed_IdempotentAboveThreshold ×1
    ///   SO_EvaluateStreak_FiresMilestoneEvent                    ×1
    ///   SO_NextMilestone_ReturnsFirstPendingTier                 ×1
    ///   SO_NextMilestone_ReturnsNegativeOne_WhenAllUnlocked      ×1
    ///   SO_Reset_ClearsProgress                                  ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_Refresh_NullRewardSO_HidesPanel               ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlCaptureStreakRewardTests
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

        private static ZoneControlCaptureStreakRewardSO CreateRewardSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureStreakRewardSO>();
            // Default milestones {3,5,10,20}, rewardPerMilestone=100
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalReward_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureStreakRewardSO>();
            Assert.AreEqual(0, so.TotalRewardAwarded,
                "TotalRewardAwarded must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateStreak_BelowFirstMilestone_NoReward()
        {
            var so = CreateRewardSO(); // first milestone = 3
            so.EvaluateStreak(2);
            Assert.AreEqual(0, so.TotalRewardAwarded,
                "No reward must be awarded when streak is below the first milestone.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateStreak_MeetsMilestone_AwardsReward()
        {
            var so = CreateRewardSO(); // milestone[0]=3, rewardPerMilestone=100
            so.EvaluateStreak(3);
            Assert.AreEqual(100, so.TotalRewardAwarded,
                "EvaluateStreak must award rewardPerMilestone when meeting a milestone.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateStreak_CrossesMultipleMilestones_AccumulatesReward()
        {
            var so = CreateRewardSO(); // milestones {3,5,10,20}, reward=100
            so.EvaluateStreak(10); // crosses milestones 3, 5, 10 → 3×100 = 300
            Assert.AreEqual(300, so.TotalRewardAwarded,
                "EvaluateStreak must award for every milestone crossed in one call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateStreak_MilestoneNotReCrossed_IdempotentAboveThreshold()
        {
            var so = CreateRewardSO(); // milestone[0]=3
            so.EvaluateStreak(3);  // crosses milestone 3
            so.EvaluateStreak(4);  // still above 3, below 5 — no new milestone
            Assert.AreEqual(100, so.TotalRewardAwarded,
                "Already-crossed milestones must not be re-awarded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateStreak_FiresMilestoneEvent()
        {
            var so  = CreateRewardSO();
            var evt = CreateEvent();
            SetField(so, "_onMilestoneReached", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.EvaluateStreak(3); // crosses milestone at index 0

            Assert.AreEqual(1, fired,
                "_onMilestoneReached must fire once per newly crossed milestone.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_NextMilestone_ReturnsFirstPendingTier()
        {
            var so = CreateRewardSO(); // milestones {3,5,10,20}
            Assert.AreEqual(3, so.NextMilestone,
                "NextMilestone must return the first milestone when none are crossed.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NextMilestone_ReturnsNegativeOne_WhenAllUnlocked()
        {
            var so = CreateRewardSO(); // milestones {3,5,10,20}
            so.EvaluateStreak(20);    // crosses all 4 milestones
            Assert.AreEqual(-1, so.NextMilestone,
                "NextMilestone must return -1 when all milestones have been crossed.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsProgress()
        {
            var so = CreateRewardSO();
            so.EvaluateStreak(10); // cross 3 milestones
            so.Reset();
            Assert.AreEqual(0, so.TotalRewardAwarded,
                "TotalRewardAwarded must be 0 after Reset.");
            Assert.AreEqual(0, so.UnlockedMilestoneCount,
                "UnlockedMilestoneCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlCaptureStreakRewardController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlCaptureStreakRewardController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_Refresh_NullRewardSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlCaptureStreakRewardController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when RewardSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
