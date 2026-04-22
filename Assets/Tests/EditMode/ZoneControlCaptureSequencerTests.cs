using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSequencerTests
    {
        private static ZoneControlCaptureSequencerSO CreateSO(
            int stepsNeeded      = 8,
            int skipPerBot       = 2,
            int bonusPerSequence = 1750)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSequencerSO>();
            typeof(ZoneControlCaptureSequencerSO)
                .GetField("_stepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stepsNeeded);
            typeof(ZoneControlCaptureSequencerSO)
                .GetField("_skipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, skipPerBot);
            typeof(ZoneControlCaptureSequencerSO)
                .GetField("_bonusPerSequence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSequence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSequencerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSequencerController>();
        }

        [Test]
        public void SO_FreshInstance_Steps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Steps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SequenceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SequenceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSteps()
        {
            var so = CreateSO(stepsNeeded: 8);
            so.RecordPlayerCapture();
            Assert.That(so.Steps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(stepsNeeded: 3, bonusPerSequence: 1750);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(1750));
            Assert.That(so.SequenceCount,  Is.EqualTo(1));
            Assert.That(so.Steps,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stepsNeeded: 8);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSteps()
        {
            var so = CreateSO(stepsNeeded: 8, skipPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Steps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stepsNeeded: 8, skipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Steps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StepProgress_Clamped()
        {
            var so = CreateSO(stepsNeeded: 8);
            so.RecordPlayerCapture();
            Assert.That(so.StepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSequencerAdvanced_FiresEvent()
        {
            var so    = CreateSO(stepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSequencerSO)
                .GetField("_onSequencerAdvanced", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stepsNeeded: 2, bonusPerSequence: 1750);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Steps,             Is.EqualTo(0));
            Assert.That(so.SequenceCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSequences_Accumulate()
        {
            var so = CreateSO(stepsNeeded: 2, bonusPerSequence: 1750);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SequenceCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SequencerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SequencerSO, Is.Null);
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
            typeof(ZoneControlCaptureSequencerController)
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
