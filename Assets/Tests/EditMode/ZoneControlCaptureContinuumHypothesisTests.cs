using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureContinuumHypothesisTests
    {
        private static ZoneControlCaptureContinuumHypothesisSO CreateSO(
            int cardinalWitnessesNeeded  = 5,
            int forcingObstructionsPerBot = 1,
            int bonusPerCardinalClass    = 4615)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureContinuumHypothesisSO>();
            typeof(ZoneControlCaptureContinuumHypothesisSO)
                .GetField("_cardinalWitnessesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cardinalWitnessesNeeded);
            typeof(ZoneControlCaptureContinuumHypothesisSO)
                .GetField("_forcingObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, forcingObstructionsPerBot);
            typeof(ZoneControlCaptureContinuumHypothesisSO)
                .GetField("_bonusPerCardinalClass", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCardinalClass);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureContinuumHypothesisController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureContinuumHypothesisController>();
        }

        [Test]
        public void SO_FreshInstance_CardinalWitnesses_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CardinalWitnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CardinalClassCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CardinalClassCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWitnesses()
        {
            var so = CreateSO(cardinalWitnessesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CardinalWitnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cardinalWitnessesNeeded: 3, bonusPerCardinalClass: 4615);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4615));
            Assert.That(so.CardinalClassCount,  Is.EqualTo(1));
            Assert.That(so.CardinalWitnesses,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cardinalWitnessesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesForcingObstructions()
        {
            var so = CreateSO(cardinalWitnessesNeeded: 5, forcingObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CardinalWitnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cardinalWitnessesNeeded: 5, forcingObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CardinalWitnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CardinalWitnessProgress_Clamped()
        {
            var so = CreateSO(cardinalWitnessesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CardinalWitnessProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCardinalClassified_FiresEvent()
        {
            var so    = CreateSO(cardinalWitnessesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureContinuumHypothesisSO)
                .GetField("_onCardinalClassified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cardinalWitnessesNeeded: 2, bonusPerCardinalClass: 4615);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CardinalWitnesses,  Is.EqualTo(0));
            Assert.That(so.CardinalClassCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClassifications_Accumulate()
        {
            var so = CreateSO(cardinalWitnessesNeeded: 2, bonusPerCardinalClass: 4615);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CardinalClassCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(9230));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ContinuumSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ContinuumSO, Is.Null);
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
            typeof(ZoneControlCaptureContinuumHypothesisController)
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
