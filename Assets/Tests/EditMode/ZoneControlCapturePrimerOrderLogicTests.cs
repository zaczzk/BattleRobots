using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePrimerOrderLogicTests
    {
        private static ZoneControlCapturePrimerOrderLogicSO CreateSO(
            int validFormulasNeeded  = 6,
            int countermodelsPerBot  = 1,
            int bonusPerCompleteness = 4915)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePrimerOrderLogicSO>();
            typeof(ZoneControlCapturePrimerOrderLogicSO)
                .GetField("_validFormulasNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, validFormulasNeeded);
            typeof(ZoneControlCapturePrimerOrderLogicSO)
                .GetField("_countermodelsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, countermodelsPerBot);
            typeof(ZoneControlCapturePrimerOrderLogicSO)
                .GetField("_bonusPerCompleteness", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompleteness);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePrimerOrderLogicController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePrimerOrderLogicController>();
        }

        [Test]
        public void SO_FreshInstance_ValidFormulas_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ValidFormulas, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompletenessCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompletenessCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFormulas()
        {
            var so = CreateSO(validFormulasNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ValidFormulas, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(validFormulasNeeded: 3, bonusPerCompleteness: 4915);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4915));
            Assert.That(so.CompletenessCount, Is.EqualTo(1));
            Assert.That(so.ValidFormulas,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(validFormulasNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCountermodels()
        {
            var so = CreateSO(validFormulasNeeded: 6, countermodelsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ValidFormulas, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(validFormulasNeeded: 6, countermodelsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ValidFormulas, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ValidFormulaProgress_Clamped()
        {
            var so = CreateSO(validFormulasNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ValidFormulaProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFirstOrderCompletenessAchieved_FiresEvent()
        {
            var so    = CreateSO(validFormulasNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePrimerOrderLogicSO)
                .GetField("_onFirstOrderCompletenessAchieved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(validFormulasNeeded: 2, bonusPerCompleteness: 4915);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ValidFormulas,    Is.EqualTo(0));
            Assert.That(so.CompletenessCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompletenesses_Accumulate()
        {
            var so = CreateSO(validFormulasNeeded: 2, bonusPerCompleteness: 4915);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CompletenessCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9830));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PrimerOrderLogicSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PrimerOrderLogicSO, Is.Null);
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
            typeof(ZoneControlCapturePrimerOrderLogicController)
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
