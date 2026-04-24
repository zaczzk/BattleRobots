using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCharacteristicClassTests
    {
        private static ZoneControlCaptureCharacteristicClassSO CreateSO(
            int obstructionsNeeded   = 5,
            int trivializationsPerBot = 1,
            int bonusPerEvaluation   = 4060)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCharacteristicClassSO>();
            typeof(ZoneControlCaptureCharacteristicClassSO)
                .GetField("_obstructionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, obstructionsNeeded);
            typeof(ZoneControlCaptureCharacteristicClassSO)
                .GetField("_trivializationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, trivializationsPerBot);
            typeof(ZoneControlCaptureCharacteristicClassSO)
                .GetField("_bonusPerEvaluation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEvaluation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCharacteristicClassController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCharacteristicClassController>();
        }

        [Test]
        public void SO_FreshInstance_Obstructions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Obstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EvaluationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EvaluationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesObstructions()
        {
            var so = CreateSO(obstructionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Obstructions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(obstructionsNeeded: 3, bonusPerEvaluation: 4060);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4060));
            Assert.That(so.EvaluationCount,  Is.EqualTo(1));
            Assert.That(so.Obstructions,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(obstructionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesTrivializations()
        {
            var so = CreateSO(obstructionsNeeded: 5, trivializationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Obstructions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(obstructionsNeeded: 5, trivializationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Obstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ObstructionProgress_Clamped()
        {
            var so = CreateSO(obstructionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ObstructionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCharacteristicClassEvaluated_FiresEvent()
        {
            var so    = CreateSO(obstructionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCharacteristicClassSO)
                .GetField("_onCharacteristicClassEvaluated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(obstructionsNeeded: 2, bonusPerEvaluation: 4060);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Obstructions,     Is.EqualTo(0));
            Assert.That(so.EvaluationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEvaluations_Accumulate()
        {
            var so = CreateSO(obstructionsNeeded: 2, bonusPerEvaluation: 4060);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EvaluationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8120));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CharacteristicClassSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CharacteristicClassSO, Is.Null);
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
            typeof(ZoneControlCaptureCharacteristicClassController)
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
