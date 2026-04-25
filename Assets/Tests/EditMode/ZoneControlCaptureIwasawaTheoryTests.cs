using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureIwasawaTheoryTests
    {
        private static ZoneControlCaptureIwasawaTheorySO CreateSO(
            int padicFunctionsNeeded    = 7,
            int selmerObstructionsPerBot = 2,
            int bonusPerInterpolation   = 4435)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureIwasawaTheorySO>();
            typeof(ZoneControlCaptureIwasawaTheorySO)
                .GetField("_padicFunctionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, padicFunctionsNeeded);
            typeof(ZoneControlCaptureIwasawaTheorySO)
                .GetField("_selmerObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, selmerObstructionsPerBot);
            typeof(ZoneControlCaptureIwasawaTheorySO)
                .GetField("_bonusPerInterpolation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInterpolation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureIwasawaTheoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureIwasawaTheoryController>();
        }

        [Test]
        public void SO_FreshInstance_PadicFunctions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PadicFunctions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InterpolationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InterpolationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPadicFunctions()
        {
            var so = CreateSO(padicFunctionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.PadicFunctions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(padicFunctionsNeeded: 3, bonusPerInterpolation: 4435);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                 Is.EqualTo(4435));
            Assert.That(so.InterpolationCount, Is.EqualTo(1));
            Assert.That(so.PadicFunctions,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(padicFunctionsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSelmerObstructions()
        {
            var so = CreateSO(padicFunctionsNeeded: 7, selmerObstructionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PadicFunctions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(padicFunctionsNeeded: 7, selmerObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PadicFunctions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PadicFunctionProgress_Clamped()
        {
            var so = CreateSO(padicFunctionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.PadicFunctionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnIwasawaTheoryInterpolated_FiresEvent()
        {
            var so    = CreateSO(padicFunctionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureIwasawaTheorySO)
                .GetField("_onIwasawaTheoryInterpolated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(padicFunctionsNeeded: 2, bonusPerInterpolation: 4435);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PadicFunctions,     Is.EqualTo(0));
            Assert.That(so.InterpolationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInterpolations_Accumulate()
        {
            var so = CreateSO(padicFunctionsNeeded: 2, bonusPerInterpolation: 4435);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InterpolationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(8870));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_IwasawaTheorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.IwasawaTheorySO, Is.Null);
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
            typeof(ZoneControlCaptureIwasawaTheoryController)
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
