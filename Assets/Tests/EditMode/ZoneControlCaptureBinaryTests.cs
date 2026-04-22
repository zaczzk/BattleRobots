using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBinaryTests
    {
        private static ZoneControlCaptureBinarySO CreateSO(
            int bitsNeeded      = 6,
            int flipPerBot      = 2,
            int bonusPerPattern = 2080)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBinarySO>();
            typeof(ZoneControlCaptureBinarySO)
                .GetField("_bitsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bitsNeeded);
            typeof(ZoneControlCaptureBinarySO)
                .GetField("_flipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, flipPerBot);
            typeof(ZoneControlCaptureBinarySO)
                .GetField("_bonusPerPattern", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPattern);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBinaryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBinaryController>();
        }

        [Test]
        public void SO_FreshInstance_Bits_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bits, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PatternCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PatternCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBits()
        {
            var so = CreateSO(bitsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Bits, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bitsNeeded: 3, bonusPerPattern: 2080);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2080));
            Assert.That(so.PatternCount, Is.EqualTo(1));
            Assert.That(so.Bits,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bitsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FlipsBits()
        {
            var so = CreateSO(bitsNeeded: 6, flipPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bits, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bitsNeeded: 6, flipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bits, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BitProgress_Clamped()
        {
            var so = CreateSO(bitsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.BitProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPatternMatched_FiresEvent()
        {
            var so    = CreateSO(bitsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBinarySO)
                .GetField("_onPatternMatched", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bitsNeeded: 2, bonusPerPattern: 2080);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bits,              Is.EqualTo(0));
            Assert.That(so.PatternCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePatterns_Accumulate()
        {
            var so = CreateSO(bitsNeeded: 2, bonusPerPattern: 2080);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PatternCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4160));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BinarySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BinarySO, Is.Null);
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
            typeof(ZoneControlCaptureBinaryController)
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
