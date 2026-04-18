using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T445: <see cref="ZoneControlMatchFinisherSO"/> and
    /// <see cref="ZoneControlMatchFinisherController"/>.
    ///
    /// ZoneControlMatchFinisherTests (12):
    ///   SO_FreshInstance_LastBonus_Zero                               x1
    ///   SO_ComputeLeadBonus_NegativeLead_ReturnsZero                  x1
    ///   SO_ComputeLeadBonus_ZeroLead_ReturnsZero                      x1
    ///   SO_ComputeLeadBonus_PositiveLead_ReturnsBonus                 x1
    ///   SO_ComputeLeadBonus_ClampsToMaxBonus                          x1
    ///   SO_ApplyFinisher_PositiveLead_AccumulatesBonus                x1
    ///   SO_ApplyFinisher_NegativeLead_NoBonus                         x1
    ///   SO_ApplyFinisher_FiresOnFinisherApplied                       x1
    ///   SO_Reset_ClearsAll                                            x1
    ///   Controller_FreshInstance_FinisherSO_Null                     x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlMatchFinisherTests
    {
        private static ZoneControlMatchFinisherSO CreateSO(int bonusPerLead = 50, int maxBonus = 1000)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchFinisherSO>();
            typeof(ZoneControlMatchFinisherSO)
                .GetField("_bonusPerLead", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLead);
            typeof(ZoneControlMatchFinisherSO)
                .GetField("_maxBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchFinisherController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchFinisherController>();
        }

        [Test]
        public void SO_FreshInstance_LastBonus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LastBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeLeadBonus_NegativeLead_ReturnsZero()
        {
            var so = CreateSO(bonusPerLead: 50);
            Assert.That(so.ComputeLeadBonus(playerCaptures: 2, botCaptures: 5), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeLeadBonus_ZeroLead_ReturnsZero()
        {
            var so = CreateSO(bonusPerLead: 50);
            Assert.That(so.ComputeLeadBonus(playerCaptures: 3, botCaptures: 3), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeLeadBonus_PositiveLead_ReturnsBonus()
        {
            var so = CreateSO(bonusPerLead: 50, maxBonus: 1000);
            // lead = 8 - 3 = 5 → 5 * 50 = 250
            Assert.That(so.ComputeLeadBonus(playerCaptures: 8, botCaptures: 3), Is.EqualTo(250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeLeadBonus_ClampsToMaxBonus()
        {
            var so = CreateSO(bonusPerLead: 100, maxBonus: 300);
            // lead = 10 → 10 * 100 = 1000 clamped to 300
            Assert.That(so.ComputeLeadBonus(playerCaptures: 10, botCaptures: 0), Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyFinisher_PositiveLead_AccumulatesBonus()
        {
            var so = CreateSO(bonusPerLead: 50, maxBonus: 1000);
            int bonus = so.ApplyFinisher(playerCaptures: 5, botCaptures: 2); // lead=3 → 150
            Assert.That(bonus,               Is.EqualTo(150));
            Assert.That(so.LastBonus,        Is.EqualTo(150));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyFinisher_NegativeLead_NoBonus()
        {
            var so = CreateSO(bonusPerLead: 50);
            int bonus = so.ApplyFinisher(playerCaptures: 1, botCaptures: 5);
            Assert.That(bonus,               Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyFinisher_FiresOnFinisherApplied()
        {
            var so      = CreateSO(bonusPerLead: 50);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchFinisherSO)
                .GetField("_onFinisherApplied", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.ApplyFinisher(playerCaptures: 0, botCaptures: 5);
            Assert.That(fired, Is.EqualTo(0)); // no bonus when behind

            so.ApplyFinisher(playerCaptures: 5, botCaptures: 0);
            Assert.That(fired, Is.EqualTo(1)); // bonus when ahead

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bonusPerLead: 50);
            so.ApplyFinisher(playerCaptures: 5, botCaptures: 0);
            so.Reset();
            Assert.That(so.LastBonus,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FinisherSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FinisherSO, Is.Null);
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
            typeof(ZoneControlMatchFinisherController)
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
