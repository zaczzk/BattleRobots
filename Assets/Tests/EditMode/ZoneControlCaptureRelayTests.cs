using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRelayTests
    {
        private static ZoneControlCaptureRelaySO CreateSO(
            int closuresNeeded = 5,
            int openPerBot     = 1,
            int bonusPerTrip   = 1600)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRelaySO>();
            typeof(ZoneControlCaptureRelaySO)
                .GetField("_closuresNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, closuresNeeded);
            typeof(ZoneControlCaptureRelaySO)
                .GetField("_openPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, openPerBot);
            typeof(ZoneControlCaptureRelaySO)
                .GetField("_bonusPerTrip", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTrip);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRelayController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRelayController>();
        }

        [Test]
        public void SO_FreshInstance_Closures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Closures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TripCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TripCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesClosures()
        {
            var so = CreateSO(closuresNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Closures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClosuresAtThreshold()
        {
            var so    = CreateSO(closuresNeeded: 3, bonusPerTrip: 1600);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1600));
            Assert.That(so.TripCount,  Is.EqualTo(1));
            Assert.That(so.Closures,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(closuresNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_OpensClosures()
        {
            var so = CreateSO(closuresNeeded: 5, openPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Closures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(closuresNeeded: 5, openPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Closures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClosureProgress_Clamped()
        {
            var so = CreateSO(closuresNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ClosureProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRelayTripped_FiresEvent()
        {
            var so    = CreateSO(closuresNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRelaySO)
                .GetField("_onRelayTripped", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(closuresNeeded: 2, bonusPerTrip: 1600);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Closures,          Is.EqualTo(0));
            Assert.That(so.TripCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTrips_Accumulate()
        {
            var so = CreateSO(closuresNeeded: 2, bonusPerTrip: 1600);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TripCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RelaySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RelaySO, Is.Null);
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
            typeof(ZoneControlCaptureRelayController)
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
