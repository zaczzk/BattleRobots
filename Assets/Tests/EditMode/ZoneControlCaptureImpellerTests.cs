using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureImpellerTests
    {
        private static ZoneControlCaptureImpellerSO CreateSO(
            int vanesNeeded  = 5,
            int slipPerBot   = 1,
            int bonusPerPump = 1315)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureImpellerSO>();
            typeof(ZoneControlCaptureImpellerSO)
                .GetField("_vanesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, vanesNeeded);
            typeof(ZoneControlCaptureImpellerSO)
                .GetField("_slipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slipPerBot);
            typeof(ZoneControlCaptureImpellerSO)
                .GetField("_bonusPerPump", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPump);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureImpellerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureImpellerController>();
        }

        [Test]
        public void SO_FreshInstance_Vanes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Vanes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PumpCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PumpCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesVanes()
        {
            var so = CreateSO(vanesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Vanes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_VanesAtThreshold()
        {
            var so    = CreateSO(vanesNeeded: 3, bonusPerPump: 1315);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1315));
            Assert.That(so.PumpCount,   Is.EqualTo(1));
            Assert.That(so.Vanes,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(vanesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesVanes()
        {
            var so = CreateSO(vanesNeeded: 5, slipPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Vanes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(vanesNeeded: 5, slipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Vanes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VaneProgress_Clamped()
        {
            var so = CreateSO(vanesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.VaneProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnImpellerPumped_FiresEvent()
        {
            var so    = CreateSO(vanesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureImpellerSO)
                .GetField("_onImpellerPumped", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(vanesNeeded: 2, bonusPerPump: 1315);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Vanes,             Is.EqualTo(0));
            Assert.That(so.PumpCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePumps_Accumulate()
        {
            var so = CreateSO(vanesNeeded: 2, bonusPerPump: 1315);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PumpCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2630));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ImpellerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ImpellerSO, Is.Null);
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
            typeof(ZoneControlCaptureImpellerController)
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
