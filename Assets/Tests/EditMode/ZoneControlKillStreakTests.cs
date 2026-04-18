using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T434: <see cref="ZoneControlKillStreakSO"/> and
    /// <see cref="ZoneControlKillStreakController"/>.
    ///
    /// ZoneControlKillStreakTests (12):
    ///   SO_FreshInstance_IsStreaking_False                           x1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                     x1
    ///   SO_RecordKill_BelowThreshold_NoStreak                       x1
    ///   SO_RecordKill_AtThreshold_StartsSurge                       x1
    ///   SO_RecordKill_FiresKillDuringSurge_WhenStreaking             x1
    ///   SO_RecordKill_BonusAccumulates_DuringSurge                  x1
    ///   SO_Tick_Prunes_OldKills_EndsStreak                          x1
    ///   SO_StreakCount_Increments_OnNewStreak                        x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_KillStreakSO_Null                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                   x1
    ///   Controller_Refresh_NullSO_HidesPanel                        x1
    /// </summary>
    public sealed class ZoneControlKillStreakTests
    {
        private static ZoneControlKillStreakSO CreateSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlKillStreakSO>();
            // Force threshold=3, window=6s
            typeof(ZoneControlKillStreakSO)
                .GetField("_streakThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 3);
            typeof(ZoneControlKillStreakSO)
                .GetField("_streakWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 6f);
            typeof(ZoneControlKillStreakSO)
                .GetField("_bonusPerKillInStreak", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 50);
            so.Reset();
            return so;
        }

        private static ZoneControlKillStreakController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlKillStreakController>();
        }

        [Test]
        public void SO_FreshInstance_IsStreaking_False()
        {
            var so = CreateSO();
            Assert.That(so.IsStreaking, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordKill_BelowThreshold_NoStreak()
        {
            var so = CreateSO();
            so.RecordKill(0f);
            so.RecordKill(1f);
            Assert.That(so.IsStreaking, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordKill_AtThreshold_StartsSurge()
        {
            var so = CreateSO();
            so.RecordKill(0f);
            so.RecordKill(1f);
            so.RecordKill(2f); // threshold = 3
            Assert.That(so.IsStreaking, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordKill_FiresKillDuringSurge_WhenStreaking()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlKillStreakSO)
                .GetField("_onKillDuringSurge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            // Build streak
            so.RecordKill(0f);
            so.RecordKill(1f);
            so.RecordKill(2f); // streak starts
            int beforeExtra = fired;
            so.RecordKill(3f); // kill during surge → should fire
            Assert.That(fired, Is.GreaterThan(beforeExtra));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordKill_BonusAccumulates_DuringSurge()
        {
            var so = CreateSO();
            // Bring to threshold
            so.RecordKill(0f);
            so.RecordKill(1f);
            so.RecordKill(2f); // streak starts
            // Kill during surge
            so.RecordKill(3f);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_Prunes_OldKills_EndsStreak()
        {
            var so = CreateSO();
            so.RecordKill(0f);
            so.RecordKill(1f);
            so.RecordKill(2f); // streak = true
            Assert.That(so.IsStreaking, Is.True);
            // Tick far in the future to prune all old kills
            so.Tick(100f);
            Assert.That(so.IsStreaking, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StreakCount_Increments_OnNewStreak()
        {
            var so = CreateSO();
            Assert.That(so.StreakCount, Is.EqualTo(0));
            so.RecordKill(0f);
            so.RecordKill(1f);
            so.RecordKill(2f); // first streak
            Assert.That(so.StreakCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordKill(0f);
            so.RecordKill(1f);
            so.RecordKill(2f);
            so.Reset();
            Assert.That(so.IsStreaking,       Is.False);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.StreakCount,       Is.EqualTo(0));
            Assert.That(so.CurrentWindowKills, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_KillStreakSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.KillStreakSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlKillStreakController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
