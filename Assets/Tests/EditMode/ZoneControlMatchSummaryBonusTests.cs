using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T421: <see cref="ZoneControlMatchSummaryBonusSO"/> and
    /// <see cref="ZoneControlMatchSummaryBonusController"/>.
    ///
    /// ZoneControlMatchSummaryBonusTests (13):
    ///   SO_FreshInstance_LastBonus_Zero                         x1
    ///   SO_FreshInstance_TotalBonus_Zero                        x1
    ///   SO_ComputeSummaryBonus_ZeroCaptures_PerfectEfficiency   x1
    ///   SO_ComputeSummaryBonus_FullInputs_ReturnsScale          x1
    ///   SO_ComputeSummaryBonus_ZeroWeights_ReturnsZero          x1
    ///   SO_ApplySummaryBonus_CachesLastBonus                    x1
    ///   SO_ApplySummaryBonus_AccumulatesTotalBonus              x1
    ///   SO_ApplySummaryBonus_FiresEventWhenBonusPositive        x1
    ///   SO_ApplySummaryBonus_ZeroBonus_NoEvent                  x1
    ///   SO_Reset_ClearsAll                                      x1
    ///   SO_ComputeSummaryBonus_EfficiencyOnly_ScalesCorrectly   x1
    ///   Controller_FreshInstance_SummaryBonusSO_Null            x1
    ///   Controller_Refresh_NullSO_HidesPanel                    x1
    /// </summary>
    public sealed class ZoneControlMatchSummaryBonusTests
    {
        private static ZoneControlMatchSummaryBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchSummaryBonusSO>();

        private static ZoneControlMatchSummaryBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchSummaryBonusController>();
        }

        [Test]
        public void SO_FreshInstance_LastBonus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LastBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeSummaryBonus_ZeroCaptures_PerfectEfficiency()
        {
            var so = CreateSO();
            // captures=1 → normCaptures=1; efficiency=1; combos=0 → normCombos=0
            // weighted = (2*1 + 3*1 + 1*0) / 6 = 5/6 ≈ 0.833
            // bonus = RoundToInt(5/6 * 100) = 83
            int bonus = so.ComputeSummaryBonus(1, 1f, 0);
            Assert.That(bonus, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeSummaryBonus_FullInputs_ReturnsScale()
        {
            var so = CreateSO();
            // captures>0 → norm=1; efficiency=1; combos>0 → norm=1
            // weighted = (2+3+1)/6 = 1 → bonus = BonusScale
            int bonus = so.ComputeSummaryBonus(5, 1f, 3);
            Assert.That(bonus, Is.EqualTo(so.BonusScale));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeSummaryBonus_ZeroWeights_ReturnsZero()
        {
            var so = CreateSO();
            // Override weights to zero via reflection
            var fi = typeof(ZoneControlMatchSummaryBonusSO);
            fi.GetField("_captureWeight",    BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, 0f);
            fi.GetField("_efficiencyWeight", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, 0f);
            fi.GetField("_comboWeight",      BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, 0f);
            int bonus = so.ComputeSummaryBonus(10, 1f, 5);
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplySummaryBonus_CachesLastBonus()
        {
            var so    = CreateSO();
            int bonus = so.ApplySummaryBonus(5, 1f, 3);
            Assert.That(so.LastBonus, Is.EqualTo(bonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplySummaryBonus_AccumulatesTotalBonus()
        {
            var so = CreateSO();
            int b1 = so.ApplySummaryBonus(5, 1f, 3);
            int b2 = so.ApplySummaryBonus(5, 1f, 3);
            Assert.That(so.TotalBonus, Is.EqualTo(b1 + b2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplySummaryBonus_FiresEventWhenBonusPositive()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchSummaryBonusSO)
                .GetField("_onBonusApplied", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.ApplySummaryBonus(5, 1f, 3);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ApplySummaryBonus_ZeroBonus_NoEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchSummaryBonusSO)
                .GetField("_onBonusApplied", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            // Force zero weight so bonus = 0
            var fi = typeof(ZoneControlMatchSummaryBonusSO);
            fi.GetField("_captureWeight",    BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, 0f);
            fi.GetField("_efficiencyWeight", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, 0f);
            fi.GetField("_comboWeight",      BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, 0f);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.ApplySummaryBonus(0, 0f, 0);

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.ApplySummaryBonus(5, 1f, 3);
            so.Reset();
            Assert.That(so.LastBonus,  Is.EqualTo(0));
            Assert.That(so.TotalBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeSummaryBonus_EfficiencyOnly_ScalesCorrectly()
        {
            var so = CreateSO();
            // captures=0 → norm=0; efficiency=0.5; combos=0 → norm=0
            // weighted = (0 + 3*0.5 + 0) / 6 = 1.5/6 = 0.25 → bonus = 25
            int bonus = so.ComputeSummaryBonus(0, 0.5f, 0);
            Assert.That(bonus, Is.EqualTo(25));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SummaryBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SummaryBonusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchSummaryBonusController)
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
