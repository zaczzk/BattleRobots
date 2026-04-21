using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureThrottleTests
    {
        private static ZoneControlCaptureThrottleSO CreateSO(
            int positionsNeeded = 6,
            int slipPerBot      = 2,
            int bonusPerOpen    = 1240)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureThrottleSO>();
            typeof(ZoneControlCaptureThrottleSO)
                .GetField("_positionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, positionsNeeded);
            typeof(ZoneControlCaptureThrottleSO)
                .GetField("_slipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slipPerBot);
            typeof(ZoneControlCaptureThrottleSO)
                .GetField("_bonusPerOpen", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerOpen);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureThrottleController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureThrottleController>();
        }

        [Test]
        public void SO_FreshInstance_Positions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Positions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_OpenCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OpenCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPositions()
        {
            var so = CreateSO(positionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Positions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PositionsAtThreshold()
        {
            var so    = CreateSO(positionsNeeded: 3, bonusPerOpen: 1240);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(1240));
            Assert.That(so.OpenCount, Is.EqualTo(1));
            Assert.That(so.Positions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(positionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPositions()
        {
            var so = CreateSO(positionsNeeded: 6, slipPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Positions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(positionsNeeded: 6, slipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Positions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PositionProgress_Clamped()
        {
            var so = CreateSO(positionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PositionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnThrottleOpened_FiresEvent()
        {
            var so    = CreateSO(positionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureThrottleSO)
                .GetField("_onThrottleOpened", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(positionsNeeded: 2, bonusPerOpen: 1240);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Positions,        Is.EqualTo(0));
            Assert.That(so.OpenCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleOpens_Accumulate()
        {
            var so = CreateSO(positionsNeeded: 2, bonusPerOpen: 1240);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.OpenCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2480));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ThrottleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ThrottleSO, Is.Null);
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
            typeof(ZoneControlCaptureThrottleController)
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
