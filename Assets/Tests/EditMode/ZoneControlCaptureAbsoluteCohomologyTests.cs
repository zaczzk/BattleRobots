using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAbsoluteCohomologyTests
    {
        private static ZoneControlCaptureAbsoluteCohomologySO CreateSO(
            int absoluteCyclesNeeded         = 6,
            int arithmeticObstructionsPerBot = 2,
            int bonusPerRealization           = 4240)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAbsoluteCohomologySO>();
            typeof(ZoneControlCaptureAbsoluteCohomologySO)
                .GetField("_absoluteCyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, absoluteCyclesNeeded);
            typeof(ZoneControlCaptureAbsoluteCohomologySO)
                .GetField("_arithmeticObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, arithmeticObstructionsPerBot);
            typeof(ZoneControlCaptureAbsoluteCohomologySO)
                .GetField("_bonusPerRealization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRealization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAbsoluteCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAbsoluteCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_AbsoluteCycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AbsoluteCycles, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesAbsoluteCycles()
        {
            var so = CreateSO(absoluteCyclesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.AbsoluteCycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(absoluteCyclesNeeded: 3, bonusPerRealization: 4240);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4240));
            Assert.That(so.RealizationCount, Is.EqualTo(1));
            Assert.That(so.AbsoluteCycles,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(absoluteCyclesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesArithmeticObstructions()
        {
            var so = CreateSO(absoluteCyclesNeeded: 6, arithmeticObstructionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.AbsoluteCycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(absoluteCyclesNeeded: 6, arithmeticObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.AbsoluteCycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AbsoluteCycleProgress_Clamped()
        {
            var so = CreateSO(absoluteCyclesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.AbsoluteCycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAbsoluteCohomologyRealized_FiresEvent()
        {
            var so    = CreateSO(absoluteCyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAbsoluteCohomologySO)
                .GetField("_onAbsoluteCohomologyRealized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(absoluteCyclesNeeded: 2, bonusPerRealization: 4240);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.AbsoluteCycles,    Is.EqualTo(0));
            Assert.That(so.RealizationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRealizations_Accumulate()
        {
            var so = CreateSO(absoluteCyclesNeeded: 2, bonusPerRealization: 4240);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RealizationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8480));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AbsoluteCohomologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AbsoluteCohomologySO, Is.Null);
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
            typeof(ZoneControlCaptureAbsoluteCohomologyController)
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
