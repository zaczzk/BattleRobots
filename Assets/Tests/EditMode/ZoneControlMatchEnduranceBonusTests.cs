using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T417: <see cref="ZoneControlMatchEnduranceBonusSO"/> and
    /// <see cref="ZoneControlMatchEnduranceBonusController"/>.
    ///
    /// ZoneControlMatchEnduranceBonusTests (13):
    ///   SO_FreshInstance_CaptureCount_Zero                  x1
    ///   SO_FreshInstance_TiersReached_Zero                  x1
    ///   SO_ComputeTier_BelowTier1_ReturnsZero               x1
    ///   SO_ComputeTier_MeetsTier1_ReturnsOne                x1
    ///   SO_ComputeTier_MeetsTier2_ReturnsTwo                x1
    ///   SO_ComputeTier_MeetsTier3_ReturnsThree              x1
    ///   SO_ApplyEndBonus_Tier1_AwardsBonus                  x1
    ///   SO_ApplyEndBonus_Tier2_AwardsCumulativeBonus        x1
    ///   SO_ApplyEndBonus_NoTier_ReturnsZero                 x1
    ///   SO_ApplyEndBonus_FiresEvent                         x1
    ///   SO_Reset_ClearsAll                                  x1
    ///   Controller_FreshInstance_EnduranceSO_Null           x1
    ///   Controller_Refresh_NullSO_HidesPanel                x1
    /// </summary>
    public sealed class ZoneControlMatchEnduranceBonusTests
    {
        private static ZoneControlMatchEnduranceBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchEnduranceBonusSO>();

        private static ZoneControlMatchEnduranceBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchEnduranceBonusController>();
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TiersReached_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TiersReached, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeTier_BelowTier1_ReturnsZero()
        {
            var so = CreateSO();
            for (int i = 0; i < so.Tier1Target - 1; i++)
                so.RecordCapture();
            Assert.That(so.ComputeTier(), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeTier_MeetsTier1_ReturnsOne()
        {
            var so = CreateSO();
            for (int i = 0; i < so.Tier1Target; i++)
                so.RecordCapture();
            Assert.That(so.ComputeTier(), Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeTier_MeetsTier2_ReturnsTwo()
        {
            var so = CreateSO();
            for (int i = 0; i < so.Tier2Target; i++)
                so.RecordCapture();
            Assert.That(so.ComputeTier(), Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeTier_MeetsTier3_ReturnsThree()
        {
            var so = CreateSO();
            for (int i = 0; i < so.Tier3Target; i++)
                so.RecordCapture();
            Assert.That(so.ComputeTier(), Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyEndBonus_Tier1_AwardsBonus()
        {
            var so = CreateSO();
            for (int i = 0; i < so.Tier1Target; i++)
                so.RecordCapture();
            int bonus = so.ApplyEndBonus();
            Assert.That(bonus,                Is.EqualTo(so.Tier1Bonus));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.Tier1Bonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyEndBonus_Tier2_AwardsCumulativeBonus()
        {
            var so = CreateSO();
            for (int i = 0; i < so.Tier2Target; i++)
                so.RecordCapture();
            int bonus    = so.ApplyEndBonus();
            int expected = so.Tier1Bonus + so.Tier2Bonus;
            Assert.That(bonus, Is.EqualTo(expected));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyEndBonus_NoTier_ReturnsZero()
        {
            var so = CreateSO();
            int bonus = so.ApplyEndBonus();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyEndBonus_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchEnduranceBonusSO)
                .GetField("_onBonusApplied", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.Tier1Target; i++)
                so.RecordCapture();
            so.ApplyEndBonus();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.Tier3Target; i++)
                so.RecordCapture();
            so.ApplyEndBonus();
            so.Reset();
            Assert.That(so.CaptureCount,      Is.EqualTo(0));
            Assert.That(so.TiersReached,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EnduranceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EnduranceSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchEnduranceBonusController)
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
