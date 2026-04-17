using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlChallengeStreakTests
    {
        private static ZoneControlChallengeStreakSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlChallengeStreakSO>();

        private static ZoneControlChallengeStreakController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlChallengeStreakController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_StreakCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StreakCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalRewardsEarned_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalRewardsEarned, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCompletion_IncrementsStreak()
        {
            var so = CreateSO();
            so.RecordCompletion();
            Assert.That(so.StreakCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCompletion_AccumulatesTotalRewards()
        {
            var so = CreateSO();
            so.RecordCompletion();
            int expected = so.RewardBase + 1 * so.RewardPerStreak;
            Assert.That(so.TotalRewardsEarned, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCompletion_FiresStreakIncreasedEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlChallengeStreakSO)
                .GetField("_onStreakIncreased",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCompletion();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordFailure_WithStreak_ResetsStreakAndFiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlChallengeStreakSO)
                .GetField("_onStreakBroken",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCompletion();
            so.RecordFailure();

            Assert.That(so.StreakCount, Is.EqualTo(0));
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordFailure_WithoutStreak_DoesNotFireEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlChallengeStreakSO)
                .GetField("_onStreakBroken",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordFailure();

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetCurrentReward_IncreasesWith_Streak()
        {
            var so = CreateSO();
            int reward0 = so.GetCurrentReward();
            so.RecordCompletion();
            int reward1 = so.GetCurrentReward();
            Assert.That(reward1, Is.GreaterThan(reward0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCompletion();
            so.RecordCompletion();
            so.Reset();
            Assert.That(so.StreakCount,        Is.EqualTo(0));
            Assert.That(so.TotalRewardsEarned, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_StreakSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.StreakSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullStreakSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlChallengeStreakController)
                .GetField("_panel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
