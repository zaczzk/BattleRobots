using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInductorTests
    {
        private static ZoneControlCaptureInductorSO CreateSO(
            int turnsNeeded       = 5,
            int fluxPerBot        = 1,
            int bonusPerInduction = 1540)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInductorSO>();
            typeof(ZoneControlCaptureInductorSO)
                .GetField("_turnsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, turnsNeeded);
            typeof(ZoneControlCaptureInductorSO)
                .GetField("_fluxPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fluxPerBot);
            typeof(ZoneControlCaptureInductorSO)
                .GetField("_bonusPerInduction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInduction);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInductorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInductorController>();
        }

        [Test]
        public void SO_FreshInstance_Turns_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Turns, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InductionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InductionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTurns()
        {
            var so = CreateSO(turnsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Turns, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TurnsAtThreshold()
        {
            var so    = CreateSO(turnsNeeded: 3, bonusPerInduction: 1540);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(1540));
            Assert.That(so.InductionCount, Is.EqualTo(1));
            Assert.That(so.Turns,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(turnsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FluxesTurns()
        {
            var so = CreateSO(turnsNeeded: 5, fluxPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Turns, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(turnsNeeded: 5, fluxPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Turns, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TurnProgress_Clamped()
        {
            var so = CreateSO(turnsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.TurnProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnInductorCharged_FiresEvent()
        {
            var so    = CreateSO(turnsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInductorSO)
                .GetField("_onInductorCharged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(turnsNeeded: 2, bonusPerInduction: 1540);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Turns,             Is.EqualTo(0));
            Assert.That(so.InductionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInductions_Accumulate()
        {
            var so = CreateSO(turnsNeeded: 2, bonusPerInduction: 1540);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InductionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3080));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InductorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InductorSO, Is.Null);
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
            typeof(ZoneControlCaptureInductorController)
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
