using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTerminalTests
    {
        private static ZoneControlCaptureTerminalSO CreateSO(
            int arrowsNeeded    = 5,
            int rejectPerBot    = 1,
            int bonusPerTerminal = 2860)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTerminalSO>();
            typeof(ZoneControlCaptureTerminalSO)
                .GetField("_arrowsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, arrowsNeeded);
            typeof(ZoneControlCaptureTerminalSO)
                .GetField("_rejectPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rejectPerBot);
            typeof(ZoneControlCaptureTerminalSO)
                .GetField("_bonusPerTerminal", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTerminal);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTerminalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTerminalController>();
        }

        [Test]
        public void SO_FreshInstance_Arrows_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Arrows, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TerminalCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TerminalCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesArrows()
        {
            var so = CreateSO(arrowsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Arrows, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(arrowsNeeded: 3, bonusPerTerminal: 2860);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(2860));
            Assert.That(so.TerminalCount,   Is.EqualTo(1));
            Assert.That(so.Arrows,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(arrowsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesArrows()
        {
            var so = CreateSO(arrowsNeeded: 5, rejectPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Arrows, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(arrowsNeeded: 5, rejectPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Arrows, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ArrowProgress_Clamped()
        {
            var so = CreateSO(arrowsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ArrowProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTerminalReached_FiresEvent()
        {
            var so    = CreateSO(arrowsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTerminalSO)
                .GetField("_onTerminalReached", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(arrowsNeeded: 2, bonusPerTerminal: 2860);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Arrows,            Is.EqualTo(0));
            Assert.That(so.TerminalCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTerminals_Accumulate()
        {
            var so = CreateSO(arrowsNeeded: 2, bonusPerTerminal: 2860);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TerminalCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5720));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TerminalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TerminalSO, Is.Null);
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
            typeof(ZoneControlCaptureTerminalController)
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
