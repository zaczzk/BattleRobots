using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T429: <see cref="ZoneControlZoneRushBonusSO"/> and
    /// <see cref="ZoneControlZoneRushBonusController"/>.
    ///
    /// ZoneControlZoneRushBonusTests (12):
    ///   SO_FreshInstance_RushCount_Zero                           x1
    ///   SO_FreshInstance_FastCaptureCount_Zero                    x1
    ///   SO_RecordCapture_SingleCapture_NoRush                     x1
    ///   SO_RecordCapture_TargetReached_IncrementsRushCount        x1
    ///   SO_RecordCapture_OutsideGap_ResetsCount                   x1
    ///   SO_RecordCapture_AfterRush_StartsNewSequence              x1
    ///   SO_RecordCapture_FiresRushAchievedEvent                   x1
    ///   SO_TotalBonusAwarded_AccumulatesPerRush                   x1
    ///   SO_Reset_ClearsAll                                        x1
    ///   SO_RushTargetCount_DefaultPositive                        x1
    ///   Controller_FreshInstance_RushBonusSO_Null                 x1
    ///   Controller_Refresh_NullSO_HidesPanel                      x1
    /// </summary>
    public sealed class ZoneControlZoneRushBonusTests
    {
        private static ZoneControlZoneRushBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneRushBonusSO>();

        private static ZoneControlZoneRushBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneRushBonusController>();
        }

        [Test]
        public void SO_FreshInstance_RushCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RushCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FastCaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FastCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_SingleCapture_NoRush()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            Assert.That(so.RushCount,        Is.EqualTo(0));
            Assert.That(so.FastCaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_TargetReached_IncrementsRushCount()
        {
            var so = CreateSO();
            // All at t=0 so gaps are 0 (within default 5s window)
            for (int i = 0; i < so.RushTargetCount; i++)
                so.RecordCapture(0f);
            Assert.That(so.RushCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_OutsideGap_ResetsCount()
        {
            var so = CreateSO();
            so.RecordCapture(0f);   // first capture, fastCount = 1
            so.RecordCapture(100f); // gap = 100 > 5 default → reset to 1
            Assert.That(so.FastCaptureCount, Is.EqualTo(1));
            Assert.That(so.RushCount,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AfterRush_StartsNewSequence()
        {
            var so = CreateSO();
            // Complete first rush
            for (int i = 0; i < so.RushTargetCount; i++)
                so.RecordCapture(0f);

            // One more capture starts a new potential rush
            so.RecordCapture(0f);
            Assert.That(so.FastCaptureCount, Is.EqualTo(1));
            Assert.That(so.RushCount,        Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresRushAchievedEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneRushBonusSO)
                .GetField("_onRushAchieved", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.RushTargetCount; i++)
                so.RecordCapture(0f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesPerRush()
        {
            var so    = CreateSO();
            int bonus = so.RushBonus;

            for (int i = 0; i < so.RushTargetCount; i++)
                so.RecordCapture(0f);

            Assert.That(so.TotalBonusAwarded, Is.EqualTo(bonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.RushTargetCount; i++)
                so.RecordCapture(0f);
            so.Reset();
            Assert.That(so.RushCount,         Is.EqualTo(0));
            Assert.That(so.FastCaptureCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RushTargetCount_DefaultPositive()
        {
            var so = CreateSO();
            Assert.That(so.RushTargetCount, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RushBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RushBonusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneRushBonusController)
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
