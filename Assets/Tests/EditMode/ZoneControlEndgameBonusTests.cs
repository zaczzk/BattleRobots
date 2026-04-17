using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T384: <see cref="ZoneControlEndgameBonusSO"/> and
    /// <see cref="ZoneControlEndgameBonusController"/>.
    ///
    /// ZoneControlEndgameBonusTests (12):
    ///   SO_FreshInstance_LastBonusAmount_Zero                        ×1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                      ×1
    ///   SO_ComputeBonus_BelowMinimum_ReturnsZero                     ×1
    ///   SO_ComputeBonus_AtMinimum_ReturnsCorrectBonus                ×1
    ///   SO_ApplyBonus_AboveMinimum_AccumulatesTotalBonus             ×1
    ///   SO_ApplyBonus_AboveMinimum_FiresOnBonusApplied               ×1
    ///   SO_ApplyBonus_BelowMinimum_DoesNotFireEvent                  ×1
    ///   SO_Reset_ClearsAll                                           ×1
    ///   Controller_FreshInstance_BonusSO_Null                        ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_Refresh_NullBonusSO_HidesPanel                    ×1
    /// </summary>
    public sealed class ZoneControlEndgameBonusTests
    {
        private static ZoneControlEndgameBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlEndgameBonusSO>();

        private static ZoneControlEndgameBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlEndgameBonusController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_LastBonusAmount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LastBonusAmount, Is.EqualTo(0));
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
        public void SO_ComputeBonus_BelowMinimum_ReturnsZero()
        {
            var so = CreateSO();
            // default minimum = 2; passing 1 should return 0
            Assert.That(so.ComputeBonus(1), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_AtMinimum_ReturnsCorrectBonus()
        {
            var so = CreateSO();
            // default minimum = 2, bonusPerZone = 100 → ComputeBonus(2) = 200
            int expected = so.MinimumZonesRequired * so.BonusPerZone;
            Assert.That(so.ComputeBonus(so.MinimumZonesRequired), Is.EqualTo(expected));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyBonus_AboveMinimum_AccumulatesTotalBonus()
        {
            var so = CreateSO();
            so.ApplyBonus(so.MinimumZonesRequired);
            so.ApplyBonus(so.MinimumZonesRequired);

            int expected = so.ComputeBonus(so.MinimumZonesRequired) * 2;
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(expected));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyBonus_AboveMinimum_FiresOnBonusApplied()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlEndgameBonusSO)
                .GetField("_onBonusApplied", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.ApplyBonus(so.MinimumZonesRequired);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ApplyBonus_BelowMinimum_DoesNotFireEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlEndgameBonusSO)
                .GetField("_onBonusApplied", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.ApplyBonus(1); // below default minimum of 2

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.ApplyBonus(so.MinimumZonesRequired);

            so.Reset();

            Assert.That(so.LastBonusAmount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullBonusSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlEndgameBonusController)
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
