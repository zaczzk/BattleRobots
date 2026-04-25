using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePeanoAxiomsTests
    {
        private static ZoneControlCapturePeanoAxiomsSO CreateSO(
            int successorConstructionsNeeded = 6,
            int nonStandardModelsPerBot      = 1,
            int bonusPerAxiomSet             = 4840)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePeanoAxiomsSO>();
            typeof(ZoneControlCapturePeanoAxiomsSO)
                .GetField("_successorConstructionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, successorConstructionsNeeded);
            typeof(ZoneControlCapturePeanoAxiomsSO)
                .GetField("_nonStandardModelsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nonStandardModelsPerBot);
            typeof(ZoneControlCapturePeanoAxiomsSO)
                .GetField("_bonusPerAxiomSet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAxiomSet);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePeanoAxiomsController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePeanoAxiomsController>();
        }

        [Test]
        public void SO_FreshInstance_SuccessorConstructions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SuccessorConstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AxiomSetCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AxiomSetCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesConstructions()
        {
            var so = CreateSO(successorConstructionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SuccessorConstructions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(successorConstructionsNeeded: 3, bonusPerAxiomSet: 4840);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                     Is.EqualTo(4840));
            Assert.That(so.AxiomSetCount,          Is.EqualTo(1));
            Assert.That(so.SuccessorConstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(successorConstructionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesNonStandardModels()
        {
            var so = CreateSO(successorConstructionsNeeded: 6, nonStandardModelsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SuccessorConstructions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(successorConstructionsNeeded: 6, nonStandardModelsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SuccessorConstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SuccessorConstructionProgress_Clamped()
        {
            var so = CreateSO(successorConstructionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SuccessorConstructionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPeanoAxiomsConstructed_FiresEvent()
        {
            var so    = CreateSO(successorConstructionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePeanoAxiomsSO)
                .GetField("_onPeanoAxiomsConstructed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(successorConstructionsNeeded: 2, bonusPerAxiomSet: 4840);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SuccessorConstructions, Is.EqualTo(0));
            Assert.That(so.AxiomSetCount,          Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAxiomSets_Accumulate()
        {
            var so = CreateSO(successorConstructionsNeeded: 2, bonusPerAxiomSet: 4840);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AxiomSetCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9680));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PeanoAxiomsSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PeanoAxiomsSO, Is.Null);
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
            typeof(ZoneControlCapturePeanoAxiomsController)
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
