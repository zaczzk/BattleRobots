using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSessionTypesTests
    {
        private static ZoneControlCaptureSessionTypesSO CreateSO(
            int protocolStepsNeeded      = 6,
            int protocolViolationsPerBot = 1,
            int bonusPerProtocolStep     = 5155)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSessionTypesSO>();
            typeof(ZoneControlCaptureSessionTypesSO)
                .GetField("_protocolStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, protocolStepsNeeded);
            typeof(ZoneControlCaptureSessionTypesSO)
                .GetField("_protocolViolationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, protocolViolationsPerBot);
            typeof(ZoneControlCaptureSessionTypesSO)
                .GetField("_bonusPerProtocolStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerProtocolStep);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSessionTypesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSessionTypesController>();
        }

        [Test]
        public void SO_FreshInstance_ProtocolSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ProtocolSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SessionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SessionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesProtocolSteps()
        {
            var so = CreateSO(protocolStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ProtocolSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(protocolStepsNeeded: 3, bonusPerProtocolStep: 5155);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(5155));
            Assert.That(so.SessionCount,  Is.EqualTo(1));
            Assert.That(so.ProtocolSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(protocolStepsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesProtocolViolations()
        {
            var so = CreateSO(protocolStepsNeeded: 6, protocolViolationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ProtocolSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(protocolStepsNeeded: 6, protocolViolationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ProtocolSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ProtocolStepProgress_Clamped()
        {
            var so = CreateSO(protocolStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ProtocolStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSessionTypesCompleted_FiresEvent()
        {
            var so    = CreateSO(protocolStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSessionTypesSO)
                .GetField("_onSessionTypesCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(protocolStepsNeeded: 2, bonusPerProtocolStep: 5155);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ProtocolSteps,      Is.EqualTo(0));
            Assert.That(so.SessionCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSessions_Accumulate()
        {
            var so = CreateSO(protocolStepsNeeded: 2, bonusPerProtocolStep: 5155);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SessionCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10310));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SessionTypesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SessionTypesSO, Is.Null);
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
            typeof(ZoneControlCaptureSessionTypesController)
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
