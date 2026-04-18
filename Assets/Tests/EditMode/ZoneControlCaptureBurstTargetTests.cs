using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T413: <see cref="ZoneControlCaptureBurstTargetSO"/> and
    /// <see cref="ZoneControlCaptureBurstTargetController"/>.
    ///
    /// ZoneControlCaptureBurstTargetTests (12):
    ///   SO_FreshInstance_BurstsMet_Zero                     x1
    ///   SO_FreshInstance_CaptureCount_Zero                  x1
    ///   SO_RecordCapture_BelowTarget_NoFire                 x1
    ///   SO_RecordCapture_MetTarget_FiresEvent               x1
    ///   SO_RecordCapture_MetTarget_IncsBurstCount           x1
    ///   SO_RecordCapture_MetTarget_AccumulatesBonus         x1
    ///   SO_RecordCapture_MetTarget_ClearsTimestamps         x1
    ///   SO_Tick_PrunesOldTimestamps                         x1
    ///   SO_Reset_ClearsAll                                  x1
    ///   Controller_FreshInstance_BurstTargetSO_Null         x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow           x1
    ///   Controller_Refresh_NullSO_HidesPanel                x1
    /// </summary>
    public sealed class ZoneControlCaptureBurstTargetTests
    {
        private static ZoneControlCaptureBurstTargetSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureBurstTargetSO>();

        private static ZoneControlCaptureBurstTargetController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBurstTargetController>();
        }

        [Test]
        public void SO_FreshInstance_BurstsMet_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BurstsMet, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowTarget_NoFire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBurstTargetSO)
                .GetField("_onBurstTargetMet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            int below = so.BurstTargetCount - 1;
            for (int i = 0; i < below; i++)
                so.RecordCapture(i);

            Assert.That(fired,         Is.EqualTo(0));
            Assert.That(so.BurstsMet,  Is.EqualTo(0));
            Assert.That(so.CaptureCount, Is.EqualTo(below));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_MetTarget_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBurstTargetSO)
                .GetField("_onBurstTargetMet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.BurstTargetCount; i++)
                so.RecordCapture(i);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_MetTarget_IncsBurstCount()
        {
            var so = CreateSO();
            for (int i = 0; i < so.BurstTargetCount; i++)
                so.RecordCapture(i);
            Assert.That(so.BurstsMet, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_MetTarget_AccumulatesBonus()
        {
            var so    = CreateSO();
            int bonus = so.BurstReward;
            for (int i = 0; i < so.BurstTargetCount; i++)
                so.RecordCapture(i);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(bonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_MetTarget_ClearsTimestamps()
        {
            var so = CreateSO();
            for (int i = 0; i < so.BurstTargetCount; i++)
                so.RecordCapture(i);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_PrunesOldTimestamps()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.Tick(so.BurstTargetCount + 100f);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.BurstTargetCount; i++)
                so.RecordCapture(i);
            so.Reset();
            Assert.That(so.BurstsMet,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Assert.That(so.CaptureCount,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BurstTargetSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BurstTargetSO, Is.Null);
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureBurstTargetController)
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
