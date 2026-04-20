using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureShrineTests
    {
        private static ZoneControlCaptureShrineSO CreateSO(
            int candlesNeeded        = 5,
            int snuffPerBot          = 1,
            int bonusPerPurification = 480)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureShrineSO>();
            typeof(ZoneControlCaptureShrineSO)
                .GetField("_candlesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, candlesNeeded);
            typeof(ZoneControlCaptureShrineSO)
                .GetField("_snuffPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, snuffPerBot);
            typeof(ZoneControlCaptureShrineSO)
                .GetField("_bonusPerPurification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPurification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureShrineController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureShrineController>();
        }

        [Test]
        public void SO_FreshInstance_Candles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Candles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PurificationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PurificationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCandles()
        {
            var so = CreateSO(candlesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Candles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PurifiesAtThreshold()
        {
            var so    = CreateSO(candlesNeeded: 3, bonusPerPurification: 480);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(480));
            Assert.That(so.PurificationCount, Is.EqualTo(1));
            Assert.That(so.Candles,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileLighting()
        {
            var so    = CreateSO(candlesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SnuffsCandles()
        {
            var so = CreateSO(candlesNeeded: 5, snuffPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Candles, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(candlesNeeded: 5, snuffPerBot: 4);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Candles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CandleProgress_Clamped()
        {
            var so = CreateSO(candlesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CandleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnShrinePurified_FiresEvent()
        {
            var so    = CreateSO(candlesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureShrineSO)
                .GetField("_onShrinePurified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(candlesNeeded: 2, bonusPerPurification: 480);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Candles,            Is.EqualTo(0));
            Assert.That(so.PurificationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePurifications_Accumulate()
        {
            var so = CreateSO(candlesNeeded: 2, bonusPerPurification: 480);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PurificationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(960));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ShrineSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ShrineSO, Is.Null);
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
            typeof(ZoneControlCaptureShrineController)
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
