using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T408: <see cref="ZoneControlZoneDefenseSO"/> and
    /// <see cref="ZoneControlZoneDefenseController"/>.
    ///
    /// ZoneControlZoneDefenseTests (12):
    ///   SO_FreshInstance_ConsecutiveHolds_Zero                   x1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                  x1
    ///   SO_RecordMatchEnd_Held_IncrementsConsecutiveHolds        x1
    ///   SO_RecordMatchEnd_NotHeld_ResetsConsecutiveHolds         x1
    ///   SO_RecordMatchEnd_ReachesThreshold_AccumulatesBonus      x1
    ///   SO_RecordMatchEnd_ReachesThreshold_FiresEvent            x1
    ///   SO_RecordMatchEnd_BelowThreshold_NoBonus                 x1
    ///   SO_Reset_ClearsAll                                       x1
    ///   Controller_FreshInstance_DefenseSO_Null                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               x1
    ///   Controller_Refresh_NullSO_HidesPanel                     x1
    /// </summary>
    public sealed class ZoneControlZoneDefenseTests
    {
        private static ZoneControlZoneDefenseSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneDefenseSO>();

        private static ZoneControlZoneDefenseController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneDefenseController>();
        }

        [Test]
        public void SO_FreshInstance_ConsecutiveHolds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsecutiveHolds, Is.EqualTo(0));
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
        public void SO_RecordMatchEnd_Held_IncrementsConsecutiveHolds()
        {
            var so = CreateSO();
            so.RecordMatchEnd(true);
            Assert.That(so.ConsecutiveHolds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_NotHeld_ResetsConsecutiveHolds()
        {
            var so = CreateSO();
            so.RecordMatchEnd(true);
            so.RecordMatchEnd(false);
            Assert.That(so.ConsecutiveHolds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_ReachesThreshold_AccumulatesBonus()
        {
            var so        = CreateSO();
            int threshold = so.ConsecutiveHoldsForBonus;
            for (int i = 0; i < threshold; i++)
                so.RecordMatchEnd(true);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.BonusPerConsecutiveHold));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchEnd_ReachesThreshold_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneDefenseSO)
                .GetField("_onDefenseBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired     = 0;
            int threshold = so.ConsecutiveHoldsForBonus;
            channel.RegisterCallback(() => fired++);
            for (int i = 0; i < threshold; i++)
                so.RecordMatchEnd(true);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordMatchEnd_BelowThreshold_NoBonus()
        {
            var so        = CreateSO();
            int threshold = so.ConsecutiveHoldsForBonus;
            for (int i = 0; i < threshold - 1; i++)
                so.RecordMatchEnd(true);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so        = CreateSO();
            int threshold = so.ConsecutiveHoldsForBonus;
            for (int i = 0; i < threshold; i++)
                so.RecordMatchEnd(true);
            so.Reset();
            Assert.That(so.ConsecutiveHolds,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DefenseSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DefenseSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
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
            typeof(ZoneControlZoneDefenseController)
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
