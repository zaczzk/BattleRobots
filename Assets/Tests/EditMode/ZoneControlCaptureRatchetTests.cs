using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRatchetTests
    {
        private static ZoneControlCaptureRatchetSO CreateSO(
            int clicksNeeded    = 7,
            int slipPerBot      = 2,
            int bonusPerAdvance = 1150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRatchetSO>();
            typeof(ZoneControlCaptureRatchetSO)
                .GetField("_clicksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, clicksNeeded);
            typeof(ZoneControlCaptureRatchetSO)
                .GetField("_slipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slipPerBot);
            typeof(ZoneControlCaptureRatchetSO)
                .GetField("_bonusPerAdvance", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAdvance);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRatchetController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRatchetController>();
        }

        [Test]
        public void SO_FreshInstance_Clicks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Clicks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AdvanceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AdvanceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesClicks()
        {
            var so = CreateSO(clicksNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Clicks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClicksAtThreshold()
        {
            var so    = CreateSO(clicksNeeded: 3, bonusPerAdvance: 1150);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(1150));
            Assert.That(so.AdvanceCount,  Is.EqualTo(1));
            Assert.That(so.Clicks,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(clicksNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesClicks()
        {
            var so = CreateSO(clicksNeeded: 7, slipPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Clicks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(clicksNeeded: 7, slipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Clicks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClickProgress_Clamped()
        {
            var so = CreateSO(clicksNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ClickProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRatchetAdvanced_FiresEvent()
        {
            var so    = CreateSO(clicksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRatchetSO)
                .GetField("_onRatchetAdvanced", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(clicksNeeded: 2, bonusPerAdvance: 1150);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Clicks,            Is.EqualTo(0));
            Assert.That(so.AdvanceCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAdvances_Accumulate()
        {
            var so = CreateSO(clicksNeeded: 2, bonusPerAdvance: 1150);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AdvanceCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RatchetSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RatchetSO, Is.Null);
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
            typeof(ZoneControlCaptureRatchetController)
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
