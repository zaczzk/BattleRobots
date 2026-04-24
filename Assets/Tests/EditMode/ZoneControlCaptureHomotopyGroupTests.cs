using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHomotopyGroupTests
    {
        private static ZoneControlCaptureHomotopyGroupSO CreateSO(
            int loopsNeeded    = 5,
            int contractPerBot = 1,
            int bonusPerCompute = 3985)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHomotopyGroupSO>();
            typeof(ZoneControlCaptureHomotopyGroupSO)
                .GetField("_loopsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, loopsNeeded);
            typeof(ZoneControlCaptureHomotopyGroupSO)
                .GetField("_contractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, contractPerBot);
            typeof(ZoneControlCaptureHomotopyGroupSO)
                .GetField("_bonusPerCompute", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompute);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHomotopyGroupController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHomotopyGroupController>();
        }

        [Test]
        public void SO_FreshInstance_Loops_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComputeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComputeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLoops()
        {
            var so = CreateSO(loopsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(loopsNeeded: 3, bonusPerCompute: 3985);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(3985));
            Assert.That(so.ComputeCount,  Is.EqualTo(1));
            Assert.That(so.Loops,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(loopsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ContractsLoops()
        {
            var so = CreateSO(loopsNeeded: 5, contractPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(loopsNeeded: 5, contractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoopProgress_Clamped()
        {
            var so = CreateSO(loopsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.LoopProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHomotopyGroupComputed_FiresEvent()
        {
            var so    = CreateSO(loopsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHomotopyGroupSO)
                .GetField("_onHomotopyGroupComputed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(loopsNeeded: 2, bonusPerCompute: 3985);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Loops,            Is.EqualTo(0));
            Assert.That(so.ComputeCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleComputes_Accumulate()
        {
            var so = CreateSO(loopsNeeded: 2, bonusPerCompute: 3985);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComputeCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7970));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HomotopyGroupSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HomotopyGroupSO, Is.Null);
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
            typeof(ZoneControlCaptureHomotopyGroupController)
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
