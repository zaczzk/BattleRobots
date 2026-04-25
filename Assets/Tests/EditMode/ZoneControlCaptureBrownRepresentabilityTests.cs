using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBrownRepresentabilityTests
    {
        private static ZoneControlCaptureBrownRepresentabilitySO CreateSO(
            int representableFunctorsNeeded        = 5,
            int nonRepresentableObstructionsPerBot = 1,
            int bonusPerRepresentation             = 4330)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBrownRepresentabilitySO>();
            typeof(ZoneControlCaptureBrownRepresentabilitySO)
                .GetField("_representableFunctorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, representableFunctorsNeeded);
            typeof(ZoneControlCaptureBrownRepresentabilitySO)
                .GetField("_nonRepresentableObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nonRepresentableObstructionsPerBot);
            typeof(ZoneControlCaptureBrownRepresentabilitySO)
                .GetField("_bonusPerRepresentation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRepresentation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBrownRepresentabilityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBrownRepresentabilityController>();
        }

        [Test]
        public void SO_FreshInstance_RepresentableFunctors_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RepresentableFunctors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RepresentationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RepresentationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRepresentableFunctors()
        {
            var so = CreateSO(representableFunctorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RepresentableFunctors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(representableFunctorsNeeded: 3, bonusPerRepresentation: 4330);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4330));
            Assert.That(so.RepresentationCount, Is.EqualTo(1));
            Assert.That(so.RepresentableFunctors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(representableFunctorsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesNonRepresentableObstructions()
        {
            var so = CreateSO(representableFunctorsNeeded: 5, nonRepresentableObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RepresentableFunctors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(representableFunctorsNeeded: 5, nonRepresentableObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RepresentableFunctors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RepresentableFunctorProgress_Clamped()
        {
            var so = CreateSO(representableFunctorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RepresentableFunctorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBrownRepresentabilityRepresented_FiresEvent()
        {
            var so    = CreateSO(representableFunctorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBrownRepresentabilitySO)
                .GetField("_onBrownRepresentabilityRepresented", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(representableFunctorsNeeded: 2, bonusPerRepresentation: 4330);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RepresentableFunctors, Is.EqualTo(0));
            Assert.That(so.RepresentationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRepresentations_Accumulate()
        {
            var so = CreateSO(representableFunctorsNeeded: 2, bonusPerRepresentation: 4330);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RepresentationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(8660));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BrownRepresentabilitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BrownRepresentabilitySO, Is.Null);
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
            typeof(ZoneControlCaptureBrownRepresentabilityController)
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
