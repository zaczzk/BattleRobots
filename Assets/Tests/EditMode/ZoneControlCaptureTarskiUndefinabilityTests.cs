using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTarskiUndefinabilityTests
    {
        private static ZoneControlCaptureTarskiUndefinabilitySO CreateSO(
            int truthPredicatesNeeded               = 6,
            int selfReferentialContradictionsPerBot = 1,
            int bonusPerUndefinability              = 4900)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTarskiUndefinabilitySO>();
            typeof(ZoneControlCaptureTarskiUndefinabilitySO)
                .GetField("_truthPredicatesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, truthPredicatesNeeded);
            typeof(ZoneControlCaptureTarskiUndefinabilitySO)
                .GetField("_selfReferentialContradictionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, selfReferentialContradictionsPerBot);
            typeof(ZoneControlCaptureTarskiUndefinabilitySO)
                .GetField("_bonusPerUndefinability", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerUndefinability);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTarskiUndefinabilityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTarskiUndefinabilityController>();
        }

        [Test]
        public void SO_FreshInstance_TruthPredicates_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TruthPredicates, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_UndefinabilityCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.UndefinabilityCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPredicates()
        {
            var so = CreateSO(truthPredicatesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TruthPredicates, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(truthPredicatesNeeded: 3, bonusPerUndefinability: 4900);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4900));
            Assert.That(so.UndefinabilityCount, Is.EqualTo(1));
            Assert.That(so.TruthPredicates,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(truthPredicatesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSelfReferentialContradictions()
        {
            var so = CreateSO(truthPredicatesNeeded: 6, selfReferentialContradictionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TruthPredicates, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(truthPredicatesNeeded: 6, selfReferentialContradictionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TruthPredicates, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TruthPredicateProgress_Clamped()
        {
            var so = CreateSO(truthPredicatesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TruthPredicateProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTarskiUndefinabilityReached_FiresEvent()
        {
            var so    = CreateSO(truthPredicatesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTarskiUndefinabilitySO)
                .GetField("_onTarskiUndefinabilityReached", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(truthPredicatesNeeded: 2, bonusPerUndefinability: 4900);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.TruthPredicates,    Is.EqualTo(0));
            Assert.That(so.UndefinabilityCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleUndefinabilities_Accumulate()
        {
            var so = CreateSO(truthPredicatesNeeded: 2, bonusPerUndefinability: 4900);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.UndefinabilityCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(9800));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TarskiUndefinabilitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TarskiUndefinabilitySO, Is.Null);
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
            typeof(ZoneControlCaptureTarskiUndefinabilityController)
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
