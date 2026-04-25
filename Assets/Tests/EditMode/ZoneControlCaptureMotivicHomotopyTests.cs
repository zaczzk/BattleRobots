using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMotivicHomotopyTests
    {
        private static ZoneControlCaptureMotivicHomotopySO CreateSO(
            int a1LocalizationsNeeded   = 5,
            int nonA1ObstructionsPerBot = 1,
            int bonusPerContraction     = 4300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMotivicHomotopySO>();
            typeof(ZoneControlCaptureMotivicHomotopySO)
                .GetField("_a1LocalizationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, a1LocalizationsNeeded);
            typeof(ZoneControlCaptureMotivicHomotopySO)
                .GetField("_nonA1ObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nonA1ObstructionsPerBot);
            typeof(ZoneControlCaptureMotivicHomotopySO)
                .GetField("_bonusPerContraction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerContraction);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMotivicHomotopyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMotivicHomotopyController>();
        }

        [Test]
        public void SO_FreshInstance_A1Localizations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.A1Localizations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ContractionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ContractionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesA1Localizations()
        {
            var so = CreateSO(a1LocalizationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.A1Localizations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(a1LocalizationsNeeded: 3, bonusPerContraction: 4300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4300));
            Assert.That(so.ContractionCount, Is.EqualTo(1));
            Assert.That(so.A1Localizations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(a1LocalizationsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesNonA1Obstructions()
        {
            var so = CreateSO(a1LocalizationsNeeded: 5, nonA1ObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.A1Localizations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(a1LocalizationsNeeded: 5, nonA1ObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.A1Localizations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_A1LocalizationProgress_Clamped()
        {
            var so = CreateSO(a1LocalizationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.A1LocalizationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMotivicHomotopyContracted_FiresEvent()
        {
            var so    = CreateSO(a1LocalizationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMotivicHomotopySO)
                .GetField("_onMotivicHomotopyContracted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(a1LocalizationsNeeded: 2, bonusPerContraction: 4300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.A1Localizations,  Is.EqualTo(0));
            Assert.That(so.ContractionCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleContractions_Accumulate()
        {
            var so = CreateSO(a1LocalizationsNeeded: 2, bonusPerContraction: 4300);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ContractionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8600));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MotivicHomotopySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MotivicHomotopySO, Is.Null);
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
            typeof(ZoneControlCaptureMotivicHomotopyController)
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
