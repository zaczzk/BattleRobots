using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMultiplexerTests
    {
        private static ZoneControlCaptureMultiplexerSO CreateSO(
            int inputsNeeded  = 5,
            int dropPerBot    = 1,
            int bonusPerRoute = 1630)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMultiplexerSO>();
            typeof(ZoneControlCaptureMultiplexerSO)
                .GetField("_inputsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, inputsNeeded);
            typeof(ZoneControlCaptureMultiplexerSO)
                .GetField("_dropPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dropPerBot);
            typeof(ZoneControlCaptureMultiplexerSO)
                .GetField("_bonusPerRoute", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRoute);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMultiplexerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMultiplexerController>();
        }

        [Test]
        public void SO_FreshInstance_Inputs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Inputs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RouteCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RouteCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesInputs()
        {
            var so = CreateSO(inputsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Inputs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_InputsAtThreshold()
        {
            var so    = CreateSO(inputsNeeded: 3, bonusPerRoute: 1630);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1630));
            Assert.That(so.RouteCount, Is.EqualTo(1));
            Assert.That(so.Inputs,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(inputsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DropsInputs()
        {
            var so = CreateSO(inputsNeeded: 5, dropPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Inputs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(inputsNeeded: 5, dropPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Inputs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_InputProgress_Clamped()
        {
            var so = CreateSO(inputsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.InputProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMultiplexerRouted_FiresEvent()
        {
            var so    = CreateSO(inputsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMultiplexerSO)
                .GetField("_onMultiplexerRouted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(inputsNeeded: 2, bonusPerRoute: 1630);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Inputs,            Is.EqualTo(0));
            Assert.That(so.RouteCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRoutes_Accumulate()
        {
            var so = CreateSO(inputsNeeded: 2, bonusPerRoute: 1630);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RouteCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3260));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MultiplexerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MultiplexerSO, Is.Null);
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
            typeof(ZoneControlCaptureMultiplexerController)
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
