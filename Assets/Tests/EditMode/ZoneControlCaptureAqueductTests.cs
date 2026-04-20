using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAqueductTests
    {
        private static ZoneControlCaptureAqueductSO CreateSO(
            int capturesForFlow = 5,
            int drainPerBot     = 1,
            int bonusPerFlow    = 450)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAqueductSO>();
            typeof(ZoneControlCaptureAqueductSO)
                .GetField("_capturesForFlow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForFlow);
            typeof(ZoneControlCaptureAqueductSO)
                .GetField("_drainPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drainPerBot);
            typeof(ZoneControlCaptureAqueductSO)
                .GetField("_bonusPerFlow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFlow);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAqueductController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAqueductController>();
        }

        [Test]
        public void SO_FreshInstance_WaterLevel_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WaterLevel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FlowCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FlowCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsWater()
        {
            var so = CreateSO(capturesForFlow: 5);
            so.RecordPlayerCapture();
            Assert.That(so.WaterLevel, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FlowsAtThreshold()
        {
            var so    = CreateSO(capturesForFlow: 3, bonusPerFlow: 450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(450));
            Assert.That(so.FlowCount, Is.EqualTo(1));
            Assert.That(so.WaterLevel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(capturesForFlow: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsWater()
        {
            var so = CreateSO(capturesForFlow: 5, drainPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.WaterLevel, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(capturesForFlow: 5, drainPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.WaterLevel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WaterProgress_Clamped()
        {
            var so = CreateSO(capturesForFlow: 5);
            so.RecordPlayerCapture();
            Assert.That(so.WaterProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFlow_FiresEvent()
        {
            var so    = CreateSO(capturesForFlow: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAqueductSO)
                .GetField("_onFlow", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(capturesForFlow: 2, bonusPerFlow: 450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.WaterLevel,        Is.EqualTo(0));
            Assert.That(so.FlowCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFlows_Accumulate()
        {
            var so = CreateSO(capturesForFlow: 2, bonusPerFlow: 450);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FlowCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(900));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AqueductSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AqueductSO, Is.Null);
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
            typeof(ZoneControlCaptureAqueductController)
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
