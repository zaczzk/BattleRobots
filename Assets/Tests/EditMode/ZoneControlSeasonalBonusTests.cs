using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlSeasonalBonusSO"/> and
    /// <see cref="ZoneControlSeasonalBonusController"/>.
    /// </summary>
    public sealed class ZoneControlSeasonalBonusTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlSeasonalBonusSO CreateBonusSO() =>
            ScriptableObject.CreateInstance<ZoneControlSeasonalBonusSO>();

        private static ZoneControlSeasonalBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlSeasonalBonusController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateBonusSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LastBonusAmount_Zero()
        {
            var so = CreateBonusSO();
            Assert.That(so.LastBonusAmount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetBonusForDivision_Bronze_ReturnsDefault()
        {
            var so = CreateBonusSO();
            Assert.That(so.GetBonusForDivision(ZoneControlLeagueDivision.Bronze), Is.EqualTo(so.BronzeBonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetBonusForDivision_Platinum_ReturnsHighest()
        {
            var so = CreateBonusSO();
            Assert.That(so.GetBonusForDivision(ZoneControlLeagueDivision.Platinum), Is.EqualTo(so.PlatinumBonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AwardBonus_UpdatesLastBonusAmount()
        {
            var so = CreateBonusSO();
            so.AwardBonus(ZoneControlLeagueDivision.Gold);
            Assert.That(so.LastBonusAmount, Is.EqualTo(so.GoldBonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AwardBonus_AccumulatesTotalBonus()
        {
            var so = CreateBonusSO();
            so.AwardBonus(ZoneControlLeagueDivision.Bronze);
            so.AwardBonus(ZoneControlLeagueDivision.Bronze);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.BronzeBonus * 2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AwardBonus_FiresEvent()
        {
            var so      = CreateBonusSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var field   = typeof(ZoneControlSeasonalBonusSO).GetField(
                "_onBonusAwarded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.AwardBonus(ZoneControlLeagueDivision.Silver);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateBonusSO();
            so.AwardBonus(ZoneControlLeagueDivision.Platinum);
            so.Reset();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.LastBonusAmount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BonusSO, Is.Null);
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
        public void Controller_Refresh_NullBonusSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            var panelField = typeof(ZoneControlSeasonalBonusController).GetField(
                "_panel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            panelField.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
