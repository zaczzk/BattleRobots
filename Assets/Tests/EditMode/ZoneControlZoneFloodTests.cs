using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T451: <see cref="ZoneControlZoneFloodSO"/> and
    /// <see cref="ZoneControlZoneFloodController"/>.
    ///
    /// ZoneControlZoneFloodTests (12):
    ///   SO_FreshInstance_FloodCount_Zero                                    x1
    ///   SO_FreshInstance_IsFlooded_False                                    x1
    ///   SO_RecordCapture_BelowTotal_NoFlood                                 x1
    ///   SO_RecordCapture_AtTotal_FiresFlood                                 x1
    ///   SO_RecordCapture_Idempotent_WhenAlreadyFlooded                      x1
    ///   SO_RecordLoss_ClearsFloodActive                                     x1
    ///   SO_RecordCapture_AfterLoss_FiresAgain                               x1
    ///   SO_Reset_ClearsAll                                                  x1
    ///   SO_TotalBonusAwarded_AccumulatesPerFlood                            x1
    ///   Controller_FreshInstance_FloodSO_Null                               x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlZoneFloodTests
    {
        private static ZoneControlZoneFloodSO CreateSO(int totalZones = 4, int bonusPerFlood = 500)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneFloodSO>();
            typeof(ZoneControlZoneFloodSO)
                .GetField("_totalZones", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, totalZones);
            typeof(ZoneControlZoneFloodSO)
                .GetField("_bonusPerFlood", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFlood);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneFloodController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneFloodController>();
        }

        [Test]
        public void SO_FreshInstance_FloodCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FloodCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsFlooded_False()
        {
            var so = CreateSO();
            Assert.That(so.IsFlooded, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowTotal_NoFlood()
        {
            var so = CreateSO(totalZones: 4);
            so.RecordCapture(3);
            Assert.That(so.FloodCount, Is.EqualTo(0));
            Assert.That(so.IsFlooded,  Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtTotal_FiresFlood()
        {
            var so      = CreateSO(totalZones: 4);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneFloodSO)
                .GetField("_onFloodDetected", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCapture(4);

            Assert.That(so.FloodCount, Is.EqualTo(1));
            Assert.That(so.IsFlooded,  Is.True);
            Assert.That(fired,         Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_Idempotent_WhenAlreadyFlooded()
        {
            var so = CreateSO(totalZones: 4);
            so.RecordCapture(4);
            so.RecordCapture(4);
            Assert.That(so.FloodCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLoss_ClearsFloodActive()
        {
            var so = CreateSO(totalZones: 4);
            so.RecordCapture(4);
            so.RecordLoss(3);
            Assert.That(so.IsFlooded, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AfterLoss_FiresAgain()
        {
            var so = CreateSO(totalZones: 4);
            so.RecordCapture(4);
            so.RecordLoss(3);
            so.RecordCapture(4);
            Assert.That(so.FloodCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(totalZones: 4, bonusPerFlood: 500);
            so.RecordCapture(4);
            so.Reset();
            Assert.That(so.FloodCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.IsFlooded,         Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesPerFlood()
        {
            var so = CreateSO(totalZones: 2, bonusPerFlood: 300);
            so.RecordCapture(2);
            so.RecordLoss(1);
            so.RecordCapture(2);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(600));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FloodSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FloodSO, Is.Null);
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
            typeof(ZoneControlZoneFloodController)
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
