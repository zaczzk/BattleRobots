using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRiftTests
    {
        private static ZoneControlCaptureRiftSO CreateSO(float idle = 15f, int bonus = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRiftSO>();
            typeof(ZoneControlCaptureRiftSO)
                .GetField("_idleThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, idle);
            typeof(ZoneControlCaptureRiftSO)
                .GetField("_riftBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRiftController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRiftController>();
        }

        [Test]
        public void SO_FreshInstance_IsRiftOpen_False()
        {
            var so = CreateSO();
            Assert.That(so.IsRiftOpen, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RiftCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RiftCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BelowThreshold_DoesNotOpenRift()
        {
            var so = CreateSO(idle: 10f);
            so.Tick(5f);
            Assert.That(so.IsRiftOpen, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExceedsThreshold_OpensRift()
        {
            var so = CreateSO(idle: 10f);
            so.Tick(11f);
            Assert.That(so.IsRiftOpen, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresOnRiftOpened_WhenThresholdMet()
        {
            var so    = CreateSO(idle: 5f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRiftSO)
                .GetField("_onRiftOpened", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.Tick(6f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_WhenOpen_DoesNotAccumulate()
        {
            var so = CreateSO(idle: 10f);
            so.Tick(11f);
            float timerSnapshot = so.IdleTimer;
            so.Tick(5f);
            Assert.That(so.IdleTimer, Is.EqualTo(timerSnapshot).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_WhenClosed_ReturnsZero()
        {
            var so    = CreateSO();
            int bonus = so.RecordCapture();
            Assert.That(bonus,        Is.EqualTo(0));
            Assert.That(so.RiftCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_WhenOpen_ClosesRiftAndReturnsBonus()
        {
            var so = CreateSO(idle: 5f, bonus: 100);
            so.Tick(6f);
            int bonus = so.RecordCapture();
            Assert.That(bonus,         Is.EqualTo(100));
            Assert.That(so.IsRiftOpen, Is.False);
            Assert.That(so.RiftCount,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AccumulatesTotalBonus()
        {
            var so = CreateSO(idle: 5f, bonus: 100);
            so.Tick(6f);
            so.RecordCapture();
            so.Tick(6f);
            so.RecordCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnRiftClosed()
        {
            var so    = CreateSO(idle: 5f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRiftSO)
                .GetField("_onRiftClosed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.Tick(6f);
            so.RecordCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(idle: 5f, bonus: 100);
            so.Tick(6f);
            so.RecordCapture();
            so.Reset();
            Assert.That(so.IsRiftOpen,        Is.False);
            Assert.That(so.RiftCount,         Is.EqualTo(0));
            Assert.That(so.IdleTimer,         Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RiftSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RiftSO, Is.Null);
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
            typeof(ZoneControlCaptureRiftController)
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
