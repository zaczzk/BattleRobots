using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSequentCalculusTests
    {
        private static ZoneControlCaptureSequentCalculusSO CreateSO(
            int sequentDerivationsNeeded   = 6,
            int structuralViolationsPerBot = 1,
            int bonusPerDerivation         = 4960)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSequentCalculusSO>();
            typeof(ZoneControlCaptureSequentCalculusSO)
                .GetField("_sequentDerivationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, sequentDerivationsNeeded);
            typeof(ZoneControlCaptureSequentCalculusSO)
                .GetField("_structuralViolationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, structuralViolationsPerBot);
            typeof(ZoneControlCaptureSequentCalculusSO)
                .GetField("_bonusPerDerivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDerivation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSequentCalculusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSequentCalculusController>();
        }

        [Test]
        public void SO_FreshInstance_SequentDerivations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SequentDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DerivationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DerivationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDerivations()
        {
            var so = CreateSO(sequentDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SequentDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(sequentDerivationsNeeded: 3, bonusPerDerivation: 4960);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4960));
            Assert.That(so.DerivationCount,     Is.EqualTo(1));
            Assert.That(so.SequentDerivations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(sequentDerivationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesStructuralViolations()
        {
            var so = CreateSO(sequentDerivationsNeeded: 6, structuralViolationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SequentDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(sequentDerivationsNeeded: 6, structuralViolationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SequentDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SequentDerivationProgress_Clamped()
        {
            var so = CreateSO(sequentDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SequentDerivationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSequentDerivationCompleted_FiresEvent()
        {
            var so    = CreateSO(sequentDerivationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSequentCalculusSO)
                .GetField("_onSequentDerivationCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(sequentDerivationsNeeded: 2, bonusPerDerivation: 4960);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SequentDerivations, Is.EqualTo(0));
            Assert.That(so.DerivationCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDerivations_Accumulate()
        {
            var so = CreateSO(sequentDerivationsNeeded: 2, bonusPerDerivation: 4960);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DerivationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9920));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SequentCalculusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SequentCalculusSO, Is.Null);
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
            typeof(ZoneControlCaptureSequentCalculusController)
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
