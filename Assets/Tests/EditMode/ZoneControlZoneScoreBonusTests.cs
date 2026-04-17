using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T373: <see cref="ZoneControlZoneScoreBonusSO"/> and
    /// <see cref="ZoneControlZoneScoreBonusController"/>.
    ///
    /// ZoneControlZoneScoreBonusTests (13):
    ///   SO_FreshInstance_ZonesHeld_Zero                    ×1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero            ×1
    ///   SO_SetZonesHeld_UpdatesCount                       ×1
    ///   SO_SetZonesHeld_ClampsNegative                     ×1
    ///   SO_ComputeBonus_BelowThreshold_ReturnsZero         ×1
    ///   SO_ComputeBonus_AtThreshold_ReturnsBonus           ×1
    ///   SO_ApplyBonus_AccumulatesTotalBonus                ×1
    ///   SO_ApplyBonus_FiresEvent_WhenBonusPositive         ×1
    ///   SO_ApplyBonus_DoesNotFireEvent_WhenBonusZero       ×1
    ///   SO_Reset_ClearsAll                                 ×1
    ///   Controller_FreshInstance_BonusSO_Null              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow          ×1
    ///   Controller_OnDisable_Unregisters_Channel           ×1
    /// </summary>
    public sealed class ZoneControlZoneScoreBonusTests
    {
        private static ZoneControlZoneScoreBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneScoreBonusSO>();

        private static ZoneControlZoneScoreBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneScoreBonusController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_ZonesHeld_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ZonesHeld, Is.EqualTo(0));
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
        public void SO_SetZonesHeld_UpdatesCount()
        {
            var so = CreateSO();
            so.SetZonesHeld(3);
            Assert.That(so.ZonesHeld, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZonesHeld_ClampsNegative()
        {
            var so = CreateSO();
            so.SetZonesHeld(-5);
            Assert.That(so.ZonesHeld, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_BelowThreshold_ReturnsZero()
        {
            var so = CreateSO();
            so.SetZonesHeld(so.MultiHoldThreshold - 1);
            Assert.That(so.ComputeBonus(), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_AtThreshold_ReturnsBonus()
        {
            var so = CreateSO();
            so.SetZonesHeld(so.MultiHoldThreshold);
            int expected = so.MultiHoldThreshold * so.BonusPerZoneHeld;
            Assert.That(so.ComputeBonus(), Is.EqualTo(expected));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyBonus_AccumulatesTotalBonus()
        {
            var so = CreateSO();
            so.SetZonesHeld(so.MultiHoldThreshold);
            so.ApplyBonus();
            so.ApplyBonus();
            int expected = so.MultiHoldThreshold * so.BonusPerZoneHeld * 2;
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(expected));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyBonus_FiresEvent_WhenBonusPositive()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneScoreBonusSO)
                .GetField("_onBonusTriggered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.SetZonesHeld(so.MultiHoldThreshold);
            so.ApplyBonus();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ApplyBonus_DoesNotFireEvent_WhenBonusZero()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneScoreBonusSO)
                .GetField("_onBonusTriggered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            // ZonesHeld defaults to 0 (below threshold)
            so.ApplyBonus();

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.SetZonesHeld(so.MultiHoldThreshold);
            so.ApplyBonus();
            so.Reset();
            Assert.That(so.ZonesHeld, Is.EqualTo(0));
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
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneScoreBonusController)
                .GetField("_onBonusTriggered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }
    }
}
