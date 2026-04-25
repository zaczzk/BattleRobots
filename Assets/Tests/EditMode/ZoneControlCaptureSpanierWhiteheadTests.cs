using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpanierWhiteheadTests
    {
        private static ZoneControlCaptureSpanierWhiteheadSO CreateSO(
            int dualPairsNeeded              = 7,
            int suspensionInstabilitiesPerBot = 2,
            int bonusPerDualization          = 4315)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpanierWhiteheadSO>();
            typeof(ZoneControlCaptureSpanierWhiteheadSO)
                .GetField("_dualPairsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dualPairsNeeded);
            typeof(ZoneControlCaptureSpanierWhiteheadSO)
                .GetField("_suspensionInstabilitiesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, suspensionInstabilitiesPerBot);
            typeof(ZoneControlCaptureSpanierWhiteheadSO)
                .GetField("_bonusPerDualization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDualization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpanierWhiteheadController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpanierWhiteheadController>();
        }

        [Test]
        public void SO_FreshInstance_DualPairs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DualPairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DualizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DualizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDualPairs()
        {
            var so = CreateSO(dualPairsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.DualPairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(dualPairsNeeded: 3, bonusPerDualization: 4315);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4315));
            Assert.That(so.DualizationCount, Is.EqualTo(1));
            Assert.That(so.DualPairs,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(dualPairsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSuspensionInstabilities()
        {
            var so = CreateSO(dualPairsNeeded: 7, suspensionInstabilitiesPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DualPairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(dualPairsNeeded: 7, suspensionInstabilitiesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DualPairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DualPairProgress_Clamped()
        {
            var so = CreateSO(dualPairsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.DualPairProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpanierWhiteheadDualized_FiresEvent()
        {
            var so    = CreateSO(dualPairsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpanierWhiteheadSO)
                .GetField("_onSpanierWhiteheadDualized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(dualPairsNeeded: 2, bonusPerDualization: 4315);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.DualPairs,         Is.EqualTo(0));
            Assert.That(so.DualizationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDualizations_Accumulate()
        {
            var so = CreateSO(dualPairsNeeded: 2, bonusPerDualization: 4315);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DualizationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8630));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpanierWhiteheadSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpanierWhiteheadSO, Is.Null);
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
            typeof(ZoneControlCaptureSpanierWhiteheadController)
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
