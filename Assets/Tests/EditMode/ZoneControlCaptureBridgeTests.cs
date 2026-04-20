using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBridgeTests
    {
        private static ZoneControlCaptureBridgeSO CreateSO(
            int planksNeeded   = 6,
            int removalPerBot  = 2,
            int bonusPerBridge = 460)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBridgeSO>();
            typeof(ZoneControlCaptureBridgeSO)
                .GetField("_planksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, planksNeeded);
            typeof(ZoneControlCaptureBridgeSO)
                .GetField("_removalPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removalPerBot);
            typeof(ZoneControlCaptureBridgeSO)
                .GetField("_bonusPerBridge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBridge);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBridgeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBridgeController>();
        }

        [Test]
        public void SO_FreshInstance_Planks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Planks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BridgeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BridgeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPlanks()
        {
            var so = CreateSO(planksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Planks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesAtThreshold()
        {
            var so    = CreateSO(planksNeeded: 3, bonusPerBridge: 460);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(460));
            Assert.That(so.BridgeCount,  Is.EqualTo(1));
            Assert.That(so.Planks,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileBuilding()
        {
            var so    = CreateSO(planksNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPlanks()
        {
            var so = CreateSO(planksNeeded: 6, removalPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Planks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(planksNeeded: 6, removalPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Planks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlankProgress_Clamped()
        {
            var so = CreateSO(planksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PlankProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBridgeComplete_FiresEvent()
        {
            var so    = CreateSO(planksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBridgeSO)
                .GetField("_onBridgeComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(planksNeeded: 2, bonusPerBridge: 460);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Planks,            Is.EqualTo(0));
            Assert.That(so.BridgeCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBridges_Accumulate()
        {
            var so = CreateSO(planksNeeded: 2, bonusPerBridge: 460);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BridgeCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(920));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BridgeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BridgeSO, Is.Null);
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
            typeof(ZoneControlCaptureBridgeController)
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
