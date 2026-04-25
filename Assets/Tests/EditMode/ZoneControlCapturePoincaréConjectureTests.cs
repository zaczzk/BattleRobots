using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePoincaréConjectureTests
    {
        private static ZoneControlCapturePoincaréConjectureSO CreateSO(
            int ricciFlowStepsNeeded = 5,
            int singularitiesPerBot  = 1,
            int bonusPerProof        = 4720)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePoincaréConjectureSO>();
            typeof(ZoneControlCapturePoincaréConjectureSO)
                .GetField("_ricciFlowStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ricciFlowStepsNeeded);
            typeof(ZoneControlCapturePoincaréConjectureSO)
                .GetField("_singularitiesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, singularitiesPerBot);
            typeof(ZoneControlCapturePoincaréConjectureSO)
                .GetField("_bonusPerProof", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerProof);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePoincaréConjectureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePoincaréConjectureController>();
        }

        [Test]
        public void SO_FreshInstance_RicciFlowSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RicciFlowSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ProofCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ProofCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRicciFlowSteps()
        {
            var so = CreateSO(ricciFlowStepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RicciFlowSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(ricciFlowStepsNeeded: 3, bonusPerProof: 4720);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(4720));
            Assert.That(so.ProofCount,     Is.EqualTo(1));
            Assert.That(so.RicciFlowSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ricciFlowStepsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSingularities()
        {
            var so = CreateSO(ricciFlowStepsNeeded: 5, singularitiesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RicciFlowSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ricciFlowStepsNeeded: 5, singularitiesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RicciFlowSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RicciFlowStepProgress_Clamped()
        {
            var so = CreateSO(ricciFlowStepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RicciFlowStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPoincaréConjectureProved_FiresEvent()
        {
            var so    = CreateSO(ricciFlowStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePoincaréConjectureSO)
                .GetField("_onPoincaréConjectureProved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ricciFlowStepsNeeded: 2, bonusPerProof: 4720);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RicciFlowSteps,    Is.EqualTo(0));
            Assert.That(so.ProofCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleProofs_Accumulate()
        {
            var so = CreateSO(ricciFlowStepsNeeded: 2, bonusPerProof: 4720);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ProofCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9440));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PoincaréSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PoincaréSO, Is.Null);
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
            typeof(ZoneControlCapturePoincaréConjectureController)
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
