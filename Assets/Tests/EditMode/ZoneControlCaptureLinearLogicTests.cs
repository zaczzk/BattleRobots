using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLinearLogicTests
    {
        private static ZoneControlCaptureLinearLogicSO CreateSO(
            int resourceConsumptionsNeeded    = 6,
            int exponentialContractionsPerBot = 1,
            int bonusPerConsumption           = 5110)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLinearLogicSO>();
            typeof(ZoneControlCaptureLinearLogicSO)
                .GetField("_resourceConsumptionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, resourceConsumptionsNeeded);
            typeof(ZoneControlCaptureLinearLogicSO)
                .GetField("_exponentialContractionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, exponentialContractionsPerBot);
            typeof(ZoneControlCaptureLinearLogicSO)
                .GetField("_bonusPerConsumption", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConsumption);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLinearLogicController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLinearLogicController>();
        }

        [Test]
        public void SO_FreshInstance_ResourceConsumptions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResourceConsumptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConsumptionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsumptionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesResourceConsumptions()
        {
            var so = CreateSO(resourceConsumptionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ResourceConsumptions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(resourceConsumptionsNeeded: 3, bonusPerConsumption: 5110);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(5110));
            Assert.That(so.ConsumptionCount,     Is.EqualTo(1));
            Assert.That(so.ResourceConsumptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(resourceConsumptionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesExponentialContractions()
        {
            var so = CreateSO(resourceConsumptionsNeeded: 6, exponentialContractionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ResourceConsumptions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(resourceConsumptionsNeeded: 6, exponentialContractionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ResourceConsumptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResourceConsumptionProgress_Clamped()
        {
            var so = CreateSO(resourceConsumptionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ResourceConsumptionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLinearLogicCompleted_FiresEvent()
        {
            var so    = CreateSO(resourceConsumptionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLinearLogicSO)
                .GetField("_onLinearLogicCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(resourceConsumptionsNeeded: 2, bonusPerConsumption: 5110);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ResourceConsumptions, Is.EqualTo(0));
            Assert.That(so.ConsumptionCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConsumptions_Accumulate()
        {
            var so = CreateSO(resourceConsumptionsNeeded: 2, bonusPerConsumption: 5110);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConsumptionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10220));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LinearLogicSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LinearLogicSO, Is.Null);
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
            typeof(ZoneControlCaptureLinearLogicController)
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
