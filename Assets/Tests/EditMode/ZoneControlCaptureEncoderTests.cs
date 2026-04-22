using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEncoderTests
    {
        private static ZoneControlCaptureEncoderSO CreateSO(
            int symbolsNeeded  = 5,
            int errorPerBot    = 1,
            int bonusPerEncode = 1660)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEncoderSO>();
            typeof(ZoneControlCaptureEncoderSO)
                .GetField("_symbolsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, symbolsNeeded);
            typeof(ZoneControlCaptureEncoderSO)
                .GetField("_errorPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, errorPerBot);
            typeof(ZoneControlCaptureEncoderSO)
                .GetField("_bonusPerEncode", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEncode);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEncoderController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEncoderController>();
        }

        [Test]
        public void SO_FreshInstance_Symbols_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Symbols, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EncodeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EncodeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSymbols()
        {
            var so = CreateSO(symbolsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Symbols, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(symbolsNeeded: 3, bonusPerEncode: 1660);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1660));
            Assert.That(so.EncodeCount,  Is.EqualTo(1));
            Assert.That(so.Symbols,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(symbolsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSymbols()
        {
            var so = CreateSO(symbolsNeeded: 5, errorPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Symbols, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(symbolsNeeded: 5, errorPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Symbols, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SymbolProgress_Clamped()
        {
            var so = CreateSO(symbolsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SymbolProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEncoderEncoded_FiresEvent()
        {
            var so    = CreateSO(symbolsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEncoderSO)
                .GetField("_onEncoderEncoded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(symbolsNeeded: 2, bonusPerEncode: 1660);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Symbols,           Is.EqualTo(0));
            Assert.That(so.EncodeCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEncodes_Accumulate()
        {
            var so = CreateSO(symbolsNeeded: 2, bonusPerEncode: 1660);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EncodeCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3320));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EncoderSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EncoderSO, Is.Null);
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
            typeof(ZoneControlCaptureEncoderController)
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
