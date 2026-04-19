using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T467: <see cref="ZoneControlDominanceStreakSO"/> and
    /// <see cref="ZoneControlDominanceStreakController"/>.
    ///
    /// ZoneControlDominanceStreakTests (12):
    ///   SO_FreshInstance_ConsecutiveHolds_Zero                            x1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                          x1
    ///   SO_RecordMatchEnd_HadDominance_IncrementsStreak                  x1
    ///   SO_RecordMatchEnd_NoDominance_ResetsStreak                        x1
    ///   SO_RecordMatchEnd_BelowThreshold_NoBonus                         x1
    ///   SO_RecordMatchEnd_ReachesThreshold_AwardsBonus                   x1
    ///   SO_RecordMatchEnd_BeyondThreshold_AwardsBonusEachTime            x1
    ///   SO_RecordMatchEnd_FiresBonusEvent                                 x1
    ///   SO_Reset_ClearsAll                                                x1
    ///   Controller_FreshInstance_DominanceStreakSO_Null                   x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                         x1
    ///   Controller_Refresh_NullSO_HidesPanel                              x1
    /// </summary>
    public sealed class ZoneControlDominanceStreakTests
    {
        private static ZoneControlDominanceStreakSO CreateSO(
            int consecutiveForBonus = 3, int bonusPerConsecutive = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlDominanceStreakSO>();
            var t = typeof(ZoneControlDominanceStreakSO);
            t.GetField("_consecutiveDominanceForBonus", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, consecutiveForBonus);
            t.GetField("_bonusPerConsecutive",          BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, bonusPerConsecutive);
            so.Reset();
            return so;
        }

        private static ZoneControlDominanceStreakController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlDominanceStreakController>();
        }

        [Test]
        public void SO_FreshInstance_ConsecutiveHolds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsecutiveDominanceHolds, Is.EqualTo(0));
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
        public void SO_RecordMatchEnd_HadDominance_IncrementsStreak()
        {
            var so = CreateSO(consecutiveForBonus: 5);
            so.RecordMatchEnd(true);
            Assert.That(so.ConsecutiveDominanceHolds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_NoDominance_ResetsStreak()
        {
            var so = CreateSO(consecutiveForBonus: 5);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(false);
            Assert.That(so.ConsecutiveDominanceHolds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_BelowThreshold_NoBonus()
        {
            var so = CreateSO(consecutiveForBonus: 3);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_ReachesThreshold_AwardsBonus()
        {
            var so = CreateSO(consecutiveForBonus: 3, bonusPerConsecutive: 200);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_BeyondThreshold_AwardsBonusEachTime()
        {
            var so = CreateSO(consecutiveForBonus: 2, bonusPerConsecutive: 100);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_FiresBonusEvent()
        {
            var so = CreateSO(consecutiveForBonus: 2);
            int fireCount = 0;
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlDominanceStreakSO)
                .GetField("_onDominanceStreakBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fireCount++);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            Assert.That(fireCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(consecutiveForBonus: 2, bonusPerConsecutive: 100);
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(true);
            so.Reset();
            Assert.That(so.ConsecutiveDominanceHolds, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DominanceStreakSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DominanceStreakSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlDominanceStreakController)
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
