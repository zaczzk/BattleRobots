using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNashEquilibriumTests
    {
        private static ZoneControlCaptureNashEquilibriumSO CreateSO(
            int strategyPairsNeeded = 5,
            int defectionsPerBot    = 1,
            int bonusPerEquilibrium = 4585)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNashEquilibriumSO>();
            typeof(ZoneControlCaptureNashEquilibriumSO)
                .GetField("_strategyPairsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, strategyPairsNeeded);
            typeof(ZoneControlCaptureNashEquilibriumSO)
                .GetField("_defectionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, defectionsPerBot);
            typeof(ZoneControlCaptureNashEquilibriumSO)
                .GetField("_bonusPerEquilibrium", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEquilibrium);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNashEquilibriumController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNashEquilibriumController>();
        }

        [Test]
        public void SO_FreshInstance_StrategyPairs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StrategyPairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EquilibriumCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EquilibriumCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStrategyPairs()
        {
            var so = CreateSO(strategyPairsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StrategyPairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(strategyPairsNeeded: 3, bonusPerEquilibrium: 4585);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4585));
            Assert.That(so.EquilibriumCount,  Is.EqualTo(1));
            Assert.That(so.StrategyPairs,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(strategyPairsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesDefection()
        {
            var so = CreateSO(strategyPairsNeeded: 5, defectionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StrategyPairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(strategyPairsNeeded: 5, defectionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StrategyPairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StrategyPairProgress_Clamped()
        {
            var so = CreateSO(strategyPairsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StrategyPairProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnNashEquilibriumReached_FiresEvent()
        {
            var so    = CreateSO(strategyPairsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNashEquilibriumSO)
                .GetField("_onNashEquilibriumReached", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(strategyPairsNeeded: 2, bonusPerEquilibrium: 4585);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.StrategyPairs,     Is.EqualTo(0));
            Assert.That(so.EquilibriumCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEquilibria_Accumulate()
        {
            var so = CreateSO(strategyPairsNeeded: 2, bonusPerEquilibrium: 4585);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EquilibriumCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9170));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NashSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NashSO, Is.Null);
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
            typeof(ZoneControlCaptureNashEquilibriumController)
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
