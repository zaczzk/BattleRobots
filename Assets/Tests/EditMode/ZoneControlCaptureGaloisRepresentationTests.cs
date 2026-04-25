using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGaloisRepresentationTests
    {
        private static ZoneControlCaptureGaloisRepresentationSO CreateSO(
            int representationsNeeded      = 6,
            int frobeniusObstructionsPerBot = 2,
            int bonusPerRealization         = 4360)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGaloisRepresentationSO>();
            typeof(ZoneControlCaptureGaloisRepresentationSO)
                .GetField("_representationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, representationsNeeded);
            typeof(ZoneControlCaptureGaloisRepresentationSO)
                .GetField("_frobeniusObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, frobeniusObstructionsPerBot);
            typeof(ZoneControlCaptureGaloisRepresentationSO)
                .GetField("_bonusPerRealization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRealization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGaloisRepresentationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGaloisRepresentationController>();
        }

        [Test]
        public void SO_FreshInstance_Representations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Representations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RealizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RealizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRepresentations()
        {
            var so = CreateSO(representationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Representations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(representationsNeeded: 3, bonusPerRealization: 4360);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4360));
            Assert.That(so.RealizationCount, Is.EqualTo(1));
            Assert.That(so.Representations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(representationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesFrobeniusObstructions()
        {
            var so = CreateSO(representationsNeeded: 6, frobeniusObstructionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Representations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(representationsNeeded: 6, frobeniusObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Representations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RepresentationProgress_Clamped()
        {
            var so = CreateSO(representationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.RepresentationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGaloisRepresentationRealized_FiresEvent()
        {
            var so    = CreateSO(representationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGaloisRepresentationSO)
                .GetField("_onGaloisRepresentationRealized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(representationsNeeded: 2, bonusPerRealization: 4360);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Representations,   Is.EqualTo(0));
            Assert.That(so.RealizationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRealizations_Accumulate()
        {
            var so = CreateSO(representationsNeeded: 2, bonusPerRealization: 4360);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RealizationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8720));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GaloisRepresentationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GaloisRepresentationSO, Is.Null);
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
            typeof(ZoneControlCaptureGaloisRepresentationController)
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
