using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T404: <see cref="ZoneControlZoneOwnershipBonusSO"/> and
    /// <see cref="ZoneControlZoneOwnershipBonusController"/>.
    ///
    /// ZoneControlZoneOwnershipBonusTests (13):
    ///   SO_FreshInstance_IsRunning_False                        x1
    ///   SO_FreshInstance_TotalIntervalsCompleted_Zero           x1
    ///   SO_StartTracking_SetsIsRunning                          x1
    ///   SO_StartTracking_Idempotent                             x1
    ///   SO_StopTracking_ClearsIsRunning                         x1
    ///   SO_Tick_WhileRunning_CompletesInterval                  x1
    ///   SO_Tick_WhileNotRunning_NoInterval                      x1
    ///   SO_Tick_MultiInterval_CompletesMultiple                 x1
    ///   SO_ComputeBonus_ZeroZones_ZeroBonus                     x1
    ///   SO_ComputeBonus_MultipleZones_ReturnsProduct            x1
    ///   SO_Reset_ClearsAll                                      x1
    ///   Controller_FreshInstance_BonusSO_Null                   x1
    ///   Controller_Refresh_NullSO_HidesPanel                    x1
    /// </summary>
    public sealed class ZoneControlZoneOwnershipBonusTests
    {
        private static ZoneControlZoneOwnershipBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneOwnershipBonusSO>();

        private static ZoneControlZoneOwnershipBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneOwnershipBonusController>();
        }

        [Test]
        public void SO_FreshInstance_IsRunning_False()
        {
            var so = CreateSO();
            Assert.That(so.IsRunning, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalIntervalsCompleted_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalIntervalsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartTracking_SetsIsRunning()
        {
            var so = CreateSO();
            so.StartTracking();
            Assert.That(so.IsRunning, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartTracking_Idempotent()
        {
            var so = CreateSO();
            so.StartTracking();
            so.Tick(so.BonusInterval * 0.5f);
            so.StartTracking();
            Assert.That(so.IsRunning,               Is.True);
            Assert.That(so.TotalIntervalsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StopTracking_ClearsIsRunning()
        {
            var so = CreateSO();
            so.StartTracking();
            so.StopTracking();
            Assert.That(so.IsRunning, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileRunning_CompletesInterval()
        {
            var so = CreateSO();
            so.StartTracking();
            so.Tick(so.BonusInterval);
            Assert.That(so.TotalIntervalsCompleted, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileNotRunning_NoInterval()
        {
            var so = CreateSO();
            so.Tick(so.BonusInterval * 10f);
            Assert.That(so.TotalIntervalsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_MultiInterval_CompletesMultiple()
        {
            var so = CreateSO();
            so.StartTracking();
            so.Tick(so.BonusInterval * 3.5f);
            Assert.That(so.TotalIntervalsCompleted, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_ZeroZones_ZeroBonus()
        {
            var so = CreateSO();
            Assert.That(so.ComputeBonus(0), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_MultipleZones_ReturnsProduct()
        {
            var so = CreateSO();
            Assert.That(so.ComputeBonus(3), Is.EqualTo(3 * so.BonusPerZonePerInterval));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartTracking();
            so.Tick(so.BonusInterval * 2f);
            so.Reset();
            Assert.That(so.IsRunning,               Is.False);
            Assert.That(so.TotalIntervalsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BonusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneOwnershipBonusController)
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
